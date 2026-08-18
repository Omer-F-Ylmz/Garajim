namespace Garajim.ML.Models
{
    public class CarPriceInput
    {
        public string Marka { get; set; }

        public string Seri { get; set; }

        public float Yil { get; set; }

        public float Kilometre { get; set; }

        public string YakitTipi { get; set; }

        public string VitesTipi { get; set; }

        public string KasaTipi { get; set; }

        public float Fiyat { get; set; }

        public float LogFiyat { get; set; }
    }
}
