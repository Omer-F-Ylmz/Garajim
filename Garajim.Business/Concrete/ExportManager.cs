using System.Globalization;
using System.Text;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class ExportManager : IExportService
    {
        private static readonly string[] Turler = { "yakit", "bakim", "masraf", "evrak" };
        private static readonly CultureInfo Kultur = new CultureInfo("tr-TR");

        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IFuelDal _fuelDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IExpenseDal _expenseDal;
        private readonly IEvrakDal _evrakDal;

        public ExportManager(
            IVehicleAccessService vehicleAccess,
            IFuelDal fuelDal,
            IMaintenanceDal maintenanceDal,
            IExpenseDal expenseDal,
            IEvrakDal evrakDal)
        {
            _vehicleAccess = vehicleAccess;
            _fuelDal = fuelDal;
            _maintenanceDal = maintenanceDal;
            _expenseDal = expenseDal;
            _evrakDal = evrakDal;
        }

        public async Task<IDataResult<ExportSonucDto>> CsvAsync(int userId, string tur, int? vehicleId, DateTime? baslangic, DateTime? bitis)
        {
            var secilen = Turler.FirstOrDefault(t => string.Equals(t, tur, StringComparison.OrdinalIgnoreCase));
            if (secilen == null)
                return new ErrorDataResult<ExportSonucDto>(Messages.ExportTuruBulunamadi);

            if (baslangic != null && bitis != null && bitis.Value.Date < baslangic.Value.Date)
                return new ErrorDataResult<ExportSonucDto>(Messages.InvalidValue);

            List<Vehicle> araclar;
            if (vehicleId != null)
            {
                var arac = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId.Value);
                if (arac == null)
                    return new ErrorDataResult<ExportSonucDto>(Messages.VehicleNotFound);
                araclar = new List<Vehicle> { arac };
            }
            else
            {
                araclar = await _vehicleAccess.GetAccessibleListAsync(userId);
            }

            var plakalar = araclar.ToDictionary(a => a.Id, a => a.Plate);
            var idler = araclar.Select(a => a.Id).ToList();

            var bas = baslangic?.Date ?? DateTime.MinValue;
            var son = bitis == null
                ? DateTime.MaxValue
                : (bitis.Value.Date >= DateTime.MaxValue.Date ? DateTime.MaxValue : bitis.Value.Date.AddDays(1).AddTicks(-1));

            var sb = new StringBuilder();
            var satirSayisi = 0;

            if (secilen == "yakit")
            {
                sb.AppendLine("Plaka;Tarih;Kilometre;Litre;BirimFiyat;Tutar;Kwh;SarjTuru");
                var kayitlar = idler.Count == 0
                    ? new List<FuelRecord>()
                    : await _fuelDal.GetListAsync(f => idler.Contains(f.VehicleId) && f.Date >= bas && f.Date <= son);

                foreach (var kayit in kayitlar.OrderBy(k => k.Date).ThenBy(k => k.Id))
                {
                    var birim = kayit.Liters > 0 ? Math.Round(kayit.TotalCost / kayit.Liters, 2) : 0m;
                    sb.AppendLine(Satir(Plaka(plakalar, kayit.VehicleId), Tarih(kayit.Date), kayit.Km.ToString(CultureInfo.InvariantCulture),
                        Sayi(kayit.Liters), Sayi(birim), Sayi(kayit.TotalCost),
                        kayit.Kwh == null ? string.Empty : Sayi(kayit.Kwh.Value),
                        kayit.SarjTuru?.ToString()));
                    satirSayisi++;
                }
            }
            else if (secilen == "bakim")
            {
                sb.AppendLine("Plaka;Tarih;Kilometre;Tur;Servis;Tutar;Not");
                var kayitlar = idler.Count == 0
                    ? new List<MaintenanceRecord>()
                    : await _maintenanceDal.GetListAsync(m => idler.Contains(m.VehicleId) && m.Date >= bas && m.Date <= son);

                foreach (var kayit in kayitlar.OrderBy(k => k.Date).ThenBy(k => k.Id))
                {
                    sb.AppendLine(Satir(Plaka(plakalar, kayit.VehicleId), Tarih(kayit.Date), kayit.Km.ToString(CultureInfo.InvariantCulture),
                        kayit.Type.ToString(), kayit.ServiceName, Sayi(kayit.Cost), kayit.Note));
                    satirSayisi++;
                }
            }
            else if (secilen == "masraf")
            {
                sb.AppendLine("Plaka;Tarih;Kategori;Tutar;Not");
                var kayitlar = idler.Count == 0
                    ? new List<ExpenseRecord>()
                    : await _expenseDal.GetListAsync(e => idler.Contains(e.VehicleId) && e.Date >= bas && e.Date <= son);

                foreach (var kayit in kayitlar.OrderBy(k => k.Date).ThenBy(k => k.Id))
                {
                    sb.AppendLine(Satir(Plaka(plakalar, kayit.VehicleId), Tarih(kayit.Date), kayit.Category.ToString(),
                        Sayi(kayit.Amount), kayit.Note));
                    satirSayisi++;
                }
            }
            else
            {
                sb.AppendLine("Plaka;Tur;Baslangic;Bitis;Saglayici;PoliceNo;Durum");
                var bugun = DateTime.UtcNow.Date;
                var kayitlar = await _evrakDal.GetListAsync(e => e.Aktif &&
                    ((e.VehicleId != null && idler.Contains(e.VehicleId.Value)) || e.UserId == userId));

                foreach (var kayit in kayitlar.Where(k => k.BitisTarihi >= bas && k.BitisTarihi <= son).OrderBy(k => k.BitisTarihi).ThenBy(k => k.Id))
                {
                    var plaka = kayit.VehicleId != null ? Plaka(plakalar, kayit.VehicleId.Value) : string.Empty;
                    sb.AppendLine(Satir(plaka, kayit.EvrakTuru.ToString(),
                        kayit.BaslangicTarihi == null ? string.Empty : Tarih(kayit.BaslangicTarihi.Value),
                        Tarih(kayit.BitisTarihi), kayit.Saglayici, kayit.PoliceNo,
                        EvrakKurallari.Durum(kayit.BitisTarihi, bugun)));
                    satirSayisi++;
                }
            }

            var icerik = new List<byte> { 0xEF, 0xBB, 0xBF };
            icerik.AddRange(Encoding.UTF8.GetBytes(sb.ToString()));

            return new SuccessDataResult<ExportSonucDto>(new ExportSonucDto
            {
                DosyaAdi = $"garajim-{secilen}-{DateTime.UtcNow:yyyyMMdd}.csv",
                Icerik = icerik.ToArray(),
                SatirSayisi = satirSayisi
            }, Messages.ExportHazir);
        }

        private static string Plaka(Dictionary<int, string> plakalar, int vehicleId)
        {
            return plakalar.TryGetValue(vehicleId, out var plaka) ? plaka : string.Empty;
        }

        private static string Tarih(DateTime deger)
        {
            return deger.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        private static string Sayi(decimal deger)
        {
            return deger.ToString("0.00", Kultur);
        }

        private static string Satir(params string[] alanlar)
        {
            return string.Join(";", alanlar.Select(Kacir));
        }

        private static string Kacir(string deger)
        {
            if (string.IsNullOrEmpty(deger))
            {
                return string.Empty;
            }

            if (deger.IndexOf(';') >= 0 || deger.IndexOf('"') >= 0 || deger.IndexOf('\n') >= 0 || deger.IndexOf('\r') >= 0)
            {
                return "\"" + deger.Replace("\"", "\"\"") + "\"";
            }

            return deger;
        }
    }
}
