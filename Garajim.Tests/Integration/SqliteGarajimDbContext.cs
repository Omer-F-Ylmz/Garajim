using Garajim.Core.Multitenancy;
using Garajim.Dal.Concrete.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Garajim.Tests.Integration
{
    public class SqliteGarajimDbContext : GarajimDbContext
    {
        private static readonly ValueConverter<decimal, double> DecimalToDouble =
            new ValueConverter<decimal, double>(value => (double)value, value => (decimal)value);

        public SqliteGarajimDbContext(DbContextOptions<GarajimDbContext> options, ITenantProvider tenantProvider) : base(options, tenantProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var decimalProperties = modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(entityType => entityType.GetProperties())
                .Where(property => property.ClrType == typeof(decimal));

            foreach (var property in decimalProperties)
            {
                property.SetValueConverter(DecimalToDouble);
                property.SetPrecision(null);
                property.SetScale(null);
            }
        }
    }
}
