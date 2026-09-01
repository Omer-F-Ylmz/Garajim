using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete.Evraklar
{
    public static class EvrakAdlari
    {
        public static string Ad(EvrakTuru tur)
        {
            return tur switch
            {
                EvrakTuru.Muayene => "araç muayenesi",
                EvrakTuru.TrafikSigortasi => "zorunlu trafik sigortası",
                EvrakTuru.Kasko => "kasko",
                EvrakTuru.EgzozEmisyon => "egzoz emisyon ölçümü",
                EvrakTuru.KisLastigi => "kış lastiği zorunluluğu",
                EvrakTuru.Ehliyet => "ehliyet",
                EvrakTuru.SRC => "SRC belgesi",
                EvrakTuru.Psikoteknik => "psikoteknik belgesi",
                _ => tur.ToString()
            };
        }
    }
}
