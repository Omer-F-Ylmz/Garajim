using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public static class FisDogrulugu
    {
        public static List<ReceiptDraft> Olculenler(IEnumerable<ReceiptDraft> taslaklar)
        {
            return taslaklar
                .Where(t => t.Durum == ReceiptDraftStatus.Onaylandi && !t.OtoOnaylandi && t.GuvenSkoru > 0)
                .ToList();
        }

        public static int OlculenSayisi(IEnumerable<ReceiptDraft> taslaklar)
        {
            return Olculenler(taslaklar).Count;
        }

        public static double Oran(IEnumerable<ReceiptDraft> taslaklar)
        {
            var olculenler = Olculenler(taslaklar);

            if (olculenler.Count == 0)
            {
                return 0;
            }

            var duzeltmesiz = olculenler.Count(t => string.IsNullOrWhiteSpace(t.DuzeltilenAlanlar));

            return Math.Round(duzeltmesiz * 100d / olculenler.Count, 1);
        }
    }
}
