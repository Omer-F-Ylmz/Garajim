using Microsoft.ML;
using Microsoft.ML.Data;

namespace Garajim.ML.Models
{
    public class FiyatModeliSozlugu
    {
        public const string MarkaSutunu = "MarkaEncoded";
        public const string SeriSutunu = "SeriEncoded";
        public const string KasaSutunu = "KasaTipiEncoded";

        private readonly HashSet<string> _markalar;
        private readonly HashSet<string> _seriler;
        private readonly HashSet<string> _kasalar;

        private FiyatModeliSozlugu(HashSet<string> markalar, HashSet<string> seriler, HashSet<string> kasalar)
        {
            _markalar = markalar;
            _seriler = seriler;
            _kasalar = kasalar;
        }

        public int MarkaSayisi => _markalar.Count;

        public int SeriSayisi => _seriler.Count;

        public int KasaSayisi => _kasalar.Count;

        public IReadOnlyCollection<string> Markalar => _markalar;

        public IReadOnlyCollection<string> Seriler => _seriler;

        public IReadOnlyCollection<string> Kasalar => _kasalar;

        public static FiyatModeliSozlugu Yukle(string modelYolu)
        {
            if (string.IsNullOrWhiteSpace(modelYolu) || !File.Exists(modelYolu))
            {
                return new FiyatModeliSozlugu(new HashSet<string>(), new HashSet<string>(), new HashSet<string>());
            }

            var context = new MLContext();
            var model = context.Model.Load(modelYolu, out var girdiSemasi);

            var bosGorunum = context.Data.LoadFromEnumerable(new List<CarPriceInput>());
            var semali = model.Transform(bosGorunum).Schema;

            return new FiyatModeliSozlugu(SlotAdlari(semali, MarkaSutunu), SlotAdlari(semali, SeriSutunu), SlotAdlari(semali, KasaSutunu));
        }

        public bool MarkaTaniniyor(string marka)
        {
            return _markalar.Count == 0 || (!string.IsNullOrWhiteSpace(marka) && _markalar.Contains(marka.Trim()));
        }

        public bool SeriTaniniyor(string seri)
        {
            return _seriler.Count == 0 || (!string.IsNullOrWhiteSpace(seri) && _seriler.Contains(seri.Trim()));
        }

        public bool KasaTaniniyor(string kasa)
        {
            return _kasalar.Count == 0 || (!string.IsNullOrWhiteSpace(kasa) && _kasalar.Contains(kasa.Trim()));
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
