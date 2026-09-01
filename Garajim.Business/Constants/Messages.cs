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
    }
}
