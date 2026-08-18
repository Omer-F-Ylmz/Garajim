using Microsoft.ML;

namespace Garajim.ML.Training
{
    public class PriceModelResult
    {
        public ITransformer Model { get; set; }

        public DataViewSchema TrainSchema { get; set; }

        public double RSquared { get; set; }

        public double MeanAbsoluteError { get; set; }

        public double RootMeanSquaredError { get; set; }

        public long TrainRowCount { get; set; }

        public long TestRowCount { get; set; }
    }
}
