using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class YolculukCreateDto
    {
        public int VehicleId { get; set; }
        public DateTime Tarih { get; set; }
        public int BaslangicKm { get; set; }
        public int BitisKm { get; set; }
        public YolculukAmaci Amac { get; set; }
        public string Nereden { get; set; }
        public string Nereye { get; set; }
        public string Not { get; set; }
    }

    public class YolculukUpdateDto
    {
        public DateTime Tarih { get; set; }
        public int BaslangicKm { get; set; }
        public int BitisKm { get; set; }
        public YolculukAmaci Amac { get; set; }
        public string Nereden { get; set; }
        public string Nereye { get; set; }
        public string Not { get; set; }
    }

    public class YolculukDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Plaka { get; set; }
        public int UserId { get; set; }
        public string SurucuAdi { get; set; }
        public DateTime Tarih { get; set; }
        public int BaslangicKm { get; set; }
        public int BitisKm { get; set; }
        public int MesafeKm { get; set; }
        public string Amac { get; set; }
        public string Nereden { get; set; }
        public string Nereye { get; set; }
        public string Not { get; set; }
    }

    public class AmacToplamDto
    {
        public YolculukAmaci Amac { get; set; }
        public int ToplamKm { get; set; }
        public int Adet { get; set; }
    }

    public class YolculukOzetDto
    {
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public int ToplamKm { get; set; }
        public int IsKm { get; set; }
        public int OzelKm { get; set; }
        public int YolculukSayisi { get; set; }
        public decimal IsOrani { get; set; }
    }
}
