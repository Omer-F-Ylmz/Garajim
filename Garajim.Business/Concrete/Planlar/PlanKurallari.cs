using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete.Planlar
{
    public class PlanKurallari
    {
        public const int VarsayilanBireyselLimit = 3;
        public const int VarsayilanFiloLimit = 25;
        public const int VarsayilanDavetMaxEkArac = 3;
        public const int VarsayilanBireyselFisLimiti = 100;
        public const int VarsayilanFiloFisLimiti = 500;

        private readonly IConfiguration _configuration;

        public PlanKurallari(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int AracLimiti(PlanType plan, int? sirketLimiti, int davetSayisi)
        {
            return TabanLimit(plan, sirketLimiti) + KazanilanAracHakki(plan, davetSayisi);
        }

        public int KazanilanAracHakki(PlanType plan, int davetSayisi)
        {
            if (plan != PlanType.Bireysel || davetSayisi <= 0)
            {
                return 0;
            }

            return Math.Min(davetSayisi, DavetMaxEkArac());
        }

        public int AylikFisLimiti(PlanType plan)
        {
            var anahtar = plan == PlanType.Filo ? "Receipts:AylikLimitFilo" : "Receipts:AylikLimit";
            var varsayilan = plan == PlanType.Filo ? VarsayilanFiloFisLimiti : VarsayilanBireyselFisLimiti;

            if (int.TryParse(_configuration[anahtar], out var limit) && limit > 0)
            {
                return limit;
            }

            return varsayilan;
        }

        public long AylikTokenTavani()
        {
            return long.TryParse(_configuration["Ai:AylikTokenTavani"], out var tavan) && tavan > 0 ? tavan : 0L;
        }

        public int DavetMaxEkArac()
        {
            if (int.TryParse(_configuration["Plan:DavetMaxEkArac"], out var adet) && adet >= 0)
            {
                return adet;
            }

            return VarsayilanDavetMaxEkArac;
        }

        private int TabanLimit(PlanType plan, int? sirketLimiti)
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
    }
}
