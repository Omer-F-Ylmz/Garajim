using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Garajim.API.Controllers;
using Garajim.API.Startup;
using Garajim.Business.Abstract;
using Garajim.Core.Multitenancy;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Business.Jobs;
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
builder.Services.AddScoped<IExpenseDal, EfExpenseDal>();
builder.Services.AddScoped<IReminderDal, EfReminderDal>();
builder.Services.AddScoped<IVehicleAssignmentDal, EfVehicleAssignmentDal>();
builder.Services.AddScoped<IDocumentDal, EfDocumentDal>();

builder.Services.AddScoped<IAuthService, AuthManager>();
builder.Services.AddScoped<ITeamService, TeamManager>();
builder.Services.AddScoped<IVehicleAccessService, VehicleAccessManager>();
builder.Services.AddScoped<IAssignmentService, AssignmentManager>();
builder.Services.AddScoped<IDocumentService, DocumentManager>();
builder.Services.AddScoped<IVehicleService, VehicleManager>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceManager>();
builder.Services.AddScoped<IFuelService, FuelManager>();
builder.Services.AddScoped<IExpenseService, ExpenseManager>();
builder.Services.AddScoped<IReminderService, ReminderManager>();
builder.Services.AddScoped<IReportService, ReportManager>();
builder.Services.AddScoped<IEmailService, SmtpEmailManager>();
builder.Services.AddScoped<ReminderNotificationJob>();
builder.Services.AddScoped<DemoDataSeeder>();

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
    builder.Services.AddHangfireServer();
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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

builder.Services.AddControllers().AddJsonOptions(options =>
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

app.UseSwagger();
app.UseSwaggerUI();

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

app.UseRateLimiter();

app.UseAuthentication();
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
        recurringJobManager.AddOrUpdate<ReminderNotificationJob>(
            "reminder-notifications",
            job => job.RunAsync(),
            Cron.Daily(6));
    }
}

app.Run();

public partial class Program
{
}
