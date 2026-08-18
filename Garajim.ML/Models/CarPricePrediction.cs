using Microsoft.ML.Data;

namespace Garajim.ML.Models
{
    public class CarPricePrediction
    {
        [ColumnName("Score")]
        public float TahminiFiyat { get; set; }
    }
}
