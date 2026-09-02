using System.Text;

namespace Garajim.API.Startup
{
    public static class ProductionConfigurationGuard
    {
        public const int MinimumJwtKeyBytes = 32;

        private static readonly string[] Placeholders =
        {
            "placeholder", "changeme", "buraya", "degistir", "dev-ortami-varsayilan"
        };

        public static void Validate(IConfiguration configuration, IHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                return;
            }

            var hatalar = Topla(configuration);
            if (hatalar.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                "Üretim yapılandırması geçersiz, uygulama başlatılmadı:" + Environment.NewLine +
                " - " + string.Join(Environment.NewLine + " - ", hatalar));
        }

        public static List<string> Topla(IConfiguration configuration)
        {
            var hatalar = new List<string>();
            var connectionString = configuration.GetConnectionString("Default");
            var jwtKey = configuration["Jwt:Key"];

            if (Eksik(connectionString))
            {
                hatalar.Add("ConnectionStrings:Default tanımlı değil. Sunucuda ConnectionStrings__Default ortam değişkenini uzak MSSQL bağlantı cümlesiyle tanımlayın.");
            }
            else if (connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
            {
                hatalar.Add("ConnectionStrings:Default LocalDB'ye işaret ediyor. Üretimde LocalDB kullanılamaz; ConnectionStrings__Default ortam değişkeni uzak MSSQL sunucusunu göstermelidir.");
            }

            if (Eksik(jwtKey))
            {
                hatalar.Add("Jwt:Key tanımlı değil. Sunucuda Jwt__Key ortam değişkenini tanımlayın.");
            }
            else if (Encoding.UTF8.GetByteCount(jwtKey) < MinimumJwtKeyBytes)
            {
                hatalar.Add($"Jwt:Key en az {MinimumJwtKeyBytes} bayt olmalıdır; HMAC-SHA256 daha kısa anahtarı reddeder.");
            }

            if (bool.TryParse(configuration["Usta:SahteYanit"], out var sahteYanit) && sahteYanit)
            {
                hatalar.Add("Usta:SahteYanit üretimde açık olamaz. Bu bayrak açıkken AI Usta gerçek modeli çağırmaz ve kullanıcıya sabit sahte yanıt döner; Usta__SahteYanit ortam değişkenini kaldırın ya da false yapın.");
            }

            foreach (var anahtar in new[] { "Smtp:Host", "Smtp:User", "Smtp:From" })
            {
                if (Eksik(configuration[anahtar]))
                {
                    hatalar.Add(anahtar + " tanımlı değil. E-posta doğrulama kodu gönderilemezse hiç kimse kayıt olamaz; sunucuda " + anahtar.Replace(":", "__") + " ortam değişkenini tanımlayın.");
                }
            }

            if (Eksik(configuration["Smtp:Pass"]) && Eksik(configuration["Smtp:Password"]))
            {
                hatalar.Add("Smtp:Pass tanımlı değil. E-posta doğrulama kodu gönderilemezse hiç kimse kayıt olamaz; sunucuda Smtp__Pass ortam değişkenini tanımlayın.");
            }

            return hatalar;
        }


        private static bool Eksik(string deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return true;
            }

            return Placeholders.Any(yerTutucu => deger.Contains(yerTutucu, StringComparison.OrdinalIgnoreCase));
        }
    }
}
