using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class ReceiptDraft : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? VehicleId { get; set; }
        public int YukleyenUserId { get; set; }
        public string DosyaYolu { get; set; }
        public string OrijinalAd { get; set; }
        public string IcerikTipi { get; set; }
        public long BoyutBayt { get; set; }
        public ReceiptDraftStatus Durum { get; set; }
        public DateTime? Tarih { get; set; }
        public decimal? ToplamTutar { get; set; }
        public decimal? KdvTutari { get; set; }
        public decimal? Litre { get; set; }
        public decimal? BirimFiyat { get; set; }
        public string Plaka { get; set; }
        public int? Km { get; set; }
        public ReceiptType TahminiTur { get; set; }
        public double GuvenSkoru { get; set; }
        public string DuzeltilenAlanlar { get; set; }
        public bool OtoOnaylandi { get; set; }
        public string AtlamaNedeni { get; set; }
        public string Saglayici { get; set; }
        public int SureMs { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
