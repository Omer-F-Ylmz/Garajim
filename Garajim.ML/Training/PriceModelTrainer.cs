using Garajim.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Garajim.ML.Training
{
    public class PriceModelTrainer
    {
        public const string LabelColumn = nameof(CarPriceInput.Fiyat);
        public const string FeatureColumn = "Features";

        private readonly MLContext _mlContext;

        public PriceModelTrainer(int seed = 42)
        {
            _mlContext = new MLContext(seed);
        }

        public PriceModelResult Train(IEnumerable<CarPriceInput> samples, double testFraction = 0.2)
        {
            var data = _mlContext.Data.LoadFromEnumerable(samples);
            var split = _mlContext.Data.TrainTestSplit(data, testFraction, seed: 42);

            var model = BuildPipeline().Fit(split.TrainSet);
            var scored = model.Transform(split.TestSet);
            var metrics = _mlContext.Regression.Evaluate(scored, labelColumnName: LabelColumn);

            return new PriceModelResult
            {
                Model = model,
                TrainSchema = split.TrainSet.Schema,
                RSquared = metrics.RSquared,
                MeanAbsoluteError = metrics.MeanAbsoluteError,
                RootMeanSquaredError = metrics.RootMeanSquaredError,
                TrainRowCount = CountRows(split.TrainSet),
                TestRowCount = CountRows(split.TestSet)
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

        private IEstimator<ITransformer> BuildPipeline()
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
                    labelColumnName: LabelColumn,
                    featureColumnName: FeatureColumn,
                    numberOfLeaves: 64,
                    numberOfTrees: 400,
                    minimumExampleCountPerLeaf: 10,
                    learningRate: 0.1));
        }

        private static long CountRows(IDataView view)
        {
            var count = view.GetRowCount();
            if (count.HasValue)
            {
                return count.Value;
            }

            return view.GetColumn<float>(LabelColumn).LongCount();
        }
    }
}
