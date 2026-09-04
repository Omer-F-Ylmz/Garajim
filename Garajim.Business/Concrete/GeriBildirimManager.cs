using System.Text;
using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Business.Katalog;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Concrete
{
    public class GeriBildirimManager : IGeriBildirimService
    {
        public const int GunlukSinir = 5;
        public const int ListeSiniri = 20;

        private readonly IGeriBildirimDal _geriBildirimDal;
        private readonly IUserDal _userDal;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeriBildirimManager> _logger;

        public GeriBildirimManager(
            IGeriBildirimDal geriBildirimDal,
            IUserDal userDal,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<GeriBildirimManager> logger = null)
        {
            _geriBildirimDal = geriBildirimDal;
            _userDal = userDal;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger ?? NullLogger<GeriBildirimManager>.Instance;
        }

        public async Task<IResult> EkleAsync(int userId, GeriBildirimCreateDto dto)
        {
            if (dto == null || !Enum.IsDefined(dto.Tur))
                return new ErrorResult(Messages.InvalidValue);

            var mesaj = (dto.Mesaj ?? string.Empty).Trim();
            if (mesaj.Length == 0)
                return new ErrorResult(Messages.GeriBildirimBos);

            if (!UygunsuzIfadeFiltresi.Varsayilan.Temiz(mesaj))
                return new ErrorResult(Messages.UygunsuzIfade);

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            var gunBasi = Saat.GunBasiUtc();
            var bugunkuler = await _geriBildirimDal.CountAsync(g => g.UserId == userId && g.Tarih >= gunBasi);
            if (bugunkuler >= GunlukSinir)
                return new ErrorResult(Messages.GeriBildirimGunlukSinir);

            var kayit = new GeriBildirim
            {
                CompanyId = user.CompanyId,
                UserId = userId,
                Tur = dto.Tur,
                Mesaj = MetinSinirlari.Kirp(mesaj, 1000),
                Sayfa = MetinSinirlari.Kirp(dto.Sayfa, 60),
                Surum = MetinSinirlari.Kirp(dto.Surum, 60),
                Tarih = DateTime.UtcNow
            };

            await _geriBildirimDal.AddAsync(kayit);

            await DestegeBildirAsync(kayit, user);

            return new SuccessResult(Messages.GeriBildirimAlindi);
        }

        public async Task<IDataResult<List<GeriBildirimDto>>> SonlariAsync(int limit)
        {
            var kayitlar = await _geriBildirimDal.SonlariAsync(limit <= 0 ? ListeSiniri : limit);
            var kullanicilar = await _userDal.GetListAsync(u => u.Id > 0);

            var liste = kayitlar.Select(k => new GeriBildirimDto
            {
                Id = k.Id,
                Tur = k.Tur.ToString(),
                Mesaj = k.Mesaj,
                Sayfa = k.Sayfa,
                Surum = k.Surum,
                KullaniciAdi = kullanicilar.FirstOrDefault(u => u.Id == k.UserId)?.FullName,
                Tarih = k.Tarih
            }).ToList();

            return new SuccessDataResult<List<GeriBildirimDto>>(liste);
        }

        private async Task DestegeBildirAsync(GeriBildirim kayit, AppUser user)
        {
            var destek = (_configuration["App:DestekEposta"] ?? string.Empty).Trim();
            if (destek.Length == 0)
                return;

            var govde = new StringBuilder();
            govde.AppendLine("Yeni geri bildirim.");
            govde.AppendLine();
            govde.AppendLine("Tür: " + TurAdi(kayit.Tur));
            govde.AppendLine("Gönderen: " + user.FullName + " (" + user.Email + ")");
            govde.AppendLine("Sayfa: " + (kayit.Sayfa ?? "-"));
            govde.AppendLine("Sürüm: " + (kayit.Surum ?? "-"));
            govde.AppendLine();
            govde.AppendLine(kayit.Mesaj);

            try
            {
                await _emailSender.SendAsync(destek, "Garajım geri bildirim: " + TurAdi(kayit.Tur), govde.ToString());
            }
            catch (Exception hata)
            {
                _logger.LogWarning(hata, "Geri bildirim e-postası gönderilemedi.");
            }
        }

        private static string TurAdi(GeriBildirimTuru tur)
        {
            return tur switch
            {
                GeriBildirimTuru.Hata => "Hata",
                GeriBildirimTuru.Oneri => "Öneri",
                _ => "Diğer"
            };
        }
    }
}
