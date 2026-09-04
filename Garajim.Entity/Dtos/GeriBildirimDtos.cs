using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class GeriBildirimCreateDto
    {
        public GeriBildirimTuru Tur { get; set; }
        public string Mesaj { get; set; }
        public string Sayfa { get; set; }
        public string Surum { get; set; }
    }

    public class GeriBildirimDto
    {
        public int Id { get; set; }
        public string Tur { get; set; }
        public string Mesaj { get; set; }
        public string Sayfa { get; set; }
        public string Surum { get; set; }
        public string KullaniciAdi { get; set; }
        public DateTime Tarih { get; set; }
    }
}
