using System.Globalization;
using System.Text;
using Garajim.ML.DataPrep;
using Garajim.ML.Models;
using Garajim.ML.Training;

Console.OutputEncoding = Encoding.UTF8;
var culture = new CultureInfo("tr-TR");

var solutionRoot = FindSolutionRoot();
var dataPath = args.Length > 0
    ? args[0]
    : Path.Combine(solutionRoot, "Garajim.ML", "Data", "cars.csv");
var modelPath = args.Length > 1
    ? args[1]
    : Path.Combine(solutionRoot, "Garajim.API", "MLModels", "price-model.zip");

if (!File.Exists(dataPath))
{
    Console.WriteLine($"Veri dosyası bulunamadı: {dataPath}");
    Console.WriteLine("cars.csv dosyasını Garajim.ML/Data/ klasörüne koyun (README'deki Kaggle bağlantısı).");
    return 1;
}

Console.WriteLine($"Veri okunuyor: {dataPath}");
var data = CarCsvLoader.Load(dataPath);

Console.WriteLine($"Okunan satır      : {data.TotalRows.ToString("N0", culture)}");
Console.WriteLine($"Eksik/bozuk       : {data.InvalidRows.ToString("N0", culture)}");
Console.WriteLine($"Aralık dışı       : {data.OutOfRangeRows.ToString("N0", culture)}");
Console.WriteLine($"Kopya             : {data.DuplicateRows.ToString("N0", culture)}");
Console.WriteLine($"Eğitime kalan     : {data.KeptRows.ToString("N0", culture)}");

if (data.KeptRows < 100)
{
    Console.WriteLine("Temizlik sonrası yeterli satır kalmadı, eğitim yapılmadı.");
    return 1;
}

var trainer = new PriceModelTrainer();
var split = trainer.Split(data.Samples);

Console.WriteLine();
Console.WriteLine("Aynı %80/%20 ayrımı üzerinde iki model eğitiliyor (tek değişken: hedef kolon).");

Console.WriteLine("[1/2] hedef = fiyat (TL)");
var tlModel = trainer.Train(split, PriceTarget.Tl);

Console.WriteLine("[2/2] hedef = log(fiyat)");
var logModel = trainer.Train(split, PriceTarget.Log);

Console.WriteLine();
Console.WriteLine($"Eğitim satırı: {tlModel.TrainRowCount.ToString("N0", culture)}   Test satırı: {tlModel.TestRowCount.ToString("N0", culture)}");
Console.WriteLine();
Console.WriteLine("Metrikler (test seti)");
Console.WriteLine($"{"",-24}{"hedef = TL",18}{"hedef = log(fiyat)",22}");
Console.WriteLine($"{"R² (hedef ölçeğinde)",-24}{FormatMetric(tlModel.RSquared, tlModel.RSquaredScale),18}{FormatMetric(logModel.RSquared, logModel.RSquaredScale),22}");
Console.WriteLine($"{"MAE (TL)",-24}{tlModel.MeanAbsoluteErrorTl.ToString("N0", culture),18}{logModel.MeanAbsoluteErrorTl.ToString("N0", culture),22}");
Console.WriteLine($"{"RMSE (TL)",-24}{tlModel.RootMeanSquaredErrorTl.ToString("N0", culture),18}{logModel.RootMeanSquaredErrorTl.ToString("N0", culture),22}");

var tlEngine = trainer.CreatePredictionEngine(tlModel.Model);
var logEngine = trainer.CreatePredictionEngine(logModel.Model);

var ornekler = new[]
{
    new CarPriceInput
    {
        Marka = "Renault",
        Seri = "Clio",
        Yil = 2018,
        Kilometre = 120000,
        YakitTipi = "Benzin",
        VitesTipi = "Düz",
        KasaTipi = "Hatchback/5"
    },
    new CarPriceInput
    {
        Marka = "Volkswagen",
        Seri = "Passat",
        Yil = 2015,
        Kilometre = 200000,
        YakitTipi = "Dizel",
        VitesTipi = "Otomatik",
        KasaTipi = "Sedan"
    }
};

Console.WriteLine();
Console.WriteLine("Örnek tahminler");
Console.WriteLine($"{"",-46}{"hedef = TL",18}{"hedef = log(fiyat)",22}");

foreach (var ornek in ornekler)
{
    var tlTahmin = trainer.Predict(tlEngine, ornek, PriceTarget.Tl);
    var logTahmin = trainer.Predict(logEngine, ornek, PriceTarget.Log);
    var etiket = $"{ornek.Yil:F0} {ornek.Marka} {ornek.Seri}, {ornek.Kilometre.ToString("N0", culture)} km, {ornek.YakitTipi}, {ornek.VitesTipi}, {ornek.KasaTipi}";

    Console.WriteLine($"{etiket,-46}{tlTahmin.ToString("N0", culture) + " TL",18}{logTahmin.ToString("N0", culture) + " TL",22}");
}

trainer.Save(logModel, modelPath);
Console.WriteLine();
Console.WriteLine($"Kaydedilen model: log(fiyat) hedefli -> {modelPath}");
Console.WriteLine($"Boyut: {(new FileInfo(modelPath).Length / 1024d / 1024d).ToString("F2", culture)} MB");

return 0;

string FormatMetric(double value, string scale)
{
    return $"{value.ToString("F4", culture)} ({scale})";
}

static string FindSolutionRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Garajim.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return Directory.GetCurrentDirectory();
}
