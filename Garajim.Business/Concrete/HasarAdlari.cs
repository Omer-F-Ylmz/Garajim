using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public static class HasarAdlari
    {
        public static string Tur(HasarTuru tur)
        {
            return tur switch
            {
                HasarTuru.Kaza => "Kaza",
                HasarTuru.Hasar => "Hasar",
                HasarTuru.Cam => "Cam",
                HasarTuru.Dolu => "Dolu",
                HasarTuru.Hirsizlik => "Hırsızlık",
                _ => "Diğer"
            };
        }

        public static string Durum(HasarDurumu durum)
        {
            return durum switch
            {
                HasarDurumu.Acik => "Açık",
                HasarDurumu.SigortaIslemde => "Sigorta işlemde",
                _ => "Kapandı"
            };
        }

        public static string Tutanak(TutanakTuru tutanak)
        {
            return tutanak switch
            {
                TutanakTuru.Anlasmali => "Anlaşmalı tutanak",
                TutanakTuru.Polis => "Polis/jandarma tutanağı",
                _ => "Tutanak yok"
            };
        }

        public static string Etiket(HasarFotoEtiketi etiket)
        {
            return etiket switch
            {
                HasarFotoEtiketi.Genel => "Genel görünüm",
                HasarFotoEtiketi.HasarYakin => "Hasar yakın çekim",
                HasarFotoEtiketi.KarsiArac => "Karşı araç",
                HasarFotoEtiketi.Plakalar => "Plakalar",
                HasarFotoEtiketi.Yol => "Yol ve işaretler",
                HasarFotoEtiketi.Belge => "Belge",
                _ => "Tutanak"
            };
        }
    }
}
