using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Planlar;
using Garajim.Dal.Abstract;
using Garajim.Entity.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Concrete
{
    public class AiButcesi : IAiButcesi
    {
        private readonly IAiTokenDal _tokenDal;
        private readonly PlanKurallari _planKurallari;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiButcesi> _logger;

        public AiButcesi(
            IAiTokenDal tokenDal,
            PlanKurallari planKurallari,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<AiButcesi> logger = null)
        {
            _tokenDal = tokenDal;
            _planKurallari = planKurallari;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger ?? NullLogger<AiButcesi>.Instance;
        }

        public async Task<AiButceDurumuDto> DurumAsync()
        {
            var bugun = Saat.BugunTr();
            var tavan = _planKurallari.AylikTokenTavani();
            var satir = await _tokenDal.AyiAlAsync(bugun.Year, bugun.Month);
            var kullanilan = satir == null ? 0L : satir.TokenGiris + satir.TokenCikis;

            return new AiButceDurumuDto
            {
                Tavan = tavan,
                Kullanilan = kullanilan,
                Kalan = tavan <= 0 ? -1 : Math.Max(0, tavan - kullanilan),
                Asildi = tavan > 0 && kullanilan >= tavan
            };
        }

        public async Task<bool> AsildiMiAsync()
        {
            var durum = await DurumAsync();

            if (!durum.Asildi)
            {
                return false;
            }

            await BildirAsync(durum);

            return true;
        }

        public async Task KaydetAsync(int giris, int cikis)
        {
            var bugun = Saat.BugunTr();
            await _tokenDal.EkleAsync(bugun.Year, bugun.Month, giris, cikis);
        }

        private async Task BildirAsync(AiButceDurumuDto durum)
        {
            var bugun = Saat.BugunTr();
            var destek = (_configuration["App:DestekEposta"] ?? string.Empty).Trim();

            if (destek.Length == 0)
            {
                return;
            }

            if (!await _tokenDal.BildirimiIsaretleAsync(bugun.Year, bugun.Month))
            {
                return;
            }

            try
            {
                await _emailSender.SendAsync(
                    destek,
                    "Garajım | AI aylık token tavanı aşıldı",
                    $"{bugun:MM/yyyy} ayında AI token tavanı aşıldı. Tavan: {durum.Tavan}, kullanılan: {durum.Kullanilan}. "
                    + "Fiş okuma ve AI Usta uçları ayın sonuna kadar 503 dönüyor.");
            }
            catch (Exception hata)
            {
                _logger.LogError(hata, "AI bütçe bildirimi gönderilemedi.");
            }
        }
    }
}
