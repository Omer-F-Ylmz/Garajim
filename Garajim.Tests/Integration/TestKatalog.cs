using Garajim.Business.Katalog;

namespace Garajim.Tests.Integration
{
    public static class TestKatalog
    {
        private static readonly Lazy<AracKatalogu> Ortak = new Lazy<AracKatalogu>(() =>
            AracKatalogu.Yukle(Path.Combine(AppContext.BaseDirectory, AracKatalogu.KlasorAdi)));

        public static AracKatalogu Yukle() => Ortak.Value;
    }
}
