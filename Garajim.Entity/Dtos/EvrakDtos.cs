using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class EvrakCreateDto
    {
        public int? VehicleId { get; set; }
        public int? UserId { get; set; }
        public EvrakTuru EvrakTuru { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public string Saglayici { get; set; }
        public string PoliceNo { get; set; }
        public string Not { get; set; }
        public int? DocumentId { get; set; }
    }

    public class EvrakUpdateDto
    {
        public EvrakTuru EvrakTuru { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public string Saglayici { get; set; }
        public string PoliceNo { get; set; }
        public string Not { get; set; }
        public int? DocumentId { get; set; }
    }

    public class TakvimAbonelikDto
    {
        public string Url { get; set; }
    }

    public class EvrakDto
    {
        public int Id { get; set; }
        public int? VehicleId { get; set; }
        public string Plaka { get; set; }
        public int? UserId { get; set; }
        public string KullaniciAdi { get; set; }
        public string EvrakTuru { get; set; }
        public string EvrakAdi { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public string Saglayici { get; set; }
        public string PoliceNo { get; set; }
        public string Not { get; set; }
        public int? DocumentId { get; set; }
        public bool Aktif { get; set; }
        public string Durum { get; set; }
        public int KalanGun { get; set; }
    }
}
