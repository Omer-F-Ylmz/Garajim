using Garajim.Business.Concrete;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Unit
{
    public class FisDogrulukOlcumuTests
    {
        private static ReceiptDraft Taslak(bool otoOnay, string duzeltilen, double guven)
        {
            return new ReceiptDraft
            {
                Durum = ReceiptDraftStatus.Onaylandi,
                OtoOnaylandi = otoOnay,
                DuzeltilenAlanlar = duzeltilen,
                GuvenSkoru = guven
            };
        }

        [Fact]
        public void BosTaslaklarPaydaDisindadir()
        {
            var taslaklar = new List<ReceiptDraft>
            {
                Taslak(false, null, 0.98),
                Taslak(false, "tarih", 0.95),
                Taslak(false, null, 0),
                Taslak(false, null, 0)
            };

            Assert.Equal(50, FisDogrulugu.Oran(taslaklar));
        }

        [Fact]
        public void OtoOnaylananlarSayilmaz()
        {
            var taslaklar = new List<ReceiptDraft>
            {
                Taslak(true, null, 0.99),
                Taslak(false, "tutar", 0.9)
            };

            Assert.Equal(0, FisDogrulugu.Oran(taslaklar));
        }

        [Fact]
        public void OlcecekTaslakYoksaSifirDoner()
        {
            Assert.Equal(0, FisDogrulugu.Oran(new List<ReceiptDraft>()));
            Assert.Equal(0, FisDogrulugu.Oran(new List<ReceiptDraft> { Taslak(false, null, 0) }));
        }

        [Fact]
        public void OlculebilirSayisiRaporlanir()
        {
            var taslaklar = new List<ReceiptDraft>
            {
                Taslak(false, null, 0.98),
                Taslak(false, null, 0),
                Taslak(true, null, 0.99)
            };

            Assert.Equal(1, FisDogrulugu.OlculenSayisi(taslaklar));
        }
    }
}
