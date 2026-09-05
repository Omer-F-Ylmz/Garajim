using System.Text;
using Garajim.Calibration;

Console.OutputEncoding = Encoding.UTF8;

var klasor = Ayarlar.ArgumanOku(args, "--dir");
var bekleme = Ayarlar.BeklemeOku(args);
if (string.IsNullOrWhiteSpace(klasor) || !Directory.Exists(klasor))
{
    Console.WriteLine("Kullanım: Garajim.Calibration --dir <klasör> [--bekle <ms>]");
    Console.WriteLine("Klasörde *.jpg dosyaları ve cevap-anahtari.csv bulunmalıdır.");
    Console.WriteLine($"--bekle fişler arasında beklenecek süredir; varsayılan {Ayarlar.VarsayilanBeklemeMs} ms.");
    return 1;
}

var adres = Environment.GetEnvironmentVariable("GARAJIM_URL");
var eposta = Environment.GetEnvironmentVariable("GARAJIM_EMAIL");
var sifre = Environment.GetEnvironmentVariable("GARAJIM_PASS");

if (string.IsNullOrWhiteSpace(adres) || string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
{
    Console.WriteLine("GARAJIM_URL, GARAJIM_EMAIL ve GARAJIM_PASS ortam değişkenleri gerekli.");
    return 1;
}

var anahtarYolu = Path.Combine(klasor, "cevap-anahtari.csv");
if (!File.Exists(anahtarYolu))
{
    Console.WriteLine($"Cevap anahtarı bulunamadı: {anahtarYolu}");
    return 1;
}

var beklenenler = CevapAnahtari.Oku(anahtarYolu);
Console.WriteLine($"{beklenenler.Count} satırlık cevap anahtarı okundu.");
Console.WriteLine($"Fişler arasında {bekleme} ms beklenecek (hız sınırı: dakikada 20 istek, fiş başına 2 istek).");

using var http = new HttpClient { BaseAddress = new Uri(adres), Timeout = TimeSpan.FromSeconds(120) };
var istemci = new GarajimIstemci(http);

try
{
    await istemci.GirisYapAsync(eposta, sifre);
    Console.WriteLine("Giriş yapıldı.");
}
catch (Exception ex)
{
    Console.WriteLine("Giriş başarısız: " + ex.Message);
    return 1;
}

var sonuclar = new List<DosyaSonucu>();
var sira = 0;

foreach (var beklenen in beklenenler)
{
    if (sira > 0 && bekleme > 0)
    {
        await Task.Delay(bekleme);
    }

    sira++;
    var dosyaYolu = Path.Combine(klasor, beklenen.Dosya);
    var sonuc = new DosyaSonucu { Dosya = beklenen.Dosya, Zorluk = beklenen.Zorluk, Tur = beklenen.Tur };

    if (!File.Exists(dosyaYolu))
    {
        sonuc.Hata = "Dosya bulunamadı.";
        sonuclar.Add(sonuc);
        Console.WriteLine($"[{sira}/{beklenenler.Count}] {beklenen.Dosya}: dosya yok, atlandı.");
        continue;
    }

    try
    {
        var yukleme = await istemci.FisYukleAsync(await File.ReadAllBytesAsync(dosyaYolu), beklenen.Dosya);

        if (yukleme.HizmetDolu)
        {
            Console.WriteLine($"[{sira}/{beklenenler.Count}] {beklenen.Dosya}: AI hizmeti dolu, atlandı ({yukleme.IstemciSureMs} ms).");
            continue;
        }

        var gelen = yukleme.Taslak;

        sonuc.GuvenSkoru = gelen.GuvenSkoru;
        sonuc.SureMs = yukleme.IstemciSureMs;

        Karsilastir(sonuc, "tarih", Karsilastirici.TarihEsit(beklenen.Tarih, gelen.Tarih),
            beklenen.Tarih?.ToString("dd.MM.yyyy"), gelen.Tarih?.ToString("dd.MM.yyyy"));
        Karsilastir(sonuc, "tutar", Karsilastirici.OndalikEsit(beklenen.Tutar, gelen.ToplamTutar),
            beklenen.Tutar?.ToString(), gelen.ToplamTutar?.ToString());
        Karsilastir(sonuc, "km", Karsilastirici.TamsayiEsit(beklenen.Km, gelen.Km),
            beklenen.Km?.ToString(), gelen.Km?.ToString());
        Karsilastir(sonuc, "plaka", Karsilastirici.PlakaEsit(beklenen.Plaka, gelen.Plaka),
            beklenen.Plaka, gelen.Plaka);
        Karsilastir(sonuc, "litre", Karsilastirici.OndalikEsit(beklenen.Litre, gelen.Litre),
            beklenen.Litre?.ToString(), gelen.Litre?.ToString());
        Karsilastir(sonuc, "tur", Karsilastirici.MetinEsit(beklenen.Tur, gelen.TahminiTur),
            beklenen.Tur, gelen.TahminiTur);

        var aracId = beklenen.Plaka != null ? await istemci.AracIdBulAsync(beklenen.Plaka) : 0;
        if (aracId == 0)
        {
            aracId = await istemci.IlkAracIdAsync();
        }

        if (aracId > 0 && beklenen.Tur != null)
        {
            await istemci.OnaylaAsync(yukleme.TaslakId, aracId, beklenen.Tur, beklenen.Tarih, beklenen.Tutar, beklenen.Km, beklenen.Litre);
        }

        var dogru = sonuc.AlanDogru.Values.Count(v => v);
        Console.WriteLine($"[{sira}/{beklenenler.Count}] {beklenen.Dosya}: {dogru}/{sonuc.AlanDogru.Count} alan doğru, güven {gelen.GuvenSkoru:0.00}, {yukleme.IstemciSureMs} ms");
    }
    catch (LimitAsildiException ex)
    {
        Console.WriteLine();
        Console.WriteLine((ex.AylikLimit ? "DURDURULDU (aylık limit): " : "DURDURULDU (hız sınırı): ") + ex.Message);
        Console.WriteLine($"{sira - 1} dosya işlendi, rapor bu kadarıyla üretiliyor.");
        break;
    }
    catch (Exception ex)
    {
        sonuc.Hata = ex.Message;
        Console.WriteLine($"[{sira}/{beklenenler.Count}] {beklenen.Dosya}: HATA {ex.Message}");
    }

    sonuclar.Add(sonuc);
}

var rapor = Rapor.Olustur(sonuclar);

Console.WriteLine();
Console.WriteLine("=== ALAN BAZINDA DOĞRULUK ===");
foreach (var alan in rapor.AlanDogruluk.OrderBy(a => a.Key))
{
    Console.WriteLine($"  {alan.Key,-8} %{alan.Value}");
}

Console.WriteLine();
Console.WriteLine("=== ZORLUĞA GÖRE ===");
foreach (var zorluk in rapor.ZorlukDogruluk.OrderBy(z => z.Key))
{
    Console.WriteLine($"  {zorluk.Key,-8} %{zorluk.Value}");
}

Console.WriteLine();
Console.WriteLine("=== TÜRE GÖRE ===");
foreach (var tur in rapor.TurDogruluk.OrderBy(t => t.Key))
{
    Console.WriteLine($"  {tur.Key,-8} %{tur.Value}");
}

Console.WriteLine();
Console.WriteLine($"Ortalama güven: {rapor.OrtalamaGuven}");
Console.WriteLine($"Ortalama süre : {rapor.OrtalamaSureMs} ms");

if (rapor.Yanlislar.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("=== YANLIŞLAR ===");
    foreach (var yanlis in rapor.Yanlislar)
    {
        Console.WriteLine($"  {yanlis.Dosya} ({yanlis.Zorluk})");
        if (yanlis.Hata != null)
        {
            Console.WriteLine($"    HATA: {yanlis.Hata}");
        }
        foreach (var fark in yanlis.Farklar)
        {
            Console.WriteLine($"    {fark}");
        }
    }
}

var simdi = DateTime.Now;
var raporYolu = Path.Combine(klasor, $"kalibrasyon-{simdi:yyyy-MM-dd-HHmm}.md");
await File.WriteAllTextAsync(raporYolu, Rapor.Markdown(rapor, simdi), new UTF8Encoding(true));
Console.WriteLine();
Console.WriteLine($"Rapor yazıldı: {raporYolu}");

Console.WriteLine();
Console.WriteLine("=== SUNUCU /stats ÇAPRAZ KONTROL ===");
Console.WriteLine(await istemci.StatsAsync());

return 0;

static void Karsilastir(DosyaSonucu sonuc, string alan, bool esit, string beklenen, string gelen)
{
    sonuc.AlanDogru[alan] = esit;
    if (!esit)
    {
        sonuc.Farklar.Add($"{alan}: beklenen '{beklenen ?? "yok"}' → gelen '{gelen ?? "yok"}'");
    }
}
