namespace Garajim.Business.Concrete
{
    public static class HesapSilme
    {
        public const int BeklemeGunu = 7;

        public const string EpostaKonusu = "Garajım hesap silme kodunuz";

        public static string SayacAnahtari(string email)
        {
            return "hesapsilme:" + (email ?? string.Empty);
        }

        public static string EpostaGovdesi(string kod)
        {
            return
                "Garajım hesap silme kodunuz: " + kod + Environment.NewLine + Environment.NewLine +
                "Kod " + DogrulamaKodu.GecerlilikDakika + " dakika geçerlidir." + Environment.NewLine +
                "Kodu girdiğinizde şirketiniz ve tüm verileriniz " + BeklemeGunu +
                " gün sonra kalıcı olarak silinecek." + Environment.NewLine +
                "Bu süre içinde giriş yapıp silmeyi iptal edebilirsiniz." + Environment.NewLine +
                Environment.NewLine +
                "Bu isteği siz yapmadıysanız bu e-postayı yok sayın; hiçbir şey silinmez.";
        }
    }
}
