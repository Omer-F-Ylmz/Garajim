namespace Garajim.KatalogUretici
{
    public sealed class GlobalKatalogBelgesi
    {
        public string Surum { get; set; }

        public string Kaynak { get; set; }

        public string UretimTarihi { get; set; }

        public List<GlobalMarka> Markalar { get; set; } = new List<GlobalMarka>();
    }

    public sealed class GlobalMarka
    {
        public string Ad { get; set; }

        public bool Tr { get; set; }

        public List<GlobalSeri> Seriler { get; set; } = new List<GlobalSeri>();
    }

    public sealed class GlobalSeri
    {
        public string Ad { get; set; }

        public bool Tr { get; set; }
    }
}
