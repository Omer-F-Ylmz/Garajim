using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Garajim.API.Controllers;
using Garajim.API.Startup;
using Garajim.Business.Abstract;
using Garajim.Core.Multitenancy;
using Garajim.Business.Concrete;
using Garajim.Business.Concrete.Receipts;
using Garajim.Business.Usta;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Concrete.Planlar;
using Garajim.Business.Constants;
using Garajim.Business.Jobs;
using Garajim.Business.Katalog;
using Garajim.Business.Seed;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete;
using Garajim.Dal.Concrete.Context;
using Garajim.ML.Models;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default");

ProductionConfigurationGuard.Validate(builder.Configuration, builder.Environment);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantProvider>(provider => provider.GetRequiredService<TenantContext>());

builder.Services.AddDbContext<GarajimDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<ICompanyDal, EfCompanyDal>();
builder.Services.AddScoped<IUserDal, EfUserDal>();
builder.Services.AddScoped<IVehicleDal, EfVehicleDal>();
builder.Services.AddScoped<IMaintenanceDal, EfMaintenanceDal>();
builder.Services.AddScoped<IFuelDal, EfFuelDal>();
builder.Services.AddScoped<IKmDuzeltmeLogDal, EfKmDuzeltmeLogDal>();
builder.Services.AddSingleton<ISaat, Saat>();
builder.Services.AddSingleton(sp => AracKatalogu.Yukle(Path.Combine(AppContext.BaseDirectory, AracKatalogu.KlasorAdi)));
builder.Services.AddScoped<IHesapService, HesapManager>();
builder.Services.AddScoped<IAiTokenDal, EfAiTokenDal>();
builder.Services.AddScoped<IAiButcesi, AiButcesi>();
builder.Services.AddScoped<HesapSilmeJob>();
builder.Services.AddScoped<FisTemizlemeJob>();
builder.Services.AddScoped<KatalogEslemeJob>();
builder.Services.AddScoped<DemoSifirlamaJob>();
builder.Services.AddScoped<IExpenseDal, EfExpenseDal>();
builder.Services.AddScoped<IReminderDal, EfReminderDal>();
builder.Services.AddScoped<IVehicleAssignmentDal, EfVehicleAssignmentDal>();
builder.Services.AddScoped<IDocumentDal, EfDocumentDal>();
builder.Services.AddScoped<IReceiptDraftDal, EfReceiptDraftDal>();
builder.Services.AddScoped<IMaintenancePartDal, EfMaintenancePartDal>();
builder.Services.AddScoped<IKarnePaylasimiDal, EfKarnePaylasimiDal>();
builder.Services.AddScoped<IEvrakDal, EfEvrakDal>();
builder.Services.AddScoped<ITakvimAbonelikDal, EfTakvimAbonelikDal>();
builder.Services.AddScoped<IImportKaydiDal, EfImportKaydiDal>();
builder.Services.AddScoped<IYolculukDal, EfYolculukDal>();
builder.Services.AddScoped<IUstaSohbetDal, EfUstaSohbetDal>();
builder.Services.AddScoped<IUstaMesajDal, EfUstaMesajDal>();
builder.Services.AddScoped<IUstaOnayDal, EfUstaOnayDal>();
builder.Services.AddScoped<IUstaCozumOzetiDal, EfUstaCozumOzetiDal>();
builder.Services.AddScoped<ILastikDal, EfLastikDal>();
builder.Services.AddScoped<IHasarDosyasiDal, EfHasarDosyasiDal>();
builder.Services.AddScoped<IHasarFotoDal, EfHasarFotoDal>();
builder.Services.AddScoped<IAracDegerDal, EfAracDegerDal>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

