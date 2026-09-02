namespace Garajim.Entity.Dtos
{
    public class DashboardDto
    {
        public string Plan { get; set; }
        public int AracSayisi { get; set; }
        public int AracLimiti { get; set; }
        public int AktifZimmet { get; set; }
        public int EvrakGecti { get; set; }
        public int EvrakYaklasiyor { get; set; }
        public int HatirlatmaYaklasiyor { get; set; }
        public int BekleyenFis { get; set; }
        public decimal BuAyMaliyet { get; set; }
        public decimal GecenAyMaliyet { get; set; }
        public decimal? DegisimYuzde { get; set; }
        public bool KisLastigiDonemi { get; set; }
        public string KisLastigiUyarisi { get; set; }
        public List<string> KisLastigiUyariPlakalari { get; set; } = new List<string>();
    }
}
