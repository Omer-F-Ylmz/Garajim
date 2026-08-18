using System.Text;
using Garajim.ML.Models;

namespace Garajim.ML.DataPrep
{
    public static class CarCsvLoader
    {
        public const float MinPrice = 100_000f;
        public const float MaxPrice = 50_000_000f;
        public const float MaxKilometre = 2_000_000f;
        public const float MinYear = 1990f;

        private static readonly string[] RequiredColumns =
        {
            "fiyat", "marka", "seri", "yil", "kilometre", "yakit_tipi", "vites_tipi", "kasa_tipi"
        };

        public static CarDataLoadResult Load(string csvPath)
        {
            using var reader = new StreamReader(csvPath, Encoding.UTF8);
            return Load(reader);
        }

        public static CarDataLoadResult Load(TextReader reader)
        {
            var result = new CarDataLoadResult();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> columns = null;

            foreach (var record in CsvRecordReader.Read(reader))
            {
                if (columns == null)
                {
                    columns = MapColumns(record);
                    continue;
                }

                result.TotalRows++;

                var marka = Field(record, columns, "marka");
                var seri = Field(record, columns, "seri");
                var yakitTipi = Field(record, columns, "yakit_tipi");
                var vitesTipi = Field(record, columns, "vites_tipi");
                var kasaTipi = Field(record, columns, "kasa_tipi");

                if (IsBlank(marka) || IsBlank(seri) || IsBlank(yakitTipi) || IsBlank(vitesTipi) || IsBlank(kasaTipi))
                {
                    result.InvalidRows++;
                    continue;
                }

                if (!TryParseNumber(Field(record, columns, "fiyat"), out var fiyat)
                    || !TryParseNumber(Field(record, columns, "kilometre"), out var kilometre)
                    || !TryParseNumber(Field(record, columns, "yil"), out var yil))
                {
                    result.InvalidRows++;
                    continue;
                }

                if (fiyat < MinPrice || fiyat > MaxPrice || kilometre > MaxKilometre || yil < MinYear)
                {
                    result.OutOfRangeRows++;
                    continue;
                }

                var sample = new CarPriceInput
                {
                    Marka = marka,
                    Seri = seri,
                    Yil = yil,
                    Kilometre = kilometre,
                    YakitTipi = yakitTipi,
                    VitesTipi = vitesTipi,
                    KasaTipi = kasaTipi,
                    Fiyat = fiyat,
                    LogFiyat = PriceScale.ToLog(fiyat)
                };

                if (!seen.Add(BuildKey(sample)))
                {
                    result.DuplicateRows++;
                    continue;
                }

                result.Samples.Add(sample);
            }

            if (columns == null)
            {
                throw new InvalidDataException("CSV başlık satırı okunamadı.");
            }

            return result;
        }

        private static Dictionary<string, int> MapColumns(string[] header)
        {
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < header.Length; i++)
            {
                var name = header[i].Trim().TrimStart('\uFEFF');
                if (name.Length > 0 && !columns.ContainsKey(name))
                {
                    columns[name] = i;
                }
            }

            var missing = RequiredColumns.Where(column => !columns.ContainsKey(column)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidDataException($"CSV dosyasında beklenen kolonlar yok: {string.Join(", ", missing)}");
            }

            return columns;
        }

        private static string Field(string[] record, Dictionary<string, int> columns, string name)
        {
            var index = columns[name];
            if (index >= record.Length)
            {
                return string.Empty;
            }

            return Normalize(record[index]);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(" ", value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static bool IsBlank(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "-";
        }

        private static bool TryParseNumber(string raw, out float value)
        {
            value = 0f;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var digits = new StringBuilder();

            foreach (var current in raw)
            {
                if (char.IsDigit(current))
                {
                    digits.Append(current);
                    continue;
                }

                if (current == '.' || current == ' ')
                {
                    continue;
                }

                if (digits.Length > 0)
                {
                    break;
                }

                if (current == ',' || char.IsLetter(current))
                {
                    return false;
                }
            }

            if (digits.Length == 0 || digits.Length > 12)
            {
                return false;
            }

            value = float.Parse(digits.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        private static string BuildKey(CarPriceInput sample)
        {
            return string.Join("|", sample.Marka, sample.Seri, sample.Yil, sample.Kilometre, sample.YakitTipi, sample.VitesTipi, sample.KasaTipi, sample.Fiyat);
        }
    }
}
