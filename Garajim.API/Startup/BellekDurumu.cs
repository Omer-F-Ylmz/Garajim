using System.Diagnostics;

namespace Garajim.API.Startup
{
    public static class BellekDurumu
    {
        public static object Oku()
        {
            using var surec = Process.GetCurrentProcess();

            return new
            {
                Surum = SurumBilgisi.Surum,
                YonetilenBellekMb = Mb(GC.GetTotalMemory(false)),
                CalismaKumesiMb = Mb(surec.WorkingSet64),
                EnYuksekCalismaKumesiMb = Mb(surec.PeakWorkingSet64),
                OzelBaytMb = Mb(surec.PrivateMemorySize64),
                GcSayisi = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
                SunucuGc = System.Runtime.GCSettings.IsServerGC,
                CalismaSuresiDk = Math.Round((DateTime.UtcNow - surec.StartTime.ToUniversalTime()).TotalMinutes, 1)
            };
        }

        public static string TekSatir()
        {
            using var surec = Process.GetCurrentProcess();

            return $"Bellek: yönetilen {Mb(GC.GetTotalMemory(false))} MB, çalışma kümesi {Mb(surec.WorkingSet64)} MB, " +
                   $"özel {Mb(surec.PrivateMemorySize64)} MB, sunucu GC {System.Runtime.GCSettings.IsServerGC}";
        }

        private static double Mb(long bayt)
        {
            return Math.Round(bayt / 1024d / 1024d, 1);
        }
    }
}
