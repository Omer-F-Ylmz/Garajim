using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public static class DegerAdlari
    {
        public static string Kaynak(DegerKaynagi kaynak)
        {
            return kaynak switch
            {
                DegerKaynagi.Beyan => "Beyan",
                DegerKaynagi.Tahmin => "Tahmin",
                DegerKaynagi.Ekspertiz => "Ekspertiz",
                _ => "İlan"
            };
        }
    }
}
