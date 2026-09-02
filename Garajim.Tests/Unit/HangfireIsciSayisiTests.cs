namespace Garajim.Tests.Unit
{
    public class HangfireIsciSayisiTests
    {
        private static string DepoKoku()
        {
            var klasor = new DirectoryInfo(AppContext.BaseDirectory);

            while (klasor != null && !File.Exists(Path.Combine(klasor.FullName, "Garajim.sln")))
            {
                klasor = klasor.Parent;
            }

            Assert.NotNull(klasor);
            return klasor.FullName;
        }

        private static string ProgramMetni() => File.ReadAllText(Path.Combine(DepoKoku(), "Garajim.API", "Program.cs"));

        [Fact]
        public void HangfireSunucusuIsciSayisiniAcikcaBelirler()
        {
            var metin = ProgramMetni();

            Assert.DoesNotContain("AddHangfireServer();", metin);
            Assert.Contains("options.WorkerCount", metin);
        }

        [Fact]
        public void IsciSayisiVarsayilaniBirdir()
        {
            var metin = ProgramMetni();

            Assert.Contains("GetValue(\"Hangfire:WorkerCount\", 1)", metin);
        }

        [Fact]
        public void IsciSayisiDegiskeniBelgelenmis()
        {
            var belgeler = File.ReadAllText(Path.Combine(DepoKoku(), "README.md"))
                + File.ReadAllText(Path.Combine(DepoKoku(), "DEPLOY.md"));

            Assert.Contains("Hangfire__WorkerCount", belgeler);
        }
    }
}
