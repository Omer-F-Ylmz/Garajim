namespace Garajim.API.Startup
{
    public static class YuklemeOkuyucu
    {
        public static async Task<byte[]> OkuAsync(IFormFile dosya, CancellationToken ct = default)
        {
            if (dosya == null || dosya.Length <= 0)
            {
                return Array.Empty<byte>();
            }

            var icerik = new byte[dosya.Length];

            await using var kaynak = dosya.OpenReadStream();
            await kaynak.ReadExactlyAsync(icerik, ct);

            return icerik;
        }
    }
}
