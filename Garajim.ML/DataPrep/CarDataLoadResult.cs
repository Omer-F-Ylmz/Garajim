using Garajim.ML.Models;

namespace Garajim.ML.DataPrep
{
    public class CarDataLoadResult
    {
        public List<CarPriceInput> Samples { get; } = new List<CarPriceInput>();

        public int TotalRows { get; set; }

        public int InvalidRows { get; set; }

        public int OutOfRangeRows { get; set; }

        public int DuplicateRows { get; set; }

        public int KeptRows => Samples.Count;
    }
}
