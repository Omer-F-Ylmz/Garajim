using Garajim.KatalogUretici;

namespace Garajim.Tests.Unit
{
    public class KatalogUreticiTests
    {
        private static string DepoKoku()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok.FullName;
        }

        private static string TrKatalogYolu() =>
            Path.Combine(DepoKoku(), "Garajim.Business", "Katalog", "arac-katalogu.json");

        // ---------- MetinTemizleyici ----------

        [Theory]
        [InlineData("TOYOTA", "Toyota")]
        [InlineData("toyota", "Toyota")]
        [InlineData("  Land   Rover  ", "Land Rover")]
        [InlineData("mercedes-benz", "Mercedes-Benz")]
        [InlineData("ALFA ROMEO", "Alfa Romeo")]
        public void TemizleyiciBaslikBicimineCevirir(string ham, string beklenen)
        {
            Assert.Equal(beklenen, MetinTemizleyici.Duzelt(ham));
        }

        [Theory]
        [InlineData("BMW")]
        [InlineData("GMC")]
        [InlineData("MG")]
        [InlineData("DS")]
        [InlineData("BYD")]
        [InlineData("VW")]
        [InlineData("SRT")]
        public void TemizleyiciKisaltmalariKorur(string kisaltma)
        {
            Assert.Equal(kisaltma, MetinTemizleyici.Duzelt(kisaltma));
            Assert.Equal(kisaltma, MetinTemizleyici.Duzelt(kisaltma.ToLowerInvariant()));
        }

        [Theory]
        [InlineData("2024")]
        [InlineData("  911  ")]
        [InlineData("X")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("NOT APPLICABLE")]
        [InlineData("not applicable")]
        [InlineData("N/A")]
        [InlineData("n/a")]
        public void TemizleyiciGecersizleriEler(string ham)
        {
            Assert.Null(MetinTemizleyici.Duzelt(ham));
        }

        [Fact]
        public void TemizleyiciElliKarakterUstunuEler()
        {
            var uzun = new string('A', MetinTemizleyici.EnUzunAd + 1);

            Assert.Null(MetinTemizleyici.Duzelt(uzun));
            Assert.NotNull(MetinTemizleyici.Duzelt(new string('A', MetinTemizleyici.EnUzunAd)));
        }

        [Theory]
        [InlineData("Model<script>")]
        [InlineData("Cee'd")]
        [InlineData("Seri (özel)")]
        [InlineData("Seri, ikinci")]
        [InlineData("Seri_alt")]
        public void TemizleyiciKarakterSetiDisinaCikaniEler(string ham)
        {
            Assert.Null(MetinTemizleyici.Duzelt(ham));
        }

        [Theory]
        [InlineData("Model 3", "Model 3")]
        [InlineData("C 200", "C 200")]
        [InlineData("1.6 TDI", "1.6 TDI")]
        [InlineData("A-Class", "A-Class")]
        [InlineData("XC 60/90", "XC 60/90")]
        [InlineData("R & T", "R & T")]
        public void TemizleyiciIzinliKarakterleriGecirir(string ham, string beklenen)
        {
            Assert.Equal(beklenen, MetinTemizleyici.Duzelt(ham));
        }

        // ---------- MarkaEslemesi ----------

