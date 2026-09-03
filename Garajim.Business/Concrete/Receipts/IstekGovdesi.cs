using System.Buffers;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Garajim.Business.Concrete.Receipts
{
    public static class IstekGovdesi
    {
        public static ArrayBufferWriter<byte> Tampon(int goruntuUzunlugu)
        {
            var base64 = Base64.GetMaxEncodedToUtf8Length(Math.Max(goruntuUzunlugu, 0));
            var prompt = Encoding.UTF8.GetByteCount(ReceiptResponseParser.Prompt);

            return new ArrayBufferWriter<byte>(base64 + prompt + 2048);
        }

        public static byte[] VeriUrl(string mimeType, byte[] ham)
        {
            var onek = JsonEncodedText.Encode("data:" + mimeType + ";base64,").EncodedUtf8Bytes;
            var base64Uzunluk = Base64.GetMaxEncodedToUtf8Length(ham.Length);

            var hedef = new byte[onek.Length + base64Uzunluk + 2];
            hedef[0] = (byte)'"';
            onek.CopyTo(hedef.AsSpan(1));
            Base64.EncodeToUtf8(ham, hedef.AsSpan(1 + onek.Length, base64Uzunluk), out _, out _);
            hedef[hedef.Length - 1] = (byte)'"';

            return hedef;
        }

        public static HttpContent Icerik(ArrayBufferWriter<byte> tampon)
        {
            var icerik = new ReadOnlyMemoryContent(tampon.WrittenMemory);
            icerik.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            return icerik;
        }
    }
}
