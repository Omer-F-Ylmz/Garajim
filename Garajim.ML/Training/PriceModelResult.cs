using Microsoft.ML;

namespace Garajim.ML.Training
{
    public class PriceModelResult
    {
        public PriceTarget Target { get; set; }

        public ITransformer Model { get; set; }

        public DataViewSchema TrainSchema { get; set; }

        public double RSquared { get; set; }

        public double MeanAbsoluteErrorTl { get; set; }

        public double RootMeanSquaredErrorTl { get; set; }

        public long TrainRowCount { get; set; }

        public long TestRowCount { get; set; }

        public string RSquaredScale => Target == PriceTarget.Log ? "log" : "TL";
    }
}
