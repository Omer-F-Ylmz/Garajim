using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete.Parts
{
    public class ParcaAraligi
    {
        public int? Km { get; set; }
        public int? Ay { get; set; }
        public string Ad { get; set; }
    }

    public static class ParcaAraliklari
    {
        private static readonly Dictionary<ParcaTuru, ParcaAraligi> Katalog = new Dictionary<ParcaTuru, ParcaAraligi>
        {
            [ParcaTuru.MotorYagi] = new ParcaAraligi { Km = 10000, Ay = 12, Ad = "Motor yağı" },
            [ParcaTuru.YagFiltresi] = new ParcaAraligi { Km = 15000, Ay = 12, Ad = "Yağ filtresi" },
            [ParcaTuru.HavaFiltresi] = new ParcaAraligi { Km = 20000, Ay = 12, Ad = "Hava filtresi" },
            [ParcaTuru.PolenFiltresi] = new ParcaAraligi { Km = 15000, Ay = 12, Ad = "Polen filtresi" },
            [ParcaTuru.YakitFiltresi] = new ParcaAraligi { Km = 20000, Ay = 12, Ad = "Yakıt filtresi" },
            [ParcaTuru.FrenBalatasiOn] = new ParcaAraligi { Km = 40000, Ad = "Ön fren balatası" },
            [ParcaTuru.FrenBalatasiArka] = new ParcaAraligi { Km = 40000, Ad = "Arka fren balatası" },
            [ParcaTuru.FrenDiskiOn] = new ParcaAraligi { Km = 80000, Ad = "Ön fren diski" },
            [ParcaTuru.FrenDiskiArka] = new ParcaAraligi { Km = 80000, Ad = "Arka fren diski" },
            [ParcaTuru.Buji] = new ParcaAraligi { Km = 40000, Ad = "Buji" },
            [ParcaTuru.TrigerSeti] = new ParcaAraligi { Km = 90000, Ay = 60, Ad = "Triger seti" },
            [ParcaTuru.VKayisi] = new ParcaAraligi { Km = 60000, Ay = 48, Ad = "V kayışı" },
            [ParcaTuru.Aku] = new ParcaAraligi { Ay = 48, Ad = "Akü" },
            [ParcaTuru.Lastik] = new ParcaAraligi { Km = 50000, Ay = 72, Ad = "Lastik" },
            [ParcaTuru.Amortisor] = new ParcaAraligi { Km = 80000, Ad = "Amortisör" },
            [ParcaTuru.Silecek] = new ParcaAraligi { Ay = 12, Ad = "Silecek" },
            [ParcaTuru.Antifriz] = new ParcaAraligi { Ay = 24, Ad = "Antifriz" },
            [ParcaTuru.FrenHidroligi] = new ParcaAraligi { Ay = 24, Ad = "Fren hidroliği" },
            [ParcaTuru.SanzimanYagi] = new ParcaAraligi { Km = 60000, Ay = 48, Ad = "Şanzıman yağı" },
            [ParcaTuru.Devirdaim] = new ParcaAraligi { Km = 90000, Ad = "Devirdaim" },
            [ParcaTuru.RotBasi] = new ParcaAraligi { Km = 60000, Ad = "Rot başı" },
            [ParcaTuru.Salincak] = new ParcaAraligi { Km = 80000, Ad = "Salıncak" },
            [ParcaTuru.Debriyaj] = new ParcaAraligi { Km = 120000, Ad = "Debriyaj" },
            [ParcaTuru.Diger] = new ParcaAraligi { Ad = "Diğer" }
        };

        public static ParcaAraligi Al(ParcaTuru tur)
        {
            return Katalog.TryGetValue(tur, out var aralik) ? aralik : new ParcaAraligi { Ad = tur.ToString() };
        }

        public static string Ad(ParcaTuru tur)
        {
            return Al(tur).Ad;
        }
    }
}
