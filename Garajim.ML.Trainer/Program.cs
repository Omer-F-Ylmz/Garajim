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

Console.WriteLine();
Console.WriteLine("Model eğitiliyor (FastTree regresyon, %80 eğitim / %20 test)...");

var trainer = new PriceModelTrainer();
var result = trainer.Train(data.Samples);

Console.WriteLine();
Console.WriteLine($"Eğitim satırı     : {result.TrainRowCount.ToString("N0", culture)}");
Console.WriteLine($"Test satırı       : {result.TestRowCount.ToString("N0", culture)}");
Console.WriteLine($"R²                : {result.RSquared.ToString("F4", culture)}");
Console.WriteLine($"MAE               : {result.MeanAbsoluteError.ToString("N0", culture)} TL");
Console.WriteLine($"RMSE              : {result.RootMeanSquaredError.ToString("N0", culture)} TL");

var engine = trainer.CreatePredictionEngine(result.Model);
var ornek = new CarPriceInput
{
    Marka = "Renault",
    Seri = "Clio",
    Yil = 2018,
    Kilometre = 120000,
    YakitTipi = "Benzin",
    VitesTipi = "Düz",
    KasaTipi = "Hatchback/5"
};

var tahmin = engine.Predict(ornek);
Console.WriteLine();
Console.WriteLine($"Örnek: {ornek.Yil:F0} {ornek.Marka} {ornek.Seri}, {ornek.Kilometre.ToString("N0", culture)} km, {ornek.YakitTipi}, {ornek.VitesTipi}, {ornek.KasaTipi}");
Console.WriteLine($"Tahmin: {tahmin.TahminiFiyat.ToString("N0", culture)} TL");

trainer.Save(result, modelPath);
Console.WriteLine();
Console.WriteLine($"Model kaydedildi: {modelPath}");
Console.WriteLine($"Boyut: {(new FileInfo(modelPath).Length / 1024d / 1024d).ToString("F2", culture)} MB");

return 0;

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
