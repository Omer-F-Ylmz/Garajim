namespace Garajim.Business.Constants
{
    public static class Messages
    {
        public const string RegisterSuccess = "Kayıt başarılı.";
        public const string EmailAlreadyExists = "Bu e-posta adresi zaten kayıtlı.";
        public const string InvalidCredentials = "E-posta veya şifre hatalı.";
        public const string LoginSuccess = "Giriş başarılı.";
        public const string VehicleAdded = "Araç eklendi.";
        public const string VehicleUpdated = "Araç güncellendi.";
        public const string VehicleDeleted = "Araç silindi.";
        public const string VehicleNotFound = "Araç bulunamadı.";
        public const string PlateAlreadyExists = "Bu plaka zaten kayıtlı.";
        public const string RecordAdded = "Kayıt eklendi.";
        public const string RecordDeleted = "Kayıt silindi.";
        public const string RecordNotFound = "Kayıt bulunamadı.";
        public const string ReminderAdded = "Hatırlatma eklendi.";
        public const string RecordUpdated = "Kayıt güncellendi.";
        public const string ReminderCompleted = "Hatırlatma tamamlandı olarak işaretlendi.";
        public const string ReminderDeleted = "Hatırlatma silindi.";
        public const string ReminderNotFound = "Hatırlatma bulunamadı.";
        public const string ReminderDateOrKmRequired = "Hatırlatma için tarih veya kilometre bilgisinden en az biri gerekli.";
        public const string NotEnoughFuelData = "Tüketim hesabı için kilometre bilgisi olan en az iki yakıt kaydı gerekli.";
        public const string InvalidValue = "Geçersiz değer.";
        public const string PriceEstimated = "Fiyat tahmini üretildi.";
        public const string PriceInputRequired = "Marka, seri, yakıt tipi, vites tipi ve kasa tipi zorunludur.";
        public const string PriceYearOutOfRange = "Yıl 1990 ile içinde bulunulan yıldan sonraki yıl arasında olmalıdır.";
        public const string PriceKilometreOutOfRange = "Kilometre 0 ile 2.000.000 arasında olmalıdır.";
        public const string PriceEstimateFailed = "Fiyat tahmini üretilemedi.";
        public const string TooManyRequests = "Çok fazla deneme yaptınız, lütfen bir dakika sonra tekrar deneyin.";
        public const string UserInactive = "Hesabınız pasif durumda, şirket yöneticinizle görüşün.";
        public const string UserNotFound = "Kullanıcı bulunamadı.";
        public const string UserAdded = "Kullanıcı eklendi, geçici şifreyi kendisine iletin.";
        public const string UserRoleChanged = "Kullanıcının rolü güncellendi.";
        public const string UserDeactivated = "Kullanıcı pasifleştirildi.";
        public const string CannotManageSelf = "Kendi hesabınız üzerinde bu işlemi yapamazsınız.";
        public const string LastOwnerRequired = "Şirkette en az bir Owner kalmalıdır.";
        public const string AssignmentAlreadyActive = "Bu araçta zaten aktif bir zimmet var; önce devredin veya sonlandırın.";
        public const string AssignmentNotFound = "Araçta aktif zimmet bulunamadı.";
        public const string AssignmentSameDriver = "Araç zaten bu sürücüye zimmetli.";
        public const string AssignmentCreated = "Zimmet oluşturuldu.";
        public const string AssignmentTransferred = "Zimmet devredildi.";
        public const string AssignmentEnded = "Zimmet sonlandırıldı.";
        public const string DocumentContextRequired = "Belge için araç veya bakım kaydı belirtilmelidir.";
        public const string DocumentExtensionNotAllowed = "Bu uzantı kabul edilmiyor; yalnızca jpg, jpeg, png ve pdf yüklenebilir.";
        public const string DocumentTooLarge = "Dosya boyutu izin verilen sınırın üstünde.";
        public const string DocumentContentMismatch = "Dosya içeriği uzantısıyla uyuşmuyor.";
        public const string DocumentQuotaExceeded = "Şirket belge kotası doldu; yer açmak için eski belgeleri silin.";
        public const string DocumentNotFound = "Belge bulunamadı.";
        public const string DocumentUploaded = "Belge yüklendi.";
        public const string DocumentDeleted = "Belge silindi.";

        public const string ReceiptNotFound = "Fiş taslağı bulunamadı.";
        public const string ReceiptAlreadyHandled = "Bu taslak zaten sonuçlandırılmış.";
        public const string ReceiptMonthlyLimitExceeded = "Bu ay için fiş okuma limitiniz doldu; yeni fişleri gelecek ay yükleyebilir veya kayıtları elle girebilirsiniz.";
        public const string ReceiptUploaded = "Fiş okundu, kontrol edip onaylayın.";
        public const string ReceiptAutoConfirmed = "Fiş okundu ve otomatik kaydedildi.";
        public const string ReceiptConfirmed = "Kayıt oluşturuldu.";
        public const string ReceiptRejected = "Fiş taslağı silindi.";

        public const string PartNeverReplaced = "Bu parça için kayıtlı değişim yok.";
        public const string PartHasNoInterval = "Bu parça için tanımlı değişim aralığı yok.";

        public const string KarneCreated = "Karne bağlantısı oluşturuldu.";
        public const string KarneClosed = "Karne paylaşımı kapatıldı.";
        public const string KarneNotFound = "Karne bulunamadı.";

        public const string AuthorizationDenied = "Bu işlem için yetkiniz yok.";
        public const string EvrakNotFound = "Evrak kaydı bulunamadı.";
        public const string EvrakSahibiTekOlmali = "Evrak ya bir araca ya da bir kullanıcıya bağlanmalıdır; ikisi birden olamaz.";
        public const string EvrakAdded = "Evrak kaydı eklendi.";
        public const string EvrakUpdated = "Evrak kaydı güncellendi.";
        public const string EvrakRenewed = "Evrak yenilendi.";
        public const string EvrakDeleted = "Evrak kaydı silindi.";

        public const string TakvimAbonelikCreated = "Takvim aboneliği oluşturuldu.";
        public const string TakvimAbonelikClosed = "Takvim aboneliği kapatıldı.";
        public const string TakvimAbonelikNotFound = "Takvim aboneliği bulunamadı.";

        public const string ImportDosyaCokBuyuk = "Dosya 5 MB sınırını aşıyor.";
        public const string ImportCokFazlaSatir = "Dosyada 5.000 satırdan fazla kayıt var; dosyayı bölerek yükleyin.";
        public const string ImportBozukDosya = "Dosya okunamadı; sütun ayracı bulunamadı.";
        public const string ImportEksikEslesme = "Zorunlu sütunların tümü eşlenmeli.";
        public const string ImportOnizlendi = "Önizleme hazır, kayıt yazılmadı.";
        public const string DavetKoduGecersiz = "Davet kodu geçersiz.";

        public const string LastikTakildi = "Lastik seti takıldı.";
        public const string LastikSokuldu = "Lastik seti söküldü.";
        public const string LastikSilindi = "Lastik seti silindi.";
        public const string LastikBulunamadi = "Lastik seti bulunamadı.";
        public const string LastikZatenSokulmus = "Bu set zaten sökülmüş.";
        public const string LastikKmHatali = "Sökülme kilometresi ve tarihi takılmadan önce olamaz.";
        public const string LastikSetiYok = "Araçta takılı lastik seti kayıtlı değil.";
        public const string KisLastigiUyarisi = "Kış lastigi dönemindesiniz ama araçta yaz lastiği takılı.";
        public const string LastikDisDerinligiUyarisi = "Diş derinliği yasal sınıra indi; seti değiştirin.";

        public const string ElektrikliAracaYakit = "Elektrikli araçta litre girilemez; şarj miktarını kWh olarak girin.";
        public const string SarjMiktariGerekli = "Şarj miktarı (kWh) zorunludur.";
        public const string YakitliAracaSarj = "Bu araç elektrikli değil; kWh girilemez.";

        public const string YolculukEklendi = "Yolculuk kaydı eklendi.";
        public const string YolculukGuncellendi = "Yolculuk kaydı güncellendi.";
        public const string YolculukSilindi = "Yolculuk kaydı silindi.";
        public const string YolculukBulunamadi = "Yolculuk kaydı bulunamadı.";
        public const string YolculukKmHatali = "Bitiş kilometresi başlangıçtan büyük olmalı.";

        public const string ExportTuruBulunamadi = "Bilinmeyen dışa aktarma türü.";
        public const string ExportHazir = "Dosya hazırlandı.";

        public const string AracLimitiAsildi = "Planınızın araç limitine ulaştınız; planı yükseltin veya bir aracı silin.";

        public const string ImportTamamlandi = "İçe aktarma tamamlandı.";
    }
}
