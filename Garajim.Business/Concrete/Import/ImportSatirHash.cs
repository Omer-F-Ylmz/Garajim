using System.Security.Cryptography;
using System.Text;

namespace Garajim.Business.Concrete.Import
{
    public static class ImportSatirHash
    {
        public static string Hesapla(int vehicleId, IEnumerable<string> alanlar)
        {
            var kaynak = vehicleId + "|" + string.Join("|", alanlar.Select(a => (a ?? string.Empty).Trim()));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kaynak))).ToLowerInvariant();
        }
    }
}
