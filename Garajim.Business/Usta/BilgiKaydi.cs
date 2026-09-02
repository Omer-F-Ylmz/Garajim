namespace Garajim.Business.Usta
{
    public class BilgiKaydi
    {
        public string Id { get; set; }
        public string Kategori { get; set; }
        public List<string> Anahtarlar { get; set; } = new List<string>();
        public string Metin { get; set; }
        public string Kaynak { get; set; }
        public string Guncelleme { get; set; }
    }
}
