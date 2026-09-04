namespace Garajim.Entity.Dtos
{
    public class YonetimGunDto
    {
        public string Gun { get; set; }
        public int Sirket { get; set; }
        public int Kullanici { get; set; }
    }

    public class YonetimOzetDto
    {
        public int SirketSayisi { get; set; }
        public int KullaniciSayisi { get; set; }
        public int AracSayisi { get; set; }
        public List<YonetimGunDto> GunlukKayitlar { get; set; } = new List<YonetimGunDto>();
        public int FisSayisi { get; set; }
        public double FisDogrulukOrani { get; set; }
        public double OtoOnayOrani { get; set; }
        public long AiTokenKullanilan { get; set; }
        public long AiTokenTavani { get; set; }
        public double AiTahminiMaliyetUsd { get; set; }
        public int KotaHatasi { get; set; }
        public double KarnePaylasimOrani { get; set; }
        public double DavetKayitOrani { get; set; }
        public bool UstaAcik { get; set; }
        public object Bellek { get; set; }
        public List<GeriBildirimDto> SonGeriBildirimler { get; set; } = new List<GeriBildirimDto>();
    }
}
