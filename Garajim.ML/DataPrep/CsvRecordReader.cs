using System.Text;

namespace Garajim.ML.DataPrep
{
    public static class CsvRecordReader
    {
        public static IEnumerable<string[]> Read(TextReader reader)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var started = false;

            int read;
            while ((read = reader.Read()) != -1)
            {
                var current = (char)read;

                if (inQuotes)
                {
                    if (current != '"')
                    {
                        field.Append(current);
                        continue;
                    }

                    if (reader.Peek() == '"')
                    {
                        reader.Read();
                        field.Append('"');
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                if (current == '"')
                {
                    inQuotes = true;
                    started = true;
                    continue;
                }

                if (current == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    started = true;
                    continue;
                }

                if (current == '\r')
                {
                    continue;
                }

                if (current == '\n')
                {
                    if (started || field.Length > 0)
                    {
                        fields.Add(field.ToString());
                        yield return fields.ToArray();
                    }

                    fields.Clear();
                    field.Clear();
                    started = false;
                    continue;
                }

                field.Append(current);
                started = true;
            }

            if (started || field.Length > 0)
            {
                fields.Add(field.ToString());
                yield return fields.ToArray();
            }
        }
    }
}
