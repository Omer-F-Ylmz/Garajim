using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Garajim.Business.Abstract;

namespace Garajim.Tests.Integration
{
    public class SahteEpostaGonderici : IEmailSender
    {
        public static readonly SahteEpostaGonderici Ortak = new SahteEpostaGonderici();

        private static readonly Regex KodDeseni = new Regex(@"\b\d{6}\b", RegexOptions.Compiled);

        private readonly ConcurrentQueue<(string Alici, string Konu, string Govde)> _gonderilenler = new();

        public IReadOnlyCollection<(string Alici, string Konu, string Govde)> Gonderilenler => _gonderilenler.ToArray();

        public Task SendAsync(string to, string subject, string body)
        {
            _gonderilenler.Enqueue((to, subject, body));
            return Task.CompletedTask;
        }

        public string SonKod(string alici)
        {
            var eslesme = _gonderilenler
                .Where(g => string.Equals(g.Alici, alici, StringComparison.OrdinalIgnoreCase))
                .Select(g => KodDeseni.Match(g.Govde))
                .LastOrDefault(m => m.Success);

            return eslesme?.Value;
        }

        public int SayiOf(string alici)
        {
            return _gonderilenler.Count(g => string.Equals(g.Alici, alici, StringComparison.OrdinalIgnoreCase));
        }

        public void Temizle()
        {
            while (_gonderilenler.TryDequeue(out _))
            {
            }
        }
    }
}
