using Garajim.KatalogUretici;

var ayarlar = Ayarlar.Coz(args);
var iptal = new CancellationTokenSource();

Console.CancelKeyPress += (_, olay) =>
{
    olay.Cancel = true;
    iptal.Cancel();
};

void Yaz(string satir) => Console.WriteLine(satir);

Yaz("Garajim katalog ureticisi");
Yaz("  TR katalog : " + ayarlar.TrKatalogYolu);
Yaz("  cikti      : " + ayarlar.CiktiYolu);
Yaz("  onbellek   : " + ayarlar.OnbellekKlasoru);
Yaz("  bekleme    : " + ayarlar.BeklemeMs + " ms, deneme: " + ayarlar.DenemeSayisi);
Yaz(string.Empty);

TrKatalog tr;

try
{
    tr = TrKatalogOkuyucu.Oku(ayarlar.TrKatalogYolu);
}
catch (Exception hata)
{
    Console.Error.WriteLine("TR katalogu okunamadi: " + hata.Message);
    return 2;
}

Yaz("TR katalog: " + tr.Markalar.Count + " marka, " + tr.Markalar.Sum(m => m.Value.Count) + " seri");

using var istemci = new VpicIstemci(ayarlar.DenemeSayisi, ayarlar.BeklemeMs, Yaz);
var kaynak = new OnbellekliKaynak(istemci, ayarlar.OnbellekKlasoru, Yaz);

var markaAdlari = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

foreach (var tip in Ayarlar.AracTipleri)
{
    try
    {
        var yanit = await kaynak.MarkaYanitiAsync(tip, iptal.Token);
        var liste = VpicYanitAyristirici.Markalar(yanit);

        foreach (var ad in liste)
        {
            markaAdlari.Add(ad);
        }

        Yaz("Arac tipi " + tip + ": " + liste.Count + " marka (birikimli " + markaAdlari.Count + ")");
    }
    catch (Exception hata)
    {
        Console.Error.WriteLine("Arac tipi " + tip + " alinamadi: " + hata.Message);
        return 3;
    }

    await Task.Delay(ayarlar.BeklemeMs, iptal.Token);
}

var vpic = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
var sira = 0;
var hatali = 0;

foreach (var marka in markaAdlari)
{
    sira++;

    if (iptal.IsCancellationRequested)
    {
        Yaz("Iptal edildi; onbellek korundu, yeniden calistirinca kaldigi yerden devam eder.");
        return 4;
    }

    try
    {
        var yanit = await kaynak.ModelYanitiAsync(marka, iptal.Token);
        var modeller = VpicYanitAyristirici.Modeller(yanit);

        vpic[marka] = modeller;

        Yaz("[" + sira + "/" + markaAdlari.Count + "] " + marka + ": " + modeller.Count + " model");
    }
    catch (Exception hata)
    {
        hatali++;
        Console.Error.WriteLine("[" + sira + "/" + markaAdlari.Count + "] " + marka + " alinamadi: " + hata.Message);
    }

    await Task.Delay(ayarlar.BeklemeMs, iptal.Token);
}

var surum = ayarlar.Surum ?? (DateTime.UtcNow.ToString("yyyy-MM") + "-g1");
var uretimTarihi = DateTime.UtcNow.ToString("yyyy-MM-dd");

var belge = KatalogBirlestirici.Uret(tr, vpic, surum, uretimTarihi);

Yaz(string.Empty);
Yaz(JsonYazici.BoyutRaporu(belge, 15));

if (hatali > 0)
{
    Yaz("UYARI: " + hatali + " marka alinamadi; onbellek durdugu icin yeniden calistirmak yalniz eksikleri ceker.");
}

var eksik = new List<string>();

foreach (var marka in tr.Markalar)
{
    var global = belge.Markalar.FirstOrDefault(m => string.Equals(m.Ad, marka.Key, StringComparison.OrdinalIgnoreCase));

    if (global == null)
    {
        eksik.Add(marka.Key);
        continue;
    }

    foreach (var seri in marka.Value)
    {
        if (!global.Seriler.Any(s => string.Equals(s.Ad, seri, StringComparison.OrdinalIgnoreCase)))
        {
            eksik.Add(marka.Key + " / " + seri);
        }
    }
}

if (eksik.Count > 0)
{
    Console.Error.WriteLine("TR katalogundan eksik kalan " + eksik.Count + " kayit var; cikti yazilmadi:");
    foreach (var ad in eksik.Take(20))
    {
        Console.Error.WriteLine("  " + ad);
    }

    return 5;
}

if (JsonYazici.SinirAsildi(belge))
{
    Console.Error.WriteLine("Katalog " + JsonYazici.EnBuyukBoyut + " bayt sinirini asti; cikti yazilmadi.");
    Console.Error.WriteLine("Yukaridaki marka basina model sayisina bakip filtreyi sikilastirin.");
    return 6;
}

Directory.CreateDirectory(Path.GetDirectoryName(ayarlar.CiktiYolu) ?? ".");
await File.WriteAllTextAsync(ayarlar.CiktiYolu, JsonYazici.Yaz(belge));

Yaz("Yazildi: " + ayarlar.CiktiYolu);

return 0;
