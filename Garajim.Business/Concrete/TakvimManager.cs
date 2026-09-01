using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Constants;
using Garajim.Core.Multitenancy;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class TakvimManager : ITakvimService
    {
        private const int HatirlatmaGunOncesi = 7;

        private readonly ITakvimAbonelikDal _abonelikDal;
        private readonly IUserDal _userDal;
        private readonly IEvrakDal _evrakDal;
        private readonly IReminderDal _reminderDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly TenantContext _tenantContext;
        private readonly IConfiguration _configuration;

        public TakvimManager(
            ITakvimAbonelikDal abonelikDal,
            IUserDal userDal,
            IEvrakDal evrakDal,
            IReminderDal reminderDal,
            IVehicleDal vehicleDal,
            IVehicleAccessService vehicleAccess,
            TenantContext tenantContext,
            IConfiguration configuration)
        {
            _abonelikDal = abonelikDal;
            _userDal = userDal;
            _evrakDal = evrakDal;
            _reminderDal = reminderDal;
            _vehicleDal = vehicleDal;
            _vehicleAccess = vehicleAccess;
            _tenantContext = tenantContext;
            _configuration = configuration;
        }

        public async Task<IDataResult<TakvimAbonelikDto>> AbonelikOlusturAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<TakvimAbonelikDto>(Messages.UserNotFound);

            await _abonelikDal.PasiflestirAsync(userId);

            var hamToken = HamTokenUret();

            await _abonelikDal.AddAsync(new TakvimAbonelik
            {
                CompanyId = user.CompanyId,
                UserId = userId,
                TokenHash = Hashle(hamToken),
                Aktif = true,
                OlusturmaTarihi = DateTime.UtcNow
            });

            var taban = (_configuration["App:BaseUrl"] ?? string.Empty).TrimEnd('/');
            return new SuccessDataResult<TakvimAbonelikDto>(new TakvimAbonelikDto
            {
                Url = $"{taban}/api/takvim/{hamToken}.ics"
            }, Messages.TakvimAbonelikCreated);
        }

        public async Task<IResult> AbonelikKapatAsync(int userId)
        {
            await _abonelikDal.PasiflestirAsync(userId);
            return new SuccessResult(Messages.TakvimAbonelikClosed);
        }

        public async Task<IDataResult<string>> IcsAsync(string token)
        {
            var abonelik = string.IsNullOrWhiteSpace(token)
                ? null
                : await _abonelikDal.GetByTokenHashAsync(Hashle(token));

            if (abonelik == null)
                return new ErrorDataResult<string>(Messages.TakvimAbonelikNotFound);

            using var kapsam = SystemScope.For(_tenantContext, abonelik.CompanyId);

            var araclar = await _vehicleAccess.GetAccessibleListAsync(abonelik.UserId);
            var aracIdleri = araclar.Select(a => a.Id).ToHashSet();
            var plakalar = araclar.ToDictionary(a => a.Id, a => a.Plate);

            var evraklar = await _evrakDal.GetListAsync(e => e.Aktif);
            var hatirlatmalar = await _reminderDal.GetListAsync(r => !r.IsCompleted && r.DueDate != null);

            var sb = new StringBuilder();
            sb.Append("BEGIN:VCALENDAR\r\n");
            sb.Append("VERSION:2.0\r\n");
            sb.Append("PRODID:-//Garajim//Arac Takvimi//TR\r\n");
            sb.Append("CALSCALE:GREGORIAN\r\n");
            sb.Append("METHOD:PUBLISH\r\n");
            sb.Append("X-WR-CALNAME:Garajım araç takvimi\r\n");

            foreach (var evrak in evraklar.OrderBy(e => e.BitisTarihi))
            {
                if (evrak.VehicleId != null && !aracIdleri.Contains(evrak.VehicleId.Value))
                {
                    continue;
                }

                if (evrak.UserId != null && evrak.UserId != abonelik.UserId)
                {
                    continue;
                }

                var plaka = evrak.VehicleId != null && plakalar.TryGetValue(evrak.VehicleId.Value, out var p) ? p : null;
                var ad = EvrakAdlari.Ad(evrak.EvrakTuru);
                var ozet = plaka == null ? Buyuk(ad) : $"{plaka} · {Buyuk(ad)}";

                Olay(sb, $"evrak-{evrak.Id}@garajim", evrak.BitisTarihi, ozet, $"{Buyuk(ad)} bitiş tarihi.");
            }

            foreach (var hatirlatma in hatirlatmalar.OrderBy(r => r.DueDate))
            {
                if (!aracIdleri.Contains(hatirlatma.VehicleId))
                {
                    continue;
                }

                var plaka = plakalar.TryGetValue(hatirlatma.VehicleId, out var p) ? p : null;
                var ad = HatirlatmaAdi(hatirlatma.Type);
                var ozet = plaka == null ? Buyuk(ad) : $"{plaka} · {Buyuk(ad)}";

                Olay(sb, $"hatirlatma-{hatirlatma.Id}@garajim", hatirlatma.DueDate.Value, ozet, hatirlatma.Note ?? Buyuk(ad));
            }

            sb.Append("END:VCALENDAR\r\n");

            return new SuccessDataResult<string>(sb.ToString());
        }

        private static void Olay(StringBuilder sb, string uid, DateTime tarih, string ozet, string aciklama)
        {
            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append($"UID:{uid}\r\n");
            sb.Append($"DTSTAMP:{tarih:yyyyMMdd}T000000Z\r\n");
            sb.Append($"DTSTART;VALUE=DATE:{tarih:yyyyMMdd}\r\n");
            sb.Append($"DTEND;VALUE=DATE:{tarih.AddDays(1):yyyyMMdd}\r\n");
            sb.Append($"SUMMARY:{Kacir(ozet)}\r\n");
            sb.Append($"DESCRIPTION:{Kacir(aciklama)}\r\n");
            sb.Append("BEGIN:VALARM\r\n");
            sb.Append("TRIGGER:-P7D\r\n");
            sb.Append("ACTION:DISPLAY\r\n");
            sb.Append($"DESCRIPTION:{Kacir(ozet)}\r\n");
            sb.Append("END:VALARM\r\n");
            sb.Append("END:VEVENT\r\n");
        }

        private static string Kacir(string metin)
        {
            return (metin ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");
        }

        private static string Buyuk(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return metin;
            }

            var kultur = CultureInfo.GetCultureInfo("tr-TR");
            return char.ToUpper(metin[0], kultur) + metin.Substring(1);
        }

        private static string HatirlatmaAdi(Entity.Enums.ReminderType tur)
        {
            return tur switch
            {
                Entity.Enums.ReminderType.Muayene => "araç muayenesi",
                Entity.Enums.ReminderType.TrafikSigortasi => "trafik sigortası",
                Entity.Enums.ReminderType.Kasko => "kasko yenileme",
                Entity.Enums.ReminderType.EgzozEmisyon => "egzoz emisyon ölçümü",
                Entity.Enums.ReminderType.Mtv => "MTV ödemesi",
                Entity.Enums.ReminderType.PeriyodikBakim => "periyodik bakım",
                Entity.Enums.ReminderType.LastikDegisimi => "lastik değişimi",
                _ => "hatırlatma"
            };
        }

        private static string HamTokenUret()
        {
            var bayt = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bayt).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string Hashle(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }
    }
}
