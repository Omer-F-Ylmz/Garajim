using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Concrete.Planlar;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class ReportManager : IReportService
    {
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IFuelDal _fuelDal;
        private readonly IExpenseDal _expenseDal;
        private readonly IUserDal _userDal;
        private readonly ICompanyDal _companyDal;
        private readonly IEvrakDal _evrakDal;
        private readonly IReminderDal _reminderDal;
        private readonly IVehicleAssignmentDal _assignmentDal;
        private readonly IReceiptDraftDal _receiptDraftDal;
        private readonly PlanKurallari _planKurallari;

        public ReportManager(IVehicleAccessService vehicleAccess, IMaintenanceDal maintenanceDal, IFuelDal fuelDal, IExpenseDal expenseDal, IUserDal userDal,
            ICompanyDal companyDal, IEvrakDal evrakDal, IReminderDal reminderDal, IVehicleAssignmentDal assignmentDal, IReceiptDraftDal receiptDraftDal, PlanKurallari planKurallari)
        {
            _vehicleAccess = vehicleAccess;
            _maintenanceDal = maintenanceDal;
            _fuelDal = fuelDal;
            _expenseDal = expenseDal;
            _userDal = userDal;
            _companyDal = companyDal;
            _evrakDal = evrakDal;
            _reminderDal = reminderDal;
            _assignmentDal = assignmentDal;
            _receiptDraftDal = receiptDraftDal;
            _planKurallari = planKurallari;
        }

        public async Task<IDataResult<ExpenseSummaryDto>> GetSummaryAsync(int userId, int vehicleId, DateTime start, DateTime end)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<ExpenseSummaryDto>(Messages.VehicleNotFound);
            if (end < start)
                return new ErrorDataResult<ExpenseSummaryDto>(Messages.InvalidValue);
            var endInclusive = end.Date >= DateTime.MaxValue.Date
                ? DateTime.MaxValue
                : end.Date.AddDays(1).AddTicks(-1);
            var totalFuel = await _fuelDal.GetTotalCostAsync(vehicleId, start, endInclusive);
            var totalMaintenance = await _maintenanceDal.GetTotalCostAsync(vehicleId, start, endInclusive);
            var categories = await _expenseDal.GetCategoryTotalsAsync(vehicleId, start, endInclusive);
            var totalOther = categories.Sum(c => c.Total);
            var summary = new ExpenseSummaryDto
            {
                TotalFuel = totalFuel,
                TotalMaintenance = totalMaintenance,
                TotalOtherExpense = totalOther,
                GrandTotal = totalFuel + totalMaintenance + totalOther,
                Categories = categories.OrderByDescending(c => c.Total).ToList()
            };
            return new SuccessDataResult<ExpenseSummaryDto>(summary);
        }

        public async Task<IDataResult<List<MonthlyCostDto>>> GetMonthlyAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<MonthlyCostDto>>(Messages.VehicleNotFound);
            var merged = new Dictionary<(int Year, int Month), decimal>();
            foreach (var item in await _fuelDal.GetMonthlyTotalsAsync(vehicleId))
                Accumulate(merged, item);
            foreach (var item in await _maintenanceDal.GetMonthlyTotalsAsync(vehicleId))
                Accumulate(merged, item);
            foreach (var item in await _expenseDal.GetMonthlyTotalsAsync(vehicleId))
                Accumulate(merged, item);
            var list = merged
                .Select(kv => new MonthlyCostDto { Year = kv.Key.Year, Month = kv.Key.Month, Total = kv.Value })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();
            return new SuccessDataResult<List<MonthlyCostDto>>(list);
        }

        public async Task<IDataResult<FuelStatsDto>> GetFuelStatsAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<FuelStatsDto>(Messages.VehicleNotFound);
            var records = (await _fuelDal.GetListAsync(f => f.VehicleId == vehicleId && f.Km > 0))
                .OrderBy(f => f.Km)
                .ToList();
            if (records.Count < 2)
                return new ErrorDataResult<FuelStatsDto>(Messages.NotEnoughFuelData);
            var totalKm = records[records.Count - 1].Km - records[0].Km;
            if (totalKm <= 0)
                return new ErrorDataResult<FuelStatsDto>(Messages.NotEnoughFuelData);
            var consumedLiters = records.Skip(1).Sum(f => f.Liters);
            var consumedCost = records.Skip(1).Sum(f => f.TotalCost);
            var stats = new FuelStatsDto
            {
                TotalKm = totalKm,
                TotalLiters = records.Sum(f => f.Liters),
                TotalCost = records.Sum(f => f.TotalCost),
                AverageConsumptionPer100Km = Math.Round(consumedLiters / totalKm * 100, 2),
                CostPerKm = Math.Round(consumedCost / totalKm, 2)
            };
            return new SuccessDataResult<FuelStatsDto>(stats);
        }

        public async Task<IDataResult<AracMaliyetDto>> GetAracMaliyetAsync(int userId, int vehicleId, DateTime baslangic, DateTime bitis)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<AracMaliyetDto>(Messages.VehicleNotFound);

            if (bitis.Date < baslangic.Date)
                return new ErrorDataResult<AracMaliyetDto>(Messages.InvalidValue);

            var bas = baslangic.Date;
            var son = GunSonu(bitis);

            var yakitAylik = await _fuelDal.GetMonthlyTotalsAsync(vehicleId, bas, son);
            var bakimAylik = await _maintenanceDal.GetMonthlyTotalsAsync(vehicleId, bas, son);
            var masrafAylik = await _expenseDal.GetMonthlyTotalsAsync(vehicleId, bas, son);
            var olcumler = await _fuelDal.GetOlcumlerAsync(vehicleId, bas, son);

            var maliyet = new AracMaliyetDto
            {
                VehicleId = vehicle.Id,
                Plaka = vehicle.Plate,
                Baslangic = bas,
                Bitis = bitis.Date,
                ToplamYakit = yakitAylik.Sum(a => a.Total),
                ToplamBakim = bakimAylik.Sum(a => a.Total),
                ToplamMasraf = masrafAylik.Sum(a => a.Total),
                YakitKaydiSayisi = olcumler.Count,
                AylikSeri = AylikSeri(bitis.Date, yakitAylik, bakimAylik, masrafAylik)
            };

            maliyet.ToplamMaliyet = maliyet.ToplamYakit + maliyet.ToplamBakim + maliyet.ToplamMasraf;

            if (olcumler.Count >= 2)
            {
                maliyet.MesafeKm = olcumler[olcumler.Count - 1].Km - olcumler[0].Km;
            }

            if (maliyet.MesafeKm > 0)
            {
                maliyet.MaliyetKmBasi = Math.Round(maliyet.ToplamMaliyet / maliyet.MesafeKm, 2);

                var tuketilenLitre = olcumler.Skip(1).Sum(o => o.Litre);
                maliyet.Litre100Km = Math.Round(tuketilenLitre / maliyet.MesafeKm * 100, 2);
                maliyet.TuketimSeri = TuketimSeri(olcumler);
            }

            return new SuccessDataResult<AracMaliyetDto>(maliyet);
        }

        public async Task<IDataResult<FiloMaliyetDto>> GetFiloMaliyetAsync(int userId, DateTime baslangic, DateTime bitis)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<FiloMaliyetDto>(Messages.UserNotFound);
            if (user.Role == CompanyRole.Driver)
                return new ErrorDataResult<FiloMaliyetDto>(Messages.AuthorizationDenied);

            if (bitis.Date < baslangic.Date)
                return new ErrorDataResult<FiloMaliyetDto>(Messages.InvalidValue);

            var bas = baslangic.Date;
            var son = GunSonu(bitis);

            var rapor = new FiloMaliyetDto { Baslangic = bas, Bitis = bitis.Date };

            var araclar = await _vehicleAccess.GetAccessibleListAsync(userId);
            if (araclar.Count == 0)
                return new SuccessDataResult<FiloMaliyetDto>(rapor);

            var idler = araclar.Select(a => a.Id).ToList();

            var yakit = (await _fuelDal.GetTotalsByVehicleAsync(idler, bas, son)).ToDictionary(t => t.VehicleId, t => t.Toplam);
            var bakim = (await _maintenanceDal.GetTotalsByVehicleAsync(idler, bas, son)).ToDictionary(t => t.VehicleId, t => t.Toplam);
            var masraf = (await _expenseDal.GetTotalsByVehicleAsync(idler, bas, son)).ToDictionary(t => t.VehicleId, t => t.Toplam);
            var ozet = (await _fuelDal.GetYakitOzetiAsync(idler, bas, son)).ToDictionary(o => o.VehicleId);
            var ilkDolum = (await _fuelDal.GetIlkDolumLitreleriAsync(idler, bas, son)).ToDictionary(t => t.VehicleId, t => t.Toplam);

            foreach (var arac in araclar)
            {
                var satir = new FiloMaliyetSatiriDto
                {
                    VehicleId = arac.Id,
                    Plaka = arac.Plate,
                    Marka = arac.Brand,
                    Model = arac.Model,
                    ToplamYakit = Deger(yakit, arac.Id),
                    ToplamBakim = Deger(bakim, arac.Id),
                    ToplamMasraf = Deger(masraf, arac.Id)
                };

                satir.ToplamMaliyet = satir.ToplamYakit + satir.ToplamBakim + satir.ToplamMasraf;

                if (ozet.TryGetValue(arac.Id, out var yakitOzeti))
                {
                    satir.YakitKaydiSayisi = yakitOzeti.Adet;

                    if (yakitOzeti.Adet >= 2 && yakitOzeti.EnYuksekKm > yakitOzeti.EnDusukKm)
                    {
                        satir.MesafeKm = yakitOzeti.EnYuksekKm - yakitOzeti.EnDusukKm;
                        satir.MaliyetKmBasi = Math.Round(satir.ToplamMaliyet / satir.MesafeKm, 2);

                        var tuketilenLitre = yakitOzeti.Litre - Deger(ilkDolum, arac.Id);
                        if (tuketilenLitre > 0)
                        {
                            satir.Litre100Km = Math.Round(tuketilenLitre / satir.MesafeKm * 100, 2);
                        }
                    }
                }

                rapor.Araclar.Add(satir);
            }

            rapor.ToplamMaliyet = rapor.Araclar.Sum(a => a.ToplamMaliyet);
            rapor.ToplamMesafeKm = rapor.Araclar.Sum(a => a.MesafeKm);
            rapor.Araclar = rapor.Araclar
                .OrderByDescending(a => a.MaliyetKmBasi ?? -1)
                .ThenByDescending(a => a.ToplamMaliyet)
                .ThenBy(a => a.Plaka)
                .ToList();

            return new SuccessDataResult<FiloMaliyetDto>(rapor);
        }

        public async Task<IDataResult<DashboardDto>> GetDashboardAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<DashboardDto>(Messages.UserNotFound);

            var sirket = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            if (sirket == null)
                return new ErrorDataResult<DashboardDto>(Messages.UserNotFound);

            var araclar = await _vehicleAccess.GetAccessibleListAsync(userId);
            var idler = araclar.Select(a => a.Id).ToList();

            var bugun = DateTime.UtcNow.Date;
            var buAyBasi = new DateTime(bugun.Year, bugun.Month, 1);
            var gecenAyBasi = buAyBasi.AddMonths(-1);

            var panel = new DashboardDto
            {
                Plan = sirket.PlanType.ToString(),
                AracSayisi = araclar.Count,
                AracLimiti = _planKurallari.AracLimiti(sirket.PlanType, sirket.AracLimiti)
            };

            if (idler.Count == 0)
            {
                return new SuccessDataResult<DashboardDto>(panel);
            }

            panel.AktifZimmet = await _assignmentDal.AktifSayiAsync(idler);

            var evrak = await _evrakDal.DurumSayilariAsync(idler, userId, bugun, EvrakKurallari.YaklasiyorGun);
            panel.EvrakGecti = evrak.Gecti;
            panel.EvrakYaklasiyor = evrak.Yaklasiyor;

            panel.HatirlatmaYaklasiyor = await _reminderDal.YaklasanSayisiAsync(idler, bugun.AddDays(EvrakKurallari.YaklasiyorGun));

            if (user.Role != CompanyRole.Driver)
            {
                panel.BekleyenFis = await _receiptDraftDal.BekleyenSayisiAsync();
            }

            panel.BuAyMaliyet = await AyMaliyetiAsync(idler, buAyBasi, buAyBasi.AddMonths(1).AddTicks(-1));
            panel.GecenAyMaliyet = await AyMaliyetiAsync(idler, gecenAyBasi, buAyBasi.AddTicks(-1));

            if (panel.GecenAyMaliyet > 0)
            {
                panel.DegisimYuzde = Math.Round((panel.BuAyMaliyet - panel.GecenAyMaliyet) / panel.GecenAyMaliyet * 100, 1);
            }

            return new SuccessDataResult<DashboardDto>(panel);
        }

        private async Task<decimal> AyMaliyetiAsync(List<int> vehicleIds, DateTime bas, DateTime son)
        {
            var yakit = await _fuelDal.GetTotalsByVehicleAsync(vehicleIds, bas, son);
            var bakim = await _maintenanceDal.GetTotalsByVehicleAsync(vehicleIds, bas, son);
            var masraf = await _expenseDal.GetTotalsByVehicleAsync(vehicleIds, bas, son);

            return yakit.Sum(t => t.Toplam) + bakim.Sum(t => t.Toplam) + masraf.Sum(t => t.Toplam);
        }

        private static decimal Deger(Dictionary<int, decimal> kaynak, int vehicleId)
        {
            return kaynak.TryGetValue(vehicleId, out var deger) ? deger : 0m;
        }

        private static DateTime GunSonu(DateTime bitis)
        {
            return bitis.Date >= DateTime.MaxValue.Date ? DateTime.MaxValue : bitis.Date.AddDays(1).AddTicks(-1);
        }

        private static List<MaliyetAyDto> AylikSeri(DateTime bitis, List<MonthlyCostDto> yakit, List<MonthlyCostDto> bakim, List<MonthlyCostDto> masraf)
        {
            var seri = new List<MaliyetAyDto>();
            var sonAy = new DateTime(bitis.Year, bitis.Month, 1);

            for (var i = 11; i >= 0; i--)
            {
                var ay = sonAy.AddMonths(-i);
                var kalem = new MaliyetAyDto
                {
                    Yil = ay.Year,
                    Ay = ay.Month,
                    Yakit = AyToplami(yakit, ay),
                    Bakim = AyToplami(bakim, ay),
                    Masraf = AyToplami(masraf, ay)
                };
                kalem.Toplam = kalem.Yakit + kalem.Bakim + kalem.Masraf;
                seri.Add(kalem);
            }

            return seri;
        }

        private static decimal AyToplami(List<MonthlyCostDto> kaynak, DateTime ay)
        {
            return kaynak.Where(k => k.Year == ay.Year && k.Month == ay.Month).Sum(k => k.Total);
        }

        private static List<TuketimAyDto> TuketimSeri(List<YakitOlcumDto> olcumler)
        {
            var mesafeler = new Dictionary<(int Yil, int Ay), int>();
            var litreler = new Dictionary<(int Yil, int Ay), decimal>();

            for (var i = 1; i < olcumler.Count; i++)
            {
                var fark = olcumler[i].Km - olcumler[i - 1].Km;
                if (fark <= 0)
                {
                    continue;
                }

                var anahtar = (olcumler[i].Tarih.Year, olcumler[i].Tarih.Month);
                mesafeler.TryGetValue(anahtar, out var mesafe);
                mesafeler[anahtar] = mesafe + fark;
                litreler.TryGetValue(anahtar, out var litre);
                litreler[anahtar] = litre + olcumler[i].Litre;
            }

            return mesafeler
                .Where(m => m.Value > 0)
                .Select(m => new TuketimAyDto
                {
                    Yil = m.Key.Yil,
                    Ay = m.Key.Ay,
                    Litre100Km = Math.Round(litreler[m.Key] / m.Value * 100, 2)
                })
                .OrderBy(t => t.Yil).ThenBy(t => t.Ay)
                .ToList();
        }

        private static void Accumulate(Dictionary<(int Year, int Month), decimal> merged, MonthlyCostDto item)
        {
            var key = (item.Year, item.Month);
            merged.TryGetValue(key, out var current);
            merged[key] = current + item.Total;
        }
    }
}
