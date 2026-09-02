using Microsoft.ML;
using Microsoft.ML.Data;

namespace Garajim.ML.Models
{
    public class FiyatModeliSozlugu
    {
        public const string MarkaSutunu = "MarkaEncoded";
        public const string SeriSutunu = "SeriEncoded";

        private readonly HashSet<string> _markalar;
        private readonly HashSet<string> _seriler;

        private FiyatModeliSozlugu(HashSet<string> markalar, HashSet<string> seriler)
        {
            _markalar = markalar;
            _seriler = seriler;
        }

        public int MarkaSayisi => _markalar.Count;

        public int SeriSayisi => _seriler.Count;

        public static FiyatModeliSozlugu Yukle(string modelYolu)
        {
            if (string.IsNullOrWhiteSpace(modelYolu) || !File.Exists(modelYolu))
            {
                return new FiyatModeliSozlugu(new HashSet<string>(), new HashSet<string>());
            }

            var context = new MLContext();
            var model = context.Model.Load(modelYolu, out var girdiSemasi);

            var bosGorunum = context.Data.LoadFromEnumerable(new List<CarPriceInput>());
            var semali = model.Transform(bosGorunum).Schema;

            return new FiyatModeliSozlugu(SlotAdlari(semali, MarkaSutunu), SlotAdlari(semali, SeriSutunu));
        }

        public bool MarkaTaniniyor(string marka)
        {
            return _markalar.Count == 0 || (!string.IsNullOrWhiteSpace(marka) && _markalar.Contains(marka.Trim()));
        }

        public bool SeriTaniniyor(string seri)
        {
            return _seriler.Count == 0 || (!string.IsNullOrWhiteSpace(seri) && _seriler.Contains(seri.Trim()));
        }

        private static HashSet<string> SlotAdlari(DataViewSchema sema, string sutunAdi)
        {
            var adlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sutun = sema.GetColumnOrNull(sutunAdi);

            if (sutun == null || !sutun.Value.HasSlotNames())
            {
                return adlar;
            }

            VBuffer<ReadOnlyMemory<char>> tampon = default;
            sutun.Value.GetSlotNames(ref tampon);

            foreach (var deger in tampon.DenseValues())
            {
                var ad = deger.ToString().Trim();
                if (ad.Length > 0)
                {
                    adlar.Add(ad);
                }
            }

            return adlar;
        }
    }
}
