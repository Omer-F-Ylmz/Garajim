namespace Garajim.Tests.Unit
{
    public class BildirimTercihiTests
    {
        private static string Oku(string gorece)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, gorece));
        }

        [Fact]
        public void HatirlatmaSorgusuTercihiSuzer()
        {
            var dal = Oku(Path.Combine("Garajim.Dal", "Concrete", "EfReminderDal.cs"));
            var baslangic = dal.IndexOf("GetDueListAsync", StringComparison.Ordinal);
            var bitis = dal.IndexOf("TryClaimNotificationAsync", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);
            Assert.Contains("u.BildirimHatirlatma", dal.Substring(baslangic, bitis - baslangic));
        }

        [Fact]
        public void EvrakAlicilariTercihiSuzer()
        {
            var job = Oku(Path.Combine("Garajim.Business", "Jobs", "ReminderNotificationJob.cs"));
            var baslangic = job.IndexOf("EvrakAlicilariAsync(EvrakDueDto", StringComparison.Ordinal);
            var bitis = job.IndexOf("SirketIcinCalistirAsync()", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);
            Assert.Contains("BildirimEvrak", job.Substring(baslangic, bitis - baslangic));
        }
    }
}
