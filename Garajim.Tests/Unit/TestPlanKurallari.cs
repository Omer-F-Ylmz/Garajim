using Garajim.Business.Concrete.Planlar;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Unit
{
    public static class TestPlanKurallari
    {
        public static PlanKurallari Olustur()
        {
            return new PlanKurallari(new ConfigurationBuilder().Build());
        }
    }
}
