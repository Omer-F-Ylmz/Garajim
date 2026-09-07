using System.Text.Json;

namespace Garajim.KatalogUretici
{
    public static class VpicYanitAyristirici
    {
        public static List<string> Markalar(string json) => Alan(json, "MakeName");

        public static List<string> Modeller(string json) => Alan(json, "Model_Name");

        private static List<string> Alan(string json, string alanAdi)
        {
            JsonDocument belge;

            try
            {
                belge = JsonDocument.Parse(json);
            }
            catch (JsonException hata)
            {
                throw new InvalidOperationException("vPIC yaniti ayristirilamadi: " + hata.Message, hata);
            }

            using (belge)
            {
                if (!belge.RootElement.TryGetProperty("Results", out var sonuclar)
                    || sonuclar.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("vPIC yanitinda Results dizisi yok.");
                }

                var liste = new List<string>();

                foreach (var satir in sonuclar.EnumerateArray())
                {
                    if (satir.TryGetProperty(alanAdi, out var deger) && deger.ValueKind == JsonValueKind.String)
                    {
                        liste.Add(deger.GetString());
                    }
                }

                return liste;
            }
        }
    }
}
