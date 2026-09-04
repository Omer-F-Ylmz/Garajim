using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class AiTokenSayaci : IEntity
    {
        public int Id { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }
        public long TokenGiris { get; set; }
        public long TokenCikis { get; set; }
        public int KotaHatasi { get; set; }
        public bool BildirimGonderildi { get; set; }
    }
}
