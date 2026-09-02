using System.Globalization;
using System.Text;
using System.Text.Json;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Constants;
using Garajim.Business.Usta;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class UstaManager : IUstaService
    {
        public const int SohbetMesajLimiti = 12;
        public const int MaxSoruUzunlugu = 1000;
        public const int GecmisMesajSayisi = 6;
        public const string VarsayilanOnaySurumu = "2026-09-v1";
        private const int VarsayilanBireyselLimit = 20;
        private const int VarsayilanFiloLimit = 100;
        private const int CozumBakimGunu = 90;

        private static readonly JsonSerializerOptions JsonSecenekleri = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IUstaSohbetDal _sohbetDal;
        private readonly IUstaMesajDal _mesajDal;
        private readonly IUstaOnayDal _onayDal;
        private readonly IUstaCozumOzetiDal _cozumOzetiDal;
        private readonly IUserDal _userDal;
        private readonly ICompanyDal _companyDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IMaintenancePartDal _partDal;
        private readonly IPartMemoryService _partMemory;
        private readonly IEvrakDal _evrakDal;
        private readonly IFuelDal _fuelDal;
        private readonly IReminderDal _reminderDal;
        private readonly IUstaIstemci _istemci;
        private readonly UstaBilgiDeposu _bilgi;
        private readonly EvrakKurallari _evrakKurallari;
        private readonly IConfiguration _configuration;

        public UstaManager(
            IUstaSohbetDal sohbetDal,
            IUstaMesajDal mesajDal,
            IUstaOnayDal onayDal,
            IUstaCozumOzetiDal cozumOzetiDal,
            IUserDal userDal,
            ICompanyDal companyDal,
            IVehicleAccessService vehicleAccess,
            IMaintenanceDal maintenanceDal,
            IMaintenancePartDal partDal,
            IPartMemoryService partMemory,
            IEvrakDal evrakDal,
            IFuelDal fuelDal,
            IReminderDal reminderDal,
            IUstaIstemci istemci,
            UstaBilgiDeposu bilgi,
            EvrakKurallari evrakKurallari,
            IConfiguration configuration)
        {
            _sohbetDal = sohbetDal;
            _mesajDal = mesajDal;
            _onayDal = onayDal;
            _cozumOzetiDal = cozumOzetiDal;
            _userDal = userDal;
            _companyDal = companyDal;
            _vehicleAccess = vehicleAccess;
            _maintenanceDal = maintenanceDal;
            _partDal = partDal;
            _partMemory = partMemory;
            _evrakDal = evrakDal;
            _fuelDal = fuelDal;
            _reminderDal = reminderDal;
            _istemci = istemci;
            _bilgi = bilgi;
            _evrakKurallari = evrakKurallari;
            _configuration = configuration;
        }

        public string GuncelOnaySurumu()
        {
            var surum = _configuration["Usta:OnaySurumu"];
            return string.IsNullOrWhiteSpace(surum) ? VarsayilanOnaySurumu : surum.Trim();
        }

        public async Task<IDataResult<UstaOnayDurumDto>> OnayDurumuAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<UstaOnayDurumDto>(Messages.UserNotFound);

            var guncel = GuncelOnaySurumu();
            var onay = await _onayDal.GetAsync(o => o.UserId == userId && o.MetinSurumu == guncel);

            return new SuccessDataResult<UstaOnayDurumDto>(new UstaOnayDurumDto
            {
                OnayGerekli = onay == null,
                GuncelSurum = guncel,
                KabulEdilenSurum = onay?.MetinSurumu,
                KabulTarihi = onay?.KabulTarihi,
                MetinBagi = "/sartlar.html"
            });
        }

        public async Task<IResult> OnayVerAsync(int userId, UstaOnayVerDto dto)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            var guncel = GuncelOnaySurumu();
            if (!string.Equals(dto?.MetinSurumu?.Trim(), guncel, StringComparison.Ordinal))
                return new ErrorResult(Messages.UstaOnaySurumuEski);

            var mevcut = await _onayDal.GetAsync(o => o.UserId == userId && o.MetinSurumu == guncel);
            if (mevcut != null)
                return new SuccessResult(Messages.UstaOnayAlindi);

            await _onayDal.AddAsync(new UstaOnay
            {
                CompanyId = user.CompanyId,
                UserId = userId,
                MetinSurumu = guncel,
                KabulTarihi = DateTime.UtcNow
            });

            return new SuccessResult(Messages.UstaOnayAlindi);
        }

        public async Task<IDataResult<UstaSohbetDto>> SohbetOlusturAsync(int userId, UstaSohbetOlusturDto dto)
        {
            var kapi = await KapiAsync(userId);
            if (kapi.Hata != null)
                return new ErrorDataResult<UstaSohbetDto>(kapi.Hata);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<UstaSohbetDto>(Messages.VehicleNotFound);

            var sohbet = new UstaSohbet
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                UserId = userId,
                Baslik = Kirp(vehicle.Plate + " · " + DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), 120),
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _sohbetDal.AddAsync(sohbet);

            return new SuccessDataResult<UstaSohbetDto>(MapSohbet(sohbet, vehicle.Plate, 0), Messages.UstaSohbetOlusturuldu);
        }

        public async Task<IDataResult<UstaMesajSonucDto>> MesajGonderAsync(int userId, int sohbetId, UstaMesajGonderDto dto, CancellationToken ct)
        {
            var kapi = await KapiAsync(userId);
            if (kapi.Hata != null)
                return new ErrorDataResult<UstaMesajSonucDto>(kapi.Hata);

            var metin = (dto?.Metin ?? string.Empty).Trim();
            if (metin.Length == 0 || metin.Length > MaxSoruUzunlugu)
                return new ErrorDataResult<UstaMesajSonucDto>(Messages.InvalidValue);

            var sohbet = await _sohbetDal.GetAsync(s => s.Id == sohbetId);
            if (sohbet == null)
                return new ErrorDataResult<UstaMesajSonucDto>(Messages.UstaSohbetBulunamadi);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, sohbet.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<UstaMesajSonucDto>(Messages.UstaSohbetBulunamadi);

            var gunlukLimit = GunlukLimit(kapi.Sirket.PlanType);
            var bugunkuSayi = await _mesajDal.KullaniciGunlukSayisiAsync(userId, DateTime.UtcNow.Date);
            if (bugunkuSayi >= gunlukLimit)
                return new ErrorDataResult<UstaMesajSonucDto>(Messages.UstaGunlukLimit);

            var sohbetSayisi = await _mesajDal.SohbetMesajSayisiAsync(sohbetId);
            if (sohbetSayisi >= SohbetMesajLimiti)
                return new ErrorDataResult<UstaMesajSonucDto>(Messages.UstaSohbetLimiti);

            var kirmizi = KirmiziCizgiler.Bul(metin);
            UstaYanitDto yanit;
            string bilgiKategorisi = null;
            var tokenGiris = 0;
            var tokenCikis = 0;
            var sureMs = 0;

            if (kirmizi != null)
            {
                yanit = UstaYanitDenetleyici.KirmiziCizgiYaniti(kirmizi);
            }
            else
            {
                var baglam = await AracBaglamiAsync(userId, vehicle);
                var secilen = _bilgi.Secici.Sec(metin);
                bilgiKategorisi = secilen.Count > 0 ? secilen[0].Kategori : null;
                var sabitBlok = _bilgi.SabitBlok(secilen, await GarajimVerisiAsync(vehicle));
                var gecmis = await GecmisAsync(sohbetId);

                var sonuc = await _istemci.SorAsync(sabitBlok, baglam, gecmis, metin, ct);
                tokenGiris = sonuc.TokenGiris;
                tokenCikis = sonuc.TokenCikis;
                sureMs = sonuc.SureMs;

                if (sonuc.Hata != null || !UstaYanitDenetleyici.Gecerli(sonuc.Yanit, out _))
                {
                    return new ErrorDataResult<UstaMesajSonucDto>(Messages.UstaYanitAlinamadi);
                }

                yanit = UstaYanitDenetleyici.SonFiltre(sonuc.Yanit);
                yanit.KirmiziCizgi = false;
            }

            await _mesajDal.AddAsync(new UstaMesaj
            {
                CompanyId = sohbet.CompanyId,
                SohbetId = sohbet.Id,
                Rol = UstaRol.Kullanici,
                Metin = metin,
                GeriBildirim = UstaGeriBildirim.Yok,
                OlusturmaTarihi = DateTime.UtcNow
            });

            var ustaMesaji = new UstaMesaj
            {
                CompanyId = sohbet.CompanyId,
                SohbetId = sohbet.Id,
                Rol = UstaRol.Usta,
                Metin = Kirp(yanit.Ozet, 4000),
                YapiliYanit = JsonSerializer.Serialize(yanit, JsonSecenekleri),
                KirmiziCizgi = yanit.KirmiziCizgi,
                BilgiKategorisi = bilgiKategorisi,
                TokenGiris = tokenGiris,
                TokenCikis = tokenCikis,
                SureMs = sureMs,
                GeriBildirim = UstaGeriBildirim.Yok,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _mesajDal.AddAsync(ustaMesaji);

            return new SuccessDataResult<UstaMesajSonucDto>(new UstaMesajSonucDto
            {
                SohbetId = sohbet.Id,
                Mesaj = MapMesaj(ustaMesaji),
                KalanGunlukHak = Math.Max(0, gunlukLimit - bugunkuSayi - 1),
                KalanSohbetMesaji = Math.Max(0, SohbetMesajLimiti - sohbetSayisi - 1)
            }, Messages.UstaYanitHazir);
        }

        public async Task<IDataResult<List<UstaSohbetDto>>> SohbetListesiAsync(int userId, int? vehicleId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<List<UstaSohbetDto>>(Messages.UserNotFound);

            if (vehicleId != null && await _vehicleAccess.GetAccessibleAsync(userId, vehicleId.Value) == null)
                return new ErrorDataResult<List<UstaSohbetDto>>(Messages.VehicleNotFound);

            var araclar = await _vehicleAccess.GetAccessibleListAsync(userId);
            var plakalar = araclar.ToDictionary(a => a.Id, a => a.Plate);

            var sohbetler = await _sohbetDal.GetListeAsync(vehicleId, user.Role == CompanyRole.Driver ? userId : null, QueryLimits.MaxListSize);
            var liste = sohbetler
                .Where(s => plakalar.ContainsKey(s.VehicleId))
                .Select(s => MapSohbet(s, plakalar[s.VehicleId], 0))
                .ToList();

            return new SuccessDataResult<List<UstaSohbetDto>>(liste);
        }

        public async Task<IDataResult<UstaSohbetDto>> SohbetAsync(int userId, int sohbetId)
        {
            var sohbet = await _sohbetDal.GetAsync(s => s.Id == sohbetId);
            if (sohbet == null)
                return new ErrorDataResult<UstaSohbetDto>(Messages.UstaSohbetBulunamadi);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, sohbet.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<UstaSohbetDto>(Messages.UstaSohbetBulunamadi);

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user != null && user.Role == CompanyRole.Driver && sohbet.UserId != userId)
                return new ErrorDataResult<UstaSohbetDto>(Messages.UstaSohbetBulunamadi);

            var mesajlar = await _mesajDal.GetSohbetMesajlariAsync(sohbetId);
            var dto = MapSohbet(sohbet, vehicle.Plate, mesajlar.Count(m => m.Rol == UstaRol.Kullanici));
            dto.Mesajlar = mesajlar.Select(MapMesaj).ToList();

            return new SuccessDataResult<UstaSohbetDto>(dto);
        }

        public async Task<IResult> SohbetSilAsync(int userId, int sohbetId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            var sohbet = await _sohbetDal.GetAsync(s => s.Id == sohbetId);
            if (sohbet == null)
                return new ErrorResult(Messages.UstaSohbetBulunamadi);

            if (await _vehicleAccess.GetAccessibleAsync(userId, sohbet.VehicleId) == null)
                return new ErrorResult(Messages.UstaSohbetBulunamadi);

            if (user.Role != CompanyRole.Owner && sohbet.UserId != userId)
                return new ErrorResult(Messages.AuthorizationDenied);

            await _mesajDal.DeleteBySohbetAsync(sohbetId);
            await _sohbetDal.DeleteAsync(sohbet);

            return new SuccessResult(Messages.UstaSohbetSilindi);
        }

        public async Task<IResult> GeriBildirimAsync(int userId, int mesajId, UstaGeriBildirimDto dto)
        {
            if (!Enum.IsDefined(dto.GeriBildirim))
                return new ErrorResult(Messages.InvalidValue);

            var mesaj = await _mesajDal.GetAsync(m => m.Id == mesajId);
            if (mesaj == null || mesaj.Rol != UstaRol.Usta)
                return new ErrorResult(Messages.UstaMesajBulunamadi);

            var sohbet = await _sohbetDal.GetAsync(s => s.Id == mesaj.SohbetId);
            if (sohbet == null)
                return new ErrorResult(Messages.UstaMesajBulunamadi);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, sohbet.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.UstaMesajBulunamadi);

            if (dto.CozumBakimId != null)
            {
                var bakim = await _maintenanceDal.GetAsync(b => b.Id == dto.CozumBakimId.Value);
                if (bakim == null || bakim.VehicleId != sohbet.VehicleId ||
                    bakim.Date.Date < DateTime.UtcNow.Date.AddDays(-CozumBakimGunu))
                {
                    return new ErrorResult(Messages.UstaCozumBakimiUygunDegil);
                }
            }

            mesaj.GeriBildirim = dto.GeriBildirim;
            mesaj.CozumBakimId = dto.GeriBildirim == UstaGeriBildirim.Olumlu ? dto.CozumBakimId : null;
            await _mesajDal.UpdateAsync(mesaj);

            return new SuccessResult(Messages.UstaGeriBildirimAlindi);
        }

        public async Task<IDataResult<List<UstaBakimSecenegiDto>>> CozumBakimSecenekleriAsync(int userId, int sohbetId)
        {
            var sohbet = await _sohbetDal.GetAsync(s => s.Id == sohbetId);
            if (sohbet == null)
                return new ErrorDataResult<List<UstaBakimSecenegiDto>>(Messages.UstaSohbetBulunamadi);

            if (await _vehicleAccess.GetAccessibleAsync(userId, sohbet.VehicleId) == null)
                return new ErrorDataResult<List<UstaBakimSecenegiDto>>(Messages.UstaSohbetBulunamadi);

            var sinir = DateTime.UtcNow.Date.AddDays(-CozumBakimGunu);
            var bakimlar = await _maintenanceDal.GetListAsync(b => b.VehicleId == sohbet.VehicleId && b.Date >= sinir);

            var liste = bakimlar
                .OrderByDescending(b => b.Date)
                .Select(b => new UstaBakimSecenegiDto
                {
                    Id = b.Id,
                    Tarih = b.Date,
                    Tur = b.Type.ToString(),
                    Servis = b.ServiceName,
                    Tutar = b.Cost
                })
                .ToList();

            return new SuccessDataResult<List<UstaBakimSecenegiDto>>(liste);
        }

        public const int GarajimVerisiEsigi = 30;

        private async Task<string> GarajimVerisiAsync(Vehicle vehicle)
        {
            if (!_configuration.GetValue("Usta:GarajimVerisi", false))
            {
                return null;
            }

            var satirlar = (await _cozumOzetiDal.GetTumuAsync())
                .Where(o => o.Sayi >= GarajimVerisiEsigi)
                .Where(o => string.Equals(o.Marka, vehicle.Brand, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(o.Model, vehicle.Model, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => o.Sayi)
                .Take(10)
                .ToList();

            if (satirlar.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine("GARAJIM VERISI");
            sb.AppendLine("Ayni marka/modelde kullanicilarin olumlu isaretledigi cozumler (anonim, kisi ve sirket bilgisi yok):");
            foreach (var satir in satirlar)
            {
                sb.AppendLine("- " + satir.BelirtiKategori + " -> " + satir.ParcaTuru + " (n=" + satir.Sayi + ")");
            }

            return sb.ToString();
        }

        public async Task<IDataResult<UstaStatsDto>> StatsAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<UstaStatsDto>(Messages.UserNotFound);

            if (user.Role != CompanyRole.Owner)
                return new ErrorDataResult<UstaStatsDto>(Messages.AuthorizationDenied);

            var ozet = await _mesajDal.IstatistikAsync();
            var stats = new UstaStatsDto { SoruSayisi = ozet.Toplam };

            if (ozet.Toplam == 0)
            {
                return new SuccessDataResult<UstaStatsDto>(stats);
            }

            stats.PuanlananOrani = Oran(ozet.Puanlanan, ozet.Toplam);
            stats.OlumluOrani = ozet.Puanlanan == 0 ? 0m : Oran(ozet.Olumlu, ozet.Puanlanan);
            stats.KirmiziCizgiOrani = Oran(ozet.KirmiziCizgi, ozet.Toplam);
            stats.CozumBagiOrani = Oran(ozet.CozumBagli, ozet.Toplam);
            stats.OrtTokenGiris = (int)Math.Round((double)ozet.TokenGiris / ozet.Toplam);
            stats.OrtTokenCikis = (int)Math.Round((double)ozet.TokenCikis / ozet.Toplam);
            stats.OrtSureMs = (int)Math.Round((double)ozet.SureMs / ozet.Toplam);

            var milyonFiyat = _configuration.GetValue("Usta:TokenFiyat", 0m);
            stats.TahminiMaliyetTl = Math.Round(milyonFiyat * (ozet.TokenGiris + ozet.TokenCikis) / 1000000m, 2);

            return new SuccessDataResult<UstaStatsDto>(stats);
        }

        private static decimal Oran(int pay, int payda)
        {
            return payda == 0 ? 0m : Math.Round((decimal)pay / payda * 100, 1);
        }

        private async Task<(Company Sirket, string Hata)> KapiAsync(int userId)

        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return (null, Messages.UserNotFound);

            var guncel = GuncelOnaySurumu();
            var onay = await _onayDal.GetAsync(o => o.UserId == userId && o.MetinSurumu == guncel);
            if (onay == null)
                return (null, Messages.UstaOnayGerekli);

            var sirket = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            if (sirket == null)
                return (null, Messages.UserNotFound);

            return (sirket, null);
        }

        private int GunlukLimit(PlanType plan)
        {
            var anahtar = plan == PlanType.Filo ? "Usta:GunlukLimitFilo" : "Usta:GunlukLimitBireysel";
            var varsayilan = plan == PlanType.Filo ? VarsayilanFiloLimit : VarsayilanBireyselLimit;

            return int.TryParse(_configuration[anahtar], out var limit) && limit > 0 ? limit : varsayilan;
        }

        private async Task<IReadOnlyList<(string Rol, string Metin)>> GecmisAsync(int sohbetId)
        {
            var mesajlar = await _mesajDal.GetSohbetMesajlariAsync(sohbetId);

            return mesajlar
                .Where(m => m.Rol == UstaRol.Kullanici || !string.IsNullOrWhiteSpace(m.Metin))
                .TakeLast(GecmisMesajSayisi)
                .Select(m => (m.Rol == UstaRol.Kullanici ? "Kullanici" : "Usta", m.Metin))
                .ToList();
        }

        private async Task<string> AracBaglamiAsync(int userId, Vehicle vehicle)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ARAC BILGISI");
            sb.AppendLine($"Marka/Model: {vehicle.Brand} {vehicle.Model}, Yıl: {vehicle.Year}");
            sb.AppendLine($"Motor: {Deger(vehicle.Motor)}, Yakıt: {vehicle.FuelType}, Vites: {Deger(vehicle.Vites)}");
            sb.AppendLine($"Güncel kilometre: {vehicle.CurrentKm}, Kullanım: {vehicle.KullanimTuru}");

            var bakimlar = (await _maintenanceDal.GetListAsync(b => b.VehicleId == vehicle.Id))
                .OrderByDescending(b => b.Date)
                .Take(5)
                .ToList();

            sb.AppendLine();
            sb.AppendLine("SON BAKIMLAR");
            if (bakimlar.Count == 0)
            {
                sb.AppendLine("Kayıt yok.");
            }
            else
            {
                var parcalar = await _partDal.GetByVehicleAsync(vehicle.Id);
                foreach (var bakim in bakimlar)
                {
                    var bakimParcalari = parcalar
                        .Where(p => p.MaintenanceRecordId == bakim.Id)
                        .Select(p => p.ParcaTuru.ToString())
                        .ToList();

                    sb.Append($"- {bakim.Date:dd.MM.yyyy} · {bakim.Km} km · {bakim.Type}");
                    if (bakimParcalari.Count > 0)
                    {
                        sb.Append(" · parçalar: " + string.Join(", ", bakimParcalari));
                    }
                    sb.AppendLine();
                }
            }

            var hafiza = await _partMemory.GetAsync(userId, vehicle.Id);
            sb.AppendLine();
            sb.AppendLine("PARCA HAFIZASI");
            if (hafiza.Success && hafiza.Data != null && hafiza.Data.Count > 0)
            {
                foreach (var satir in hafiza.Data.Where(h => h.Durum != "Iyi").Take(8))
                {
                    sb.AppendLine($"- {satir.ParcaAdi}: {satir.Durum}");
                }
            }
            else
            {
                sb.AppendLine("Kayıt yok.");
            }

            var bugun = DateTime.UtcNow.Date;
            var evraklar = await _evrakDal.GetListAsync(e => e.Aktif && e.VehicleId == vehicle.Id);
            sb.AppendLine();
            sb.AppendLine("AKTIF EVRAK");
            if (evraklar.Count == 0)
            {
                sb.AppendLine("Kayıt yok.");
            }
            else
            {
                foreach (var evrak in evraklar.OrderBy(e => e.BitisTarihi))
                {
                    sb.AppendLine($"- {evrak.EvrakTuru}: {evrak.BitisTarihi:dd.MM.yyyy} ({EvrakKurallari.Durum(evrak.BitisTarihi, bugun)})");
                }
            }

            sb.AppendLine();
            sb.AppendLine("YAKIT TUKETIMI");
            sb.AppendLine(await TuketimMetniAsync(vehicle.Id, bugun));

            var hatirlatmalar = await _reminderDal.GetListAsync(r => r.VehicleId == vehicle.Id && !r.IsCompleted);
            sb.AppendLine();
            sb.AppendLine("ACIK HATIRLATMALAR");
            if (hatirlatmalar.Count == 0)
            {
                sb.AppendLine("Kayıt yok.");
            }
            else
            {
                foreach (var hatirlatma in hatirlatmalar.OrderBy(r => r.DueDate ?? DateTime.MaxValue).Take(8))
                {
                    var vade = hatirlatma.DueDate != null ? hatirlatma.DueDate.Value.ToString("dd.MM.yyyy") : "-";
                    var km = hatirlatma.DueKm != null ? hatirlatma.DueKm.Value + " km" : "-";
                    sb.AppendLine($"- {hatirlatma.Type}: {vade} / {km}");
                }
            }

            return sb.ToString();
        }

        private async Task<string> TuketimMetniAsync(int vehicleId, DateTime bugun)
        {
            var sonUcAy = await _fuelDal.GetOlcumlerAsync(vehicleId, bugun.AddMonths(-3), bugun.AddDays(1).AddTicks(-1));
            var oncekiUcAy = await _fuelDal.GetOlcumlerAsync(vehicleId, bugun.AddMonths(-6), bugun.AddMonths(-3).AddTicks(-1));

            var son = Tuketim(sonUcAy);
            var onceki = Tuketim(oncekiUcAy);

            if (son == null)
            {
                return "Son 3 ayda tüketim hesaplayacak kadar yakıt kaydı yok.";
            }

            var metin = $"Son 3 ay ortalama: {son.Value.ToString("0.00", CultureInfo.InvariantCulture)} L/100km.";
            if (onceki != null)
            {
                var fark = son.Value - onceki.Value;
                var yon = fark > 0 ? "arttı" : "azaldı";
                metin += $" Önceki 3 aya göre {Math.Abs(fark).ToString("0.00", CultureInfo.InvariantCulture)} L/100km {yon}.";
            }

            return metin;
        }

        private static decimal? Tuketim(List<YakitOlcumDto> olcumler)
        {
            if (olcumler == null || olcumler.Count < 2)
            {
                return null;
            }

            var mesafe = olcumler[olcumler.Count - 1].Km - olcumler[0].Km;
            if (mesafe <= 0)
            {
                return null;
            }

            var litre = olcumler.Skip(1).Sum(o => o.Litre);
            return litre <= 0 ? null : Math.Round(litre / mesafe * 100, 2);
        }

        private static string Deger(string metin)
        {
            return string.IsNullOrWhiteSpace(metin) ? "belirtilmemiş" : metin;
        }

        private static string Kirp(string metin, int uzunluk)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return "-";
            }

            var kirpik = metin.Trim();
            return kirpik.Length > uzunluk ? kirpik.Substring(0, uzunluk) : kirpik;
        }

        private static UstaSohbetDto MapSohbet(UstaSohbet sohbet, string plaka, int mesajSayisi)
        {
            return new UstaSohbetDto
            {
                Id = sohbet.Id,
                VehicleId = sohbet.VehicleId,
                Plaka = plaka,
                Baslik = sohbet.Baslik,
                MesajSayisi = mesajSayisi,
                OlusturmaTarihi = sohbet.OlusturmaTarihi
            };
        }

        private static UstaMesajDto MapMesaj(UstaMesaj mesaj)
        {
            UstaYanitDto yanit = null;
            if (!string.IsNullOrWhiteSpace(mesaj.YapiliYanit))
            {
                try
                {
                    yanit = JsonSerializer.Deserialize<UstaYanitDto>(mesaj.YapiliYanit, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    yanit = null;
                }
            }

            return new UstaMesajDto
            {
                Id = mesaj.Id,
                Rol = mesaj.Rol.ToString(),
                Metin = mesaj.Metin,
                Yanit = yanit,
                KirmiziCizgi = mesaj.KirmiziCizgi,
                GeriBildirim = mesaj.GeriBildirim.ToString(),
                CozumBakimId = mesaj.CozumBakimId,
                OlusturmaTarihi = mesaj.OlusturmaTarihi
            };
        }
    }
}
