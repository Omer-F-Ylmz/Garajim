using System.Collections.Concurrent;
using Garajim.Business.Abstract;

namespace Garajim.Business.Concrete
{
    public class BellekKodGonderimSayaci : IKodGonderimSayaci
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _gonderimler =
            new ConcurrentDictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);

        public bool IzinVer(string email)
        {
            return Guncel(email).Count < DogrulamaKodu.SaatlikGonderimSiniri;
        }

        public void Say(string email)
        {
            var liste = Guncel(email);
            lock (liste)
            {
                liste.Add(DateTime.UtcNow);
            }
        }

        private List<DateTime> Guncel(string email)
        {
            var liste = _gonderimler.GetOrAdd(email ?? string.Empty, _ => new List<DateTime>());
            var sinir = DateTime.UtcNow.AddHours(-1);

            lock (liste)
            {
                liste.RemoveAll(t => t < sinir);
                return liste;
            }
        }
    }
}
