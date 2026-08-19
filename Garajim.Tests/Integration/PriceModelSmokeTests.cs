using Garajim.ML.Models;
using Microsoft.ML;

namespace Garajim.Tests.Integration
{
    public class PriceModelSmokeTests
    {
        private static string ModelYolu()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "Garajim.API", "MLModels", "price-model.zip");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static PredictionEngine<CarPriceInput, CarPricePrediction> MotorOlustur()
        {
            var path = ModelYolu();
            Assert.False(path == null, "price-model.zip bulunamadı.");

            var mlContext = new MLContext(seed: 1);
            using var stream = File.OpenRead(path);
            var model = mlContext.Model.Load(stream, out _);
            return mlContext.Model.CreatePredictionEngine<CarPriceInput, CarPricePrediction>(model);
        }

        private static CarPriceInput Ornek()
        {
            return new CarPriceInput
            {
                Marka = "Renault",
                Seri = "Clio",
                Yil = 2018,
                Kilometre = 120000,
                YakitTipi = "Benzin",
                VitesTipi = "Düz",
                KasaTipi = "Hatchback/5"
            };
        }

        [Fact]
        public void ModelDosyasiYuklenirVePozitifTahminDoner()
        {
            var engine = MotorOlustur();

            var tahmin = PriceScale.FromLog(engine.Predict(Ornek()).LogFiyat);

            Assert.True(tahmin > 0, "Tahmin pozitif olmalı.");
            Assert.False(float.IsNaN(tahmin));
            Assert.False(float.IsInfinity(tahmin));
        }

        [Fact]
        public void TahminMakulFiyatAraliginaDuser()
        {
            var engine = MotorOlustur();

            var tahmin = PriceScale.FromLog(engine.Predict(Ornek()).LogFiyat);

            Assert.InRange(tahmin, 100_000f, 50_000_000f);
        }

        [Fact]
        public void AyniGirdiIcinTahminDeterministiktir()
        {
            var engine = MotorOlustur();

            var ilk = engine.Predict(Ornek()).LogFiyat;
            var ikinci = engine.Predict(Ornek()).LogFiyat;

            Assert.Equal(ilk, ikinci);
        }

        [Fact]
        public void YuksekKilometreDahaDusukFiyatUretir()
        {
            var engine = MotorOlustur();

            var dusukKm = Ornek();
            dusukKm.Kilometre = 40000;
            var yuksekKm = Ornek();
            yuksekKm.Kilometre = 300000;

            var dusukTahmin = PriceScale.FromLog(engine.Predict(dusukKm).LogFiyat);
            var yuksekTahmin = PriceScale.FromLog(engine.Predict(yuksekKm).LogFiyat);

            Assert.True(dusukTahmin > yuksekTahmin, "Az kilometreli araç daha pahalı tahmin edilmeli.");
        }
    }
}
