namespace Garajim.Business.Concrete
{
    public static class SifirlamaKodu
    {
        public const string EpostaKonusu = "Garajım şifre sıfırlama kodunuz";

        public static string SayacAnahtari(string email)
        {
            return "sifirlama:" + (email ?? string.Empty);
        }

        public static string EpostaGovdesi(string kod)
        {
            return
                "Garajım şifre sıfırlama kodunuz: " + kod + Environment.NewLine + Environment.NewLine +
                "Kod " + DogrulamaKodu.GecerlilikDakika + " dakika geçerlidir ve yalnız bir kez kullanılır." + Environment.NewLine +
                "Bu kodu kimseyle paylaşmayın; Garajım sizden kodu asla telefonla ya da e-postayla istemez." + Environment.NewLine +
                Environment.NewLine +
                "Şifre sıfırlamayı siz istemediyseniz bu e-postayı yok sayın; şifreniz değişmez.";
        }
    }
}
