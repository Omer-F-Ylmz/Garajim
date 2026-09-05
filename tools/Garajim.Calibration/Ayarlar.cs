using System.Globalization;

namespace Garajim.Calibration
{
    public static class Ayarlar
    {
        public const int VarsayilanBeklemeMs = 7000;

        public static string ArgumanOku(string[] args, string ad)
        {
            if (args == null)
            {
                return null;
            }

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ad)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        public static int BeklemeOku(string[] args)
        {
            var ham = ArgumanOku(args, "--bekle");

            if (string.IsNullOrWhiteSpace(ham))
            {
                return VarsayilanBeklemeMs;
            }

            if (!int.TryParse(ham, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milisaniye) || milisaniye < 0)
            {
                return VarsayilanBeklemeMs;
            }

            return milisaniye;
        }
    }
}
