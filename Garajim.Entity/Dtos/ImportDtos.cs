namespace Garajim.Entity.Dtos
{
    public class ImportOnizlemeDto
    {
        public string Sablon { get; set; }
        public string Ayrac { get; set; }
        public string KayitTuru { get; set; }
        public List<string> Basliklar { get; set; } = new List<string>();
        public Dictionary<string, int> OnerilenEslesme { get; set; } = new Dictionary<string, int>();
        public List<string> GerekliAlanlar { get; set; } = new List<string>();
        public List<string[]> OrnekSatirlar { get; set; } = new List<string[]>();
        public int ToplamSatir { get; set; }
        public List<ImportHataDto> HataliSatirlar { get; set; } = new List<ImportHataDto>();
    }

    public class ImportHataDto
    {
        public int SatirNo { get; set; }
        public string Sebep { get; set; }
        public string Icerik { get; set; }
    }

    public class ImportUygulaDto
    {
        public int VehicleId { get; set; }
        public string KayitTuru { get; set; }
        public Dictionary<string, int> Eslesme { get; set; } = new Dictionary<string, int>();
        public bool DryRun { get; set; }
        public string DosyaAdi { get; set; }
        public byte[] Icerik { get; set; }
    }

    public class ImportSonucDto
    {
        public int Eklenen { get; set; }
        public int Atlanan { get; set; }
        public bool DryRun { get; set; }
        public List<ImportHataDto> Hatali { get; set; } = new List<ImportHataDto>();
    }
}
