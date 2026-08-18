using Garajim.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Garajim.ML.Training
{
    public class PriceModelTrainer
    {
        public const string PriceColumn = nameof(CarPriceInput.Fiyat);
        public const string LogPriceColumn = nameof(CarPriceInput.LogFiyat);
        public const string FeatureColumn = "Features";
        public const string ScoreColumn = "Score";

        private readonly MLContext _mlContext;

        public PriceModelTrainer(int seed = 42)
        {
            _mlContext = new MLContext(seed);
        }

        public DataOperationsCatalog.TrainTestData Split(IEnumerable<CarPriceInput> samples, double testFraction = 0.2)
        {
            var data = _mlContext.Data.LoadFromEnumerable(samples);
            return _mlContext.Data.TrainTestSplit(data, testFraction, seed: 42);
        }

        public PriceModelResult Train(DataOperationsCatalog.TrainTestData split, PriceTarget target)
        {
            var labelColumn = target == PriceTarget.Log ? LogPriceColumn : PriceColumn;

            var model = BuildPipeline(labelColumn).Fit(split.TrainSet);
            var scored = model.Transform(split.TestSet);

            var rSquared = _mlContext.Regression.Evaluate(scored, labelColumnName: labelColumn).RSquared;
            var scores = scored.GetColumn<float>(ScoreColumn).ToArray();
            var actual = scored.GetColumn<float>(PriceColumn).ToArray();

            double absoluteTotal = 0;
            double squaredTotal = 0;

            for (var i = 0; i < scores.Length; i++)
            {
                var predicted = target == PriceTarget.Log ? PriceScale.FromLog(scores[i]) : scores[i];
                var error = predicted - actual[i];
                absoluteTotal += Math.Abs(error);
                squaredTotal += (double)error * error;
            }

            return new PriceModelResult
            {
                Target = target,
                Model = model,
                TrainSchema = split.TrainSet.Schema,
                RSquared = rSquared,
                MeanAbsoluteErrorTl = scores.Length == 0 ? 0 : absoluteTotal / scores.Length,
                RootMeanSquaredErrorTl = scores.Length == 0 ? 0 : Math.Sqrt(squaredTotal / scores.Length),
                TrainRowCount = CountRows(split.TrainSet, PriceColumn),
                TestRowCount = scores.Length
            };
        }

        public void Save(PriceModelResult result, string modelPath)
        {
            var directory = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _mlContext.Model.Save(result.Model, result.TrainSchema, modelPath);
        }

        public PredictionEngine<CarPriceInput, CarPricePrediction> CreatePredictionEngine(ITransformer model)
        {
            return _mlContext.Model.CreatePredictionEngine<CarPriceInput, CarPricePrediction>(model);
        }

        public float Predict(PredictionEngine<CarPriceInput, CarPricePrediction> engine, CarPriceInput input, PriceTarget target)
        {
            var score = engine.Predict(input).LogFiyat;
            return target == PriceTarget.Log ? PriceScale.FromLog(score) : score;
        }

        private IEstimator<ITransformer> BuildPipeline(string labelColumn)
        {
            var encoded = new[]
            {
                new InputOutputColumnPair("MarkaEncoded", nameof(CarPriceInput.Marka)),
                new InputOutputColumnPair("SeriEncoded", nameof(CarPriceInput.Seri)),
                new InputOutputColumnPair("YakitTipiEncoded", nameof(CarPriceInput.YakitTipi)),
                new InputOutputColumnPair("VitesTipiEncoded", nameof(CarPriceInput.VitesTipi)),
                new InputOutputColumnPair("KasaTipiEncoded", nameof(CarPriceInput.KasaTipi))
            };

            return _mlContext.Transforms.Categorical.OneHotEncoding(encoded)
                .Append(_mlContext.Transforms.Concatenate(
                    FeatureColumn,
                    "MarkaEncoded",
                    "SeriEncoded",
                    "YakitTipiEncoded",
                    "VitesTipiEncoded",
                    "KasaTipiEncoded",
                    nameof(CarPriceInput.Yil),
                    nameof(CarPriceInput.Kilometre)))
                .Append(_mlContext.Regression.Trainers.FastTree(
                    labelColumnName: labelColumn,
                    featureColumnName: FeatureColumn,
                    numberOfLeaves: 64,
                    numberOfTrees: 400,
                    minimumExampleCountPerLeaf: 10,
                    learningRate: 0.1));
        }

        private static long CountRows(IDataView view, string column)
        {
            var count = view.GetRowCount();
            if (count.HasValue)
            {
                return count.Value;
            }

            return view.GetColumn<float>(column).LongCount();
        }
    }
}
