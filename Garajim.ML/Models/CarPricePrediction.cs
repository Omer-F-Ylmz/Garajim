using Microsoft.ML.Data;

namespace Garajim.ML.Models
{
    public class CarPricePrediction
    {
        [ColumnName("Score")]
        public float LogFiyat { get; set; }
    }
}