builder.Services.AddScoped<IAuthService, AuthManager>();
builder.Services.AddScoped<ITeamService, TeamManager>();
builder.Services.AddScoped<IVehicleAccessService, VehicleAccessManager>();
builder.Services.AddScoped<IAssignmentService, AssignmentManager>();
builder.Services.AddScoped<IDocumentService, DocumentManager>();
builder.Services.AddScoped<IReceiptService, ReceiptManager>();
builder.Services.AddScoped<IPartMemoryService, PartMemoryManager>();
builder.Services.AddScoped<IKarneService, KarneManager>();
builder.Services.AddScoped<EvrakKurallari>();
builder.Services.AddScoped<PlanKurallari>();
builder.Services.AddScoped<IEvrakService, EvrakManager>();
builder.Services.AddScoped<ITakvimService, TakvimManager>();
builder.Services.AddScoped<IImportService, ImportManager>();
builder.Services.AddScoped<IExportService, ExportManager>();
builder.Services.AddScoped<IYolculukService, YolculukManager>();
builder.Services.AddScoped<ILastikService, LastikManager>();
builder.Services.AddScoped<IHasarService, HasarManager>();
builder.Services.AddScoped<IDegerService, DegerManager>();
builder.Services.AddScoped<IDegerTahminEdici, MlDegerTahminEdici>();
builder.Services.AddScoped<IDavetService, DavetManager>();
builder.Services.AddScoped<IPlanService, PlanManager>();
builder.Services.AddScoped<IUstaService, UstaManager>();
builder.Services.AddScoped<IVehicleService, VehicleManager>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceManager>();
builder.Services.AddScoped<IFuelService, FuelManager>();
builder.Services.AddScoped<IExpenseService, ExpenseManager>();
builder.Services.AddScoped<IReminderService, ReminderManager>();
builder.Services.AddScoped<IReportService, ReportManager>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IKodGonderimSayaci, BellekKodGonderimSayaci>();
builder.Services.AddHttpClient(ReceiptExtractorBase.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient(GeminiUstaIstemci.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(40));
if (builder.Configuration.GetValue("Usta:SahteYanit", false))
{
    builder.Services.AddSingleton<IUstaIstemci, SahteUstaIstemcisi>();
}
else
{
    builder.Services.AddScoped<IUstaIstemci, GeminiUstaIstemci>();
}
builder.Services.AddSingleton(sp =>
{
    var taban = AppContext.BaseDirectory;
    var kayitlar = new BilgiYukleyici().Yukle(Path.Combine(taban, BilgiYukleyici.KlasorAdi));
    var promptYolu = Path.Combine(taban, "Usta", "SistemPromptu.md");
    if (!File.Exists(promptYolu))
    {
        throw new InvalidOperationException("AI Usta sistem promptu bulunamadi: " + promptYolu);
    }
    return new UstaBilgiDeposu(kayitlar, File.ReadAllText(promptYolu));
});
builder.Services.AddScoped<GeminiReceiptExtractor>();
builder.Services.AddScoped<OpenAiReceiptExtractor>();
builder.Services.AddScoped<IReceiptExtractor>(provider =>
    string.Equals(provider.GetRequiredService<IConfiguration>()["Receipts:Provider"], "OpenAI", StringComparison.OrdinalIgnoreCase)
        ? provider.GetRequiredService<OpenAiReceiptExtractor>()
        : provider.GetRequiredService<GeminiReceiptExtractor>());
builder.Services.AddScoped<ReminderNotificationJob>();
builder.Services.AddScoped<UstaOzetJob>();
builder.Services.AddScoped<UstaSaklamaJob>();
builder.Services.AddScoped<DemoDataSeeder>();

builder.Services.AddSingleton(sp => FiyatModeliSozlugu.Yukle(Path.Combine(AppContext.BaseDirectory, "MLModels", "price-model.zip")));
builder.Services.AddSingleton(sp => new Lazy<FiyatModeliSozlugu>(sp.GetRequiredService<FiyatModeliSozlugu>));
builder.Services.AddSingleton(sp => new Lazy<PredictionEnginePool<CarPriceInput, CarPricePrediction>>(sp.GetRequiredService<PredictionEnginePool<CarPriceInput, CarPricePrediction>>));

builder.Services.AddPredictionEnginePool<CarPriceInput, CarPricePrediction>()
    .FromFile(
        modelName: PricePredictionController.ModelName,
        filePath: "MLModels/price-model.zip",
        watchForChanges: builder.Environment.IsDevelopment());

var useBackgroundJobs = builder.Configuration.GetValue("UseBackgroundJobs", true);

if (useBackgroundJobs)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString));
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = builder.Configuration.GetValue("Hangfire:WorkerCount", 1);
    });
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = TokenGecerlilikDenetimi.DenetleAsync
        };
    });
builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();

    var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
    foreach (var proxy in knownProxies)
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }

    var knownNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();
    foreach (var network in knownNetworks)
    {
        var parts = network.Split('/');
        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var length))
        {
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length));
        }
    }

    options.ForwardLimit = builder.Configuration.GetValue("ForwardedHeaders:ForwardLimit", 1);
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = PahaliUclar.MaxIstekGovdesi(builder.Configuration);
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = PahaliUclar.MaxIstekGovdesi(builder.Configuration);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(KarneController.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(PahaliUclar.RateLimitPolicy, httpContext =>
    {
        var pahaliYapilandirma = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var pahaliLimit = pahaliYapilandirma.GetValue("RateLimiting:PahaliUcPerMinute", 20);

        return RateLimitPartition.GetFixedWindowLimiter(
            PahaliUclar.Bolum(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = pahaliLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy(AuthController.RateLimitPolicy, httpContext =>
    {
        var rateLimitConfiguration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = rateLimitConfiguration.GetValue("RateLimiting:AuthPermitPerMinute", 10);

        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { data = (object)null, success = false, message = Messages.TooManyRequests },
            cancellationToken);
    };
});

builder.Services.AddControllers(options => options.Filters.Add<DurumKoduFiltresi>()).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Garajim API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Login cevabındaki token değerini girin."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseGuvenlikBasliklari(app.Configuration);
app.UseSurumluServiceWorker(app.Environment);

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

if (builder.Configuration.GetValue("ApplyMigrationsAtStartup", false))
{
    using (var migrationScope = app.Services.CreateScope())
    {
        var migrationLogger = migrationScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var migrationContext = migrationScope.ServiceProvider.GetRequiredService<GarajimDbContext>();

        try
        {
            var bekleyenler = migrationContext.Database.GetPendingMigrations().ToList();

            if (bekleyenler.Count == 0)
            {
                migrationLogger.LogInformation("ApplyMigrationsAtStartup açık, bekleyen migration yok.");
            }
            else
            {
                migrationLogger.LogInformation(
                    "ApplyMigrationsAtStartup açık, {Adet} migration uygulanıyor: {Migrationlar}",
                    bekleyenler.Count,
                    string.Join(", ", bekleyenler));

                migrationContext.Database.Migrate();

                migrationLogger.LogInformation("Migration tamamlandı, veritabanı şeması güncel.");
            }
        }
        catch (Exception migrationException)
        {
            migrationLogger.LogCritical(
                migrationException,
                "Migration uygulanamadı. ConnectionStrings__Default değerinin doğru sunucuyu gösterdiğini ve kullanıcının şema değiştirme yetkisi olduğunu doğrulayın. Uygulama başlatılmıyor.");
            throw;
        }
    }
}

if (builder.Configuration.GetValue("Katalog:BaslangictaEsle", true))
{
    using (var eslemeScope = app.Services.CreateScope())
    {
        var eslemeLogger = eslemeScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var guncellenen = await eslemeScope.ServiceProvider.GetRequiredService<KatalogEslemeJob>().RunAsync();

            eslemeLogger.LogInformation(
                "Katalog eşlemesi tamamlandı, {Adet} aracın marka/model alanı güncellendi.",
                guncellenen);
        }
        catch (Exception eslemeException)
        {
            eslemeLogger.LogError(eslemeException, "Katalog eşlemesi çalıştırılamadı. Uygulama eşleme yapılmadan devam ediyor.");
        }
    }
}

if (builder.Configuration.GetValue("DemoSeed:Enabled", false))
{
    using (var seedScope = app.Services.CreateScope())
    {
        var seedLogger = seedScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var eklendi = await seedScope.ServiceProvider.GetRequiredService<DemoDataSeeder>().RunAsync();

            seedLogger.LogInformation(
                eklendi
                    ? "DemoSeed:Enabled açık, demo verisi oluşturuldu ({Eposta})."
                    : "DemoSeed:Enabled açık, demo verisi zaten mevcut ({Eposta}), atlandı.",
                DemoDataSeeder.DemoEmail);
        }
        catch (Exception seedException)
        {
            seedLogger.LogError(seedException, "Demo verisi oluşturulamadı. Uygulama demo verisi olmadan devam ediyor.");
        }
    }
}

if (app.Configuration.GetValue("Swagger:Enabled", !app.Environment.IsProduction()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var headers = context.Context.Response.GetTypedHeaders();
        var ad = context.File.Name;
        var surumsuz = ad.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || ad.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            || ad.EndsWith(".css", StringComparison.OrdinalIgnoreCase);

        headers.CacheControl = surumsuz
            ? new CacheControlHeaderValue { NoCache = true, MustRevalidate = true }
            : new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromHours(1) };
    }
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

if (useBackgroundJobs && app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();

if (useBackgroundJobs)
{
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var trSaati = new RecurringJobOptions { TimeZone = Saat.Dilim };

        recurringJobManager.AddOrUpdate<ReminderNotificationJob>(
            "reminder-notifications",
            job => job.RunAsync(),
            Cron.Daily(6),
            trSaati);

        recurringJobManager.AddOrUpdate<UstaOzetJob>(
            "usta-cozum-ozeti",
            job => job.RunAsync(),
            Cron.Daily(4),
            trSaati);

        recurringJobManager.AddOrUpdate<DemoSifirlamaJob>(
            "demo-sifirlama",
            job => job.RunAsync(),
            Cron.Daily(3, 30),
            trSaati);

        recurringJobManager.AddOrUpdate<HesapSilmeJob>(
            "hesap-silme",
            job => job.RunAsync(),
            Cron.Daily(3),
            trSaati);

        recurringJobManager.AddOrUpdate<FisTemizlemeJob>(
            "fis-temizleme",
            job => job.RunAsync(),
            Cron.Daily(4, 30),
            trSaati);

        recurringJobManager.AddOrUpdate<UstaSaklamaJob>(
            "usta-saklama",
            job => job.RunAsync(),
            Cron.Daily(5),
            trSaati);
    }
}

app.Run();

public partial class Program
{
}
