namespace Garajim.Entity.Dtos
{
    public class ReceiptStatsDto
    {
        public bool UstaAcik { get; set; }
        public int ToplamCagri { get; set; }
        public int Onaylanan { get; set; }
        public int Reddedilen { get; set; }
        public int Bekleyen { get; set; }
        public int OtoOnaylanan { get; set; }
        public double OnayOrani { get; set; }
        public double RedOrani { get; set; }
        public double OrtalamaGuven { get; set; }
        public double OrtalamaSureMs { get; set; }
        public long AiTokenTavani { get; set; }
        public long AiTokenKullanilan { get; set; }
        public long AiTokenKalan { get; set; }
        public bool AiButcesiAsildi { get; set; }
        public Dictionary<string, double> AlanDoluluk { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> AlanDuzeltmeOrani { get; set; } = new Dictionary<string, double>();
    }
}
