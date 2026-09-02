using Garajim.Entity.Dtos;

namespace Garajim.Business.Usta
{
    public class SahteUstaIstemcisi : IUstaIstemci
    {
        public Task<UstaIstemciSonucu> SorAsync(string sabitBlok, string aracBaglami, IReadOnlyList<(string Rol, string Metin)> gecmis, string soru, CancellationToken ct)
        {
            var yanit = new UstaYanitDto
            {
                Ozet = "Anlattığın belirti fren tarafını işaret ediyor. Aşağıda olasılıkları sıraladım; kesin sonuç için ustaya gösterilmeli.",
                KirmiziCizgi = false,
                Kademeler = new List<UstaKademeDto>
                {
                    new UstaKademeDto
                    {
                        Kademe = "EnSik",
                        Neden = "Ön fren balatası aşınmış",
                        BelirtiUyumu = "Frenlemede metalik ses en sık balata bitiminde duyulur.",
                        EvdeKontrol = "Jant aralığından balata kalınlığına bak; 3 mm altındaysa değişmeli.",
                        MaliyetTl = new List<decimal> { 1500m, 3500m },
                        Aciliyet = "BuHafta"
                    },
                    new UstaKademeDto
                    {
                        Kademe = "Sik",
                        Neden = "Fren diski salgı yapmış",
                        BelirtiUyumu = "Sesle birlikte titreme de varsa disk öne çıkar.",
                        EvdeKontrol = "Disk yüzeyinde derin iz ya da basamak var mı bak.",
                        MaliyetTl = new List<decimal> { 3000m, 7000m },
                        Aciliyet = "Bakimda"
                    },
                    new UstaKademeDto
                    {
                        Kademe = "Nadir",
                        Neden = "Kaliper pistonu sıkışmış",
                        BelirtiUyumu = "Tek taraf ısınıyor ve araç frende çekiyorsa uyumlu.",
                        EvdeKontrol = "Kısa sürüşten sonra jantlardan biri diğerinden çok sıcaksa şüphelen.",
                        MaliyetTl = new List<decimal> { 2500m, 6000m },
                        Aciliyet = "Bugun"
                    }
                },
                AracVerisindenNotlar = new List<string>
                {
                    "Bu yanıt geliştirme modunda üretildi (Usta__SahteYanit=true); gerçek model çağrılmadı."
                },
                UstayaBoyleAnlat = "Frende metalik ses var, hızla artıyor. Ön balata ve disk kontrol edilsin.",
                TakipSorulari = new List<string>
                {
                    "Ses yalnız frene basınca mı çıkıyor?",
                    "Direksiyonda titreme de var mı?"
                },
                Uyari = UstaYanitDenetleyici.VarsayilanUyari
            };

            return Task.FromResult(new UstaIstemciSonucu
            {
                Yanit = yanit,
                TokenGiris = 1180,
                TokenCikis = 320,
                SureMs = 35
            });
        }
    }
}
