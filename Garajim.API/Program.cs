using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Garajim.API.Controllers;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Business.Jobs;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete;
using Garajim.Dal.Concrete.Context;
using Garajim.ML.Models;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<GarajimDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUserDal, EfUserDal>();
builder.Services.AddScoped<IVehicleDal, EfVehicleDal>();
builder.Services.AddScoped<IMaintenanceDal, EfMaintenanceDal>();
builder.Services.AddScoped<IFuelDal, EfFuelDal>();
builder.Services.AddScoped<IExpenseDal, EfExpenseDal>();
builder.Services.AddScoped<IReminderDal, EfReminderDal>();

builder.Services.AddScoped<IAuthService, AuthManager>();
builder.Services.AddScoped<IVehicleService, VehicleManager>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceManager>();
builder.Services.AddScoped<IFuelService, FuelManager>();
builder.Services.AddScoped<IExpenseService, ExpenseManager>();
builder.Services.AddScoped<IReminderService, ReminderManager>();
builder.Services.AddScoped<IReportService, ReportManager>();
builder.Services.AddScoped<IEmailService, SmtpEmailManager>();
builder.Services.AddScoped<ReminderNotificationJob>();

builder.Services.AddPredictionEnginePool<CarPriceInput, CarPricePrediction>()
    .FromFile(
        modelName: PricePredictionController.ModelName,
        filePath: "MLModels/price-model.zip",
        watchForChanges: true);

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

if (builder.Configuration.GetValue("ApplyMigrationsAtStartup", false))
{
    using (var migrationScope = app.Services.CreateScope())
    {
        migrationScope.ServiceProvider.GetRequiredService<GarajimDbContext>().Database.Migrate();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();
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