        [Theory]
        [InlineData("Mercedes-Benz", "Mercedes - Benz")]
        [InlineData("MERCEDES-BENZ", "Mercedes - Benz")]
        [InlineData("Mercedes Benz", "Mercedes - Benz")]
        [InlineData("Citroën", "Citroen")]
        [InlineData("CITROEN", "Citroen")]
        [InlineData("Škoda", "Skoda")]
        [InlineData("SKODA", "Skoda")]
        [InlineData("VW", "Volkswagen")]
        [InlineData("Volkswagen", "Volkswagen")]
        [InlineData("Tofas", "Tofaş")]
        [InlineData("TOFAŞ", "Tofaş")]
        [InlineData("Rolls Royce", "Rolls-Royce")]
        [InlineData("DS", "DS Automobiles")]
        [InlineData("Iran Khodro", "Ikco")]
        [InlineData("Alfa Romeo", "Alfa Romeo")]
        public void MarkaEslemesiTrYazimiKanonikYapar(string vpic, string beklenen)
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());

            Assert.Equal(beklenen, MarkaEslemesi.Kanonik(vpic, tr));
        }

        [Theory]
        [InlineData("Land Rover", "Land Rover")]
        [InlineData("GMC", "GMC")]
        [InlineData("RAM", "RAM")]
        public void MarkaEslemesiTrdeOlmayaniOlduguGibiBirakir(string vpic, string beklenen)
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());

            Assert.Equal(beklenen, MarkaEslemesi.Kanonik(vpic, tr));
        }

        [Fact]
        public void MarkaEslemesiGecersizAdaNullDoner()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());

            Assert.Null(MarkaEslemesi.Kanonik("N/A", tr));
            Assert.Null(MarkaEslemesi.Kanonik("2024", tr));
        }

        // ---------- vPIC yanıt ayrıştırma (ağsız fixture) ----------

        [Fact]
        public void VpicMarkaYanitiAyristirilir()
        {
            const string json = @"{""Count"":3,""Message"":""Response returned successfully"",""Results"":[
                {""MakeId"":440,""MakeName"":""TOYOTA"",""VehicleTypeId"":2,""VehicleTypeName"":""Passenger Car""},
                {""MakeId"":441,""MakeName"":""mercedes-benz"",""VehicleTypeId"":2,""VehicleTypeName"":""Passenger Car""},
                {""MakeId"":442,""MakeName"":""N/A"",""VehicleTypeId"":2,""VehicleTypeName"":""Passenger Car""}]}";

            var markalar = VpicYanitAyristirici.Markalar(json);

            Assert.Equal(new[] { "TOYOTA", "mercedes-benz", "N/A" }, markalar);
        }

        [Fact]
        public void VpicModelYanitiAyristirilir()
        {
            const string json = @"{""Count"":2,""Results"":[
                {""Make_ID"":440,""Make_Name"":""TOYOTA"",""Model_ID"":1,""Model_Name"":""Corolla""},
                {""Make_ID"":440,""Make_Name"":""TOYOTA"",""Model_ID"":2,""Model_Name"":""Yaris""}]}";

            Assert.Equal(new[] { "Corolla", "Yaris" }, VpicYanitAyristirici.Modeller(json));
        }

        [Fact]
        public void VpicBosSonucBosListeDoner()
        {
            Assert.Empty(VpicYanitAyristirici.Modeller(@"{""Count"":0,""Results"":[]}"));
        }

        [Fact]
        public void VpicBozukYanitIstisnaAtar()
        {
            Assert.Throws<InvalidOperationException>(() => VpicYanitAyristirici.Markalar("{ bozuk"));
        }

        // ---------- Birleştirici ----------

        private static Dictionary<string, List<string>> SahteVpic() => new()
        {
            ["TOYOTA"] = new List<string> { "Corolla", "Yaris", "Supra", "N/A", "2024" },
            ["mercedes-benz"] = new List<string> { "C 200", "S 500" },
            ["Land Rover"] = new List<string> { "Defender", "Discovery" },
            ["GMC"] = new List<string> { "Sierra" },
        };

        [Fact]
        public void BirlestiriciTrMarkalariniTrOlarakIsaretler()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var belge = KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07");

            var toyota = belge.Markalar.Single(m => m.Ad == "Toyota");
            var landRover = belge.Markalar.Single(m => m.Ad == "Land Rover");

            Assert.True(toyota.Tr);
            Assert.False(landRover.Tr);
        }

        [Fact]
        public void BirlestiriciTrSerileriniTrOlarakIsaretler()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var belge = KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07");

            var toyota = belge.Markalar.Single(m => m.Ad == "Toyota");

            Assert.True(toyota.Seriler.Single(s => s.Ad == "Corolla").Tr);
            Assert.False(toyota.Seriler.Single(s => s.Ad == "Supra").Tr);
        }

        [Fact]
        public void BirlestiriciGecersizSerileriEler()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var belge = KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07");

            var toyota = belge.Markalar.Single(m => m.Ad == "Toyota");

            Assert.DoesNotContain(toyota.Seriler, s => s.Ad == "N/A");
            Assert.DoesNotContain(toyota.Seriler, s => s.Ad == "2024");
        }

        [Fact]
        public void BirlestiriciTumTrKatalogunuIcerir()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var belge = KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07");

            Assert.Equal(56, tr.Markalar.Count);
            Assert.Equal(391, tr.Markalar.Sum(m => m.Value.Count));

            foreach (var marka in tr.Markalar)
            {
                var global = belge.Markalar.SingleOrDefault(m => m.Ad == marka.Key);

                Assert.True(global != null, "Global katalogda eksik marka: " + marka.Key);
                Assert.True(global.Tr);

                foreach (var seri in marka.Value)
                {
                    Assert.True(global.Seriler.Any(s => s.Ad == seri && s.Tr),
                        "Global katalogda eksik seri: " + marka.Key + " / " + seri);
                }
            }
        }

        [Fact]
        public void BirlestiriciVpicYokkenDeTrKatalogunuUretir()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var belge = KatalogBirlestirici.Uret(tr, new Dictionary<string, List<string>>(), "test-1", "2026-09-07");

            Assert.Equal(56, belge.Markalar.Count);
            Assert.All(belge.Markalar, m => Assert.True(m.Tr));
            Assert.All(belge.Markalar, m => Assert.All(m.Seriler, s => Assert.True(s.Tr)));
        }

        [Fact]
        public void BirlestiriciFikirSabitidir()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());

            var bir = JsonYazici.Yaz(KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07"));
            var iki = JsonYazici.Yaz(KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07"));

            Assert.Equal(bir, iki);
        }

        [Fact]
        public void BirlestiriciSiralamayiSabitler()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var belge = KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07");

            var adlar = belge.Markalar.Select(m => m.Ad).ToList();

            Assert.Equal(adlar.OrderBy(a => a, StringComparer.Ordinal).ToList(), adlar);
            Assert.All(belge.Markalar, m =>
                Assert.Equal(m.Seriler.Select(s => s.Ad).OrderBy(a => a, StringComparer.Ordinal).ToList(),
                    m.Seriler.Select(s => s.Ad).ToList()));
        }

        [Fact]
        public void BirlestiriciSeriyiMarkaIcindeTekillestirir()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var vpic = new Dictionary<string, List<string>>
            {
                ["TOYOTA"] = new List<string> { "Corolla", "corolla", "COROLLA" },
            };

            var belge = KatalogBirlestirici.Uret(tr, vpic, "test-1", "2026-09-07");
            var toyota = belge.Markalar.Single(m => m.Ad == "Toyota");

            Assert.Single(toyota.Seriler.Where(s => string.Equals(s.Ad, "Corolla", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void BirlestiriciAyniSeriyiFarkliMarkalardaKabulEder()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var vpic = new Dictionary<string, List<string>>
            {
                ["Land Rover"] = new List<string> { "Sport" },
                ["GMC"] = new List<string> { "Sport" },
            };

            var belge = KatalogBirlestirici.Uret(tr, vpic, "test-1", "2026-09-07");

            Assert.Contains(belge.Markalar.Single(m => m.Ad == "Land Rover").Seriler, s => s.Ad == "Sport");
            Assert.Contains(belge.Markalar.Single(m => m.Ad == "GMC").Seriler, s => s.Ad == "Sport");
        }

        [Fact]
        public void BelgeSemasiBeklenenAlanlariTasir()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var metin = JsonYazici.Yaz(KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07"));

            Assert.Contains("\"surum\"", metin);
            Assert.Contains("\"kaynak\"", metin);
            Assert.Contains("\"uretimTarihi\"", metin);
            Assert.Contains("\"markalar\"", metin);
            Assert.Contains("\"tr\"", metin);
            Assert.Contains("\"seriler\"", metin);
        }

        [Fact]
        public void BoyutRaporuEnKalabalikMarkalariSiralar()
        {
            var tr = TrKatalogOkuyucu.Oku(TrKatalogYolu());
            var belge = KatalogBirlestirici.Uret(tr, SahteVpic(), "test-1", "2026-09-07");

            var rapor = JsonYazici.BoyutRaporu(belge, 5);

            Assert.Contains("bayt", rapor);
            Assert.Contains("Renault", rapor);
        }

        [Fact]
        public void DortMegabaytSinirSabitiVardir()
        {
            Assert.Equal(4 * 1024 * 1024, JsonYazici.EnBuyukBoyut);
        }

        // ---------- Ayarlar ----------

        [Fact]
        public void AyarlarVarsayilanlariTasir()
        {
            var ayarlar = Ayarlar.Coz(Array.Empty<string>());

            Assert.Equal(200, ayarlar.BeklemeMs);
            Assert.Equal(3, ayarlar.DenemeSayisi);
            Assert.Contains("arac-katalogu-global.json", ayarlar.CiktiYolu);
        }

        [Fact]
        public void AyarlarArgumanlariOkur()
        {
            var ayarlar = Ayarlar.Coz(new[] { "--bekle", "500", "--deneme", "5", "--cikti", "C:/x/y.json", "--onbellek", "C:/onbellek" });

            Assert.Equal(500, ayarlar.BeklemeMs);
            Assert.Equal(5, ayarlar.DenemeSayisi);
            Assert.Equal("C:/x/y.json", ayarlar.CiktiYolu);
            Assert.Equal("C:/onbellek", ayarlar.OnbellekKlasoru);
        }

        [Fact]
        public void AyarlarAracTipleriniTasir()
        {
            Assert.Equal(new[] { "car", "mpv", "truck" }, Ayarlar.AracTipleri);
        }
    }
}
