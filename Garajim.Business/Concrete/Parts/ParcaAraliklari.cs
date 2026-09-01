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
            [ParcaTuru.MotorYagi] = new ParcaAraligi { Km = 10000, Ay = 12, Ad = "motor yağı" },
            [ParcaTuru.YagFiltresi] = new ParcaAraligi { Km = 15000, Ay = 12, Ad = "yağ filtresi" },
            [ParcaTuru.HavaFiltresi] = new ParcaAraligi { Km = 20000, Ay = 12, Ad = "hava filtresi" },
            [ParcaTuru.PolenFiltresi] = new ParcaAraligi { Km = 15000, Ay = 12, Ad = "polen filtresi" },
            [ParcaTuru.YakitFiltresi] = new ParcaAraligi { Km = 20000, Ay = 12, Ad = "yakıt filtresi" },
            [ParcaTuru.FrenBalatasiOn] = new ParcaAraligi { Km = 40000, Ad = "ön fren balatası" },
            [ParcaTuru.FrenBalatasiArka] = new ParcaAraligi { Km = 40000, Ad = "arka fren balatası" },
            [ParcaTuru.FrenDiskiOn] = new ParcaAraligi { Km = 80000, Ad = "ön fren diski" },
            [ParcaTuru.FrenDiskiArka] = new ParcaAraligi { Km = 80000, Ad = "arka fren diski" },
            [ParcaTuru.Buji] = new ParcaAraligi { Km = 40000, Ad = "buji" },
            [ParcaTuru.TrigerSeti] = new ParcaAraligi { Km = 90000, Ay = 60, Ad = "triger seti" },
            [ParcaTuru.VKayisi] = new ParcaAraligi { Km = 60000, Ay = 48, Ad = "V kayışı" },
            [ParcaTuru.Aku] = new ParcaAraligi { Ay = 48, Ad = "akü" },
            [ParcaTuru.Lastik] = new ParcaAraligi { Km = 50000, Ay = 72, Ad = "lastik" },
            [ParcaTuru.Amortisor] = new ParcaAraligi { Km = 80000, Ad = "amortisör" },
            [ParcaTuru.Silecek] = new ParcaAraligi { Ay = 12, Ad = "silecek" },
            [ParcaTuru.Antifriz] = new ParcaAraligi { Ay = 24, Ad = "antifriz" },
            [ParcaTuru.FrenHidroligi] = new ParcaAraligi { Ay = 24, Ad = "fren hidroliği" },
            [ParcaTuru.SanzimanYagi] = new ParcaAraligi { Km = 60000, Ay = 48, Ad = "şanzıman yağı" },
            [ParcaTuru.Devirdaim] = new ParcaAraligi { Km = 90000, Ad = "devirdaim" },
            [ParcaTuru.RotBasi] = new ParcaAraligi { Km = 60000, Ad = "rot başı" },
            [ParcaTuru.Salincak] = new ParcaAraligi { Km = 80000, Ad = "salıncak" },
            [ParcaTuru.Debriyaj] = new ParcaAraligi { Km = 120000, Ad = "debriyaj" },
            [ParcaTuru.Diger] = new ParcaAraligi { Ad = "diğer" }
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
