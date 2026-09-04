using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class ReceiptDraftDto
    {
        public int Id { get; set; }
        public int? VehicleId { get; set; }
        public string Durum { get; set; }
        public string OrijinalAd { get; set; }
        public DateTime? Tarih { get; set; }
        public decimal? ToplamTutar { get; set; }
        public decimal? KdvTutari { get; set; }
        public decimal? Litre { get; set; }
        public decimal? BirimFiyat { get; set; }
        public string Plaka { get; set; }
        public int? Km { get; set; }
        public string TahminiTur { get; set; }
        public double GuvenSkoru { get; set; }
        public string DuzeltilenAlanlar { get; set; }
        public bool OtoOnaylandi { get; set; }
        public string AtlamaNedeni { get; set; }
        public string CikarimHatasi { get; set; }
        public List<MaintenancePartDto> Parcalar { get; set; } = new List<MaintenancePartDto>();
        public DateTime OlusturmaTarihi { get; set; }
    }

    public class ReceiptUploadResultDto
    {
        public int TaslakId { get; set; }
        public string Durum { get; set; }
        public string AtlamaNedeni { get; set; }
        public string CikarimHatasi { get; set; }
        public OlusturulanKayitDto OlusturulanKayit { get; set; }
        public ReceiptDraftDto Taslak { get; set; }
    }

    public class OlusturulanKayitDto
    {
        public string Tur { get; set; }
        public int Id { get; set; }
    }

    public class ReceiptUploadDto
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }
    }

    public class ReceiptConfirmDto
    {
        public int VehicleId { get; set; }
        public ReceiptType Tur { get; set; }
        public DateTime Tarih { get; set; }
        public decimal Tutar { get; set; }
        public int? Km { get; set; }
        public decimal? Litre { get; set; }
        public decimal? BirimFiyat { get; set; }
        public MaintenanceType? BakimTuru { get; set; }
        public ExpenseCategory? MasrafKategorisi { get; set; }
        public string ServisAdi { get; set; }
        public string Not { get; set; }
        public List<MaintenancePartDto> Parcalar { get; set; }
    }
}
