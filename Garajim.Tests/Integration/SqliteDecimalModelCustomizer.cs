using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Garajim.Tests.Integration
{
    public class SqliteDecimalModelCustomizer : RelationalModelCustomizer
    {
        private static readonly ValueConverter<decimal, double> DecimalToDouble =
            new ValueConverter<decimal, double>(value => (double)value, value => (decimal)value);

        public SqliteDecimalModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
        {
        }

        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

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
