using System.Text;
using Garajim.RehberUretici;

Console.OutputEncoding = Encoding.UTF8;

var bilgi = Arguman(args, "--bilgi");
var cikti = Arguman(args, "--cikti");
var taban = Arguman(args, "--taban") ?? Sabitler.TabanAdres;
var sitemap = Arguman(args, "--sitemap");

if (string.IsNullOrWhiteSpace(bilgi) || string.IsNullOrWhiteSpace(cikti))
{
    Console.WriteLine("Kullanım: Garajim.RehberUretici --bilgi <bilgi klasörü> --cikti <rehber klasörü> [--taban <adres>] [--sitemap <dosya>]");
    return 1;
}

if (!Directory.Exists(bilgi))
{
    Console.WriteLine("Bilgi klasörü bulunamadı: " + bilgi);
    return 1;
}

var sonuc = Uretici.Uret(bilgi, cikti, taban.TrimEnd('/'), sitemap);

foreach (var uyari in sonuc.Uyarilar)
{
    Console.WriteLine("UYARI: " + uyari);
}

Console.WriteLine($"Rehber üretildi: {sonuc.Kayitlar.Count} kayıt sayfası, {sonuc.Uyarilar.Count} uyarı -> {cikti}");

return 0;

static string Arguman(string[] args, string ad)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == ad)
        {
            return args[i + 1];
        }
    }

    return null;
}
