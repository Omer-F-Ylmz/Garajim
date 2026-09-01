using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete.Planlar
{
    public class PlanKurallari
    {
        public const int VarsayilanBireyselLimit = 3;
        public const int VarsayilanFiloLimit = 25;
        public const int VarsayilanDavetOdulGun = 30;

        private readonly IConfiguration _configuration;

        public PlanKurallari(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int AracLimiti(PlanType plan, int? sirketLimiti)
        {
            if (sirketLimiti != null && sirketLimiti.Value > 0)
            {
                return sirketLimiti.Value;
            }

            var anahtar = plan == PlanType.Filo ? "Plan:FiloAracLimiti" : "Plan:BireyselAracLimiti";
            var varsayilan = plan == PlanType.Filo ? VarsayilanFiloLimit : VarsayilanBireyselLimit;

            if (int.TryParse(_configuration[anahtar], out var limit) && limit > 0)
            {
                return limit;
            }

            return varsayilan;
        }

        public int DavetOdulGun()
        {
            if (int.TryParse(_configuration["Plan:DavetOdulGun"], out var gun) && gun > 0)
            {
                return gun;
            }

            return VarsayilanDavetOdulGun;
        }
    }
}
