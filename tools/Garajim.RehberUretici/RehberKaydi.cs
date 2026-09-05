namespace Garajim.RehberUretici
{
    public class RehberKaydi
    {
        public string Id { get; set; }
        public string Bolum { get; set; }
        public string Kategori { get; set; }
        public List<string> Anahtarlar { get; set; } = new List<string>();
        public string Metin { get; set; }
        public string Kaynak { get; set; }
        public string Guncelleme { get; set; }

        public string Slug { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public List<RehberKaydi> Ilgili { get; set; } = new List<RehberKaydi>();

        public string Url => "/rehber/" + Bolum + "/" + Slug + ".html";
    }

    public class UretimSonucu
    {
        public List<RehberKaydi> Kayitlar { get; set; } = new List<RehberKaydi>();
        public List<string> Uyarilar { get; set; } = new List<string>();
        public List<string> Dosyalar { get; set; } = new List<string>();
    }
}
