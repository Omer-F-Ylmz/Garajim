using Garajim.Business.Concrete.Evraklar;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Unit
{
    public static class TestEvrakKurallari
    {
        public static EvrakKurallari Olustur()
        {
            return new EvrakKurallari(new ConfigurationBuilder().Build());
        }
    }
}
