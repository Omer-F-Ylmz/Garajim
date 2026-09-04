using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class ReceiptExtractionResult
    {
        public DateTime? Tarih { get; set; }
        public decimal? ToplamTutar { get; set; }
        public decimal? KdvTutari { get; set; }
        public decimal? Litre { get; set; }
        public decimal? BirimFiyat { get; set; }
        public string Plaka { get; set; }
        public int? Km { get; set; }
        public ReceiptType TahminiTur { get; set; } = ReceiptType.Bilinmiyor;
        public List<ReceiptItemResult> KalemListesi { get; set; } = new List<ReceiptItemResult>();
        public double GuvenSkoru { get; set; }
        public string HamYanit { get; set; }
        public int TokenGiris { get; set; }
        public int TokenCikis { get; set; }
        public bool HizmetDolu { get; set; }
    }

    public class ReceiptItemResult
    {
        public string Ad { get; set; }
        public decimal? Tutar { get; set; }
    }
}
