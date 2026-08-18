namespace Garajim.Entity.Dtos
{
    public class PriceEstimateDto
    {
        public string Marka { get; set; }

        public string Seri { get; set; }

        public int Yil { get; set; }

        public int Kilometre { get; set; }

        public string YakitTipi { get; set; }

        public string VitesTipi { get; set; }

        public string KasaTipi { get; set; }
    }

    public class PriceEstimateResultDto
    {
        public decimal TahminiFiyat { get; set; }

        public string ParaBirimi { get; set; }

        public string Marka { get; set; }

        public string Seri { get; set; }

        public int Yil { get; set; }

        public int Kilometre { get; set; }
    }
}
