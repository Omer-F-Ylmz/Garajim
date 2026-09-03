namespace Garajim.Business.Constants
{
    public static class Messages
    {
        public const string RegisterSuccess = "Kayıt başarılı.";
        public const string EmailAlreadyExists = "Bu e-posta adresi zaten kayıtlı.";
        public const string InvalidCredentials = "E-posta veya şifre hatalı.";
        public const string LoginSuccess = "Giriş başarılı.";
        public const string DogrulamaKoduGonderildi = "Doğrulama kodu e-posta adresinize gönderildi.";
        public const string DogrulamaKoduGecersiz = "Doğrulama kodu hatalı ya da süresi dolmuş. Yeni kod isteyin.";
        public const string EmailDogrulandiMesaji = "E-posta adresiniz doğrulandı.";
        public const string EmailDogrulanmadi = "E-posta adresiniz henüz doğrulanmadı. Size gönderilen kodu girin.";
        public const string KodGonderimYaniti = "Kod gönderildiyse e-posta adresinize ulaşacak. Spam klasörünü de kontrol edin.";
        public const string SifirlamaKoduYaniti = "Hesap varsa şifre sıfırlama kodu e-posta adresinize ulaşacak. Spam klasörünü de kontrol edin.";
        public const string SifirlamaKoduGecersiz = "Sıfırlama kodu hatalı ya da süresi dolmuş. Yeni kod isteyin.";
        public const string SifreDegistirildi = "Şifreniz değiştirildi. Yeni şifrenizle giriş yapın.";
        public const string MevcutSifreHatali = "Mevcut şifreniz hatalı.";
        public const string VehicleAdded = "Araç eklendi.";
        public const string VehicleUpdated = "Araç güncellendi.";
        public const string VehicleDeleted = "Araç silindi.";
        public const string VehicleNotFound = "Araç bulunamadı.";
        public const string KmDusurmeOnayiGerekli = "Kilometreyi düşürmek için onay kutusunu işaretleyin ve kısa bir neden yazın (en az 3 karakter).";
        public const string ParcaHatirlatmasiZatenVar = "Bu parça için açık bir hatırlatma zaten var.";
        public const string AiButcesiAsildi = "AI özellikleri bu ay geçici olarak kapalı. Fiş okuma ve AI Usta yeni ayda yeniden açılacak.";
        public const string HesapSilmeKoduYaniti = "Silme kodu e-posta adresinize gönderildi. Kod 10 dakika geçerlidir.";
        public const string HesapSilmeKoduGecersiz = "Silme kodu hatalı ya da süresi dolmuş. Yeni kod isteyin.";
        public const string HesapSilmePlanlandi = "Hesabınız 7 gün sonra kalıcı olarak silinecek. Bu süre içinde iptal edebilirsiniz.";
        public const string SahipHesabiniBoyleSilemez = "Şirket sahibi hesabını bu uçtan silemez; Ayarlar'daki şirket silme akışını kullanın.";
        public const string UyeHesabiSilindi = "Hesabınız silindi. Kişisel bilgileriniz kaldırıldı, şirketin araç ve kayıtları yerinde kaldı.";
        public const string HesapSilmeIptalEdildi = "Hesap silme isteği iptal edildi.";
        public const string AracArsivlendi = "Araç arşive alındı.";
        public const string AracArsivdenAlindi = "Araç arşivden çıkarıldı.";
        public const string AracArsivli = "Bu araç arşivde; yeni kayıt eklemek için önce arşivden çıkarın.";
        public const string MarkaKatalogdaYok = "Bu marka katalogda yok; listeden seçin.";
        public const string SeriKatalogdaYok = "Bu seri seçilen markada yok; listeden seçin ya da Listede yok kutusunu işaretleyin.";
        public const string UygunsuzIfade = "Metinde uygunsuz ifade var; düzeltip tekrar deneyin.";
        public const string ModelMetniGecersiz = "Model adı 2-40 karakter olmalı, harf içermeli ve yalnız harf, rakam, boşluk, nokta ve tire taşımalı.";
        public const string PlakaGecersiz = "Plaka Türkiye plaka kuralına uymuyor. Örnek: 34 ABC 123. Yurt dışı plakası ise yabancı plaka kutusunu işaretleyin.";
        public const string YabanciPlakaGecersiz = "Yabancı plaka 5-12 harf ve rakamdan oluşmalı, Türkçe karakter içermemeli.";
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

        public const string HasarDosyasiAcildi = "Hasar dosyası açıldı.";
        public const string HasarDosyasiGuncellendi = "Hasar dosyası güncellendi.";
        public const string HasarDosyasiSilindi = "Hasar dosyası ve fotoğrafları silindi.";
        public const string HasarDosyasiBulunamadi = "Hasar dosyası bulunamadı.";
        public const string HasarFotoEklendi = "Fotoğraf eklendi.";
        public const string HasarFotoSilindi = "Fotoğraf silindi.";
        public const string HasarFotoBulunamadi = "Fotoğraf bulunamadı.";
        public const string HasarFotoSiniri = "Bir hasar dosyasına en fazla 20 fotoğraf eklenebilir.";
        public const string HasarTarihiGecersiz = "Olay tarihi geçmiş bir tarih olmalı.";

        public const string DegerKaydedildi = "Araç değeri kaydedildi.";
        public const string DegerGecersiz = "Araç değeri sıfırdan büyük olmalı.";
        public const string DegerTarihiGecersiz = "Değer tarihi gelecekte olamaz.";
        public const string DegerModelKatalogDisi = "Bu aracın modeli katalogda yok; aracı düzenleyip listeden seçin.";
        public const string DegerModelKapsamDisi = "Bu araç modeli tahmin modelinin kapsamı dışında; değeri elle beyan edebilirsiniz.";
        public const string DegerKasaTipiGerekli = "Tahmin için önce aracın kasa tipini seçin.";
        public const string DegerVitesGerekli = "Tahmin için önce aracın vites tipini seçin.";
        public const string DegerTahminSiniri = "Bir araç için günde en fazla 3 tahmin alınabilir.";
        public const string DegerTahminUyarisi = "Ağustos 2025 piyasa verisiyle eğitilmiş model, enflasyon düzeltmesi yok, bilgilendirme amaçlıdır.";
        public const string DegerTahminAlindi = "Tahmini değer kaydedildi.";

        public const string UstaOnayGerekli = "AI Usta'yı kullanmak için kullanım şartlarını onaylamanız gerekiyor.";
        public const string UstaOnayAlindi = "Onayınız kaydedildi.";
        public const string UstaOnaySurumuEski = "Onay metni güncellendi; lütfen yeni metni onaylayın.";
        public const string UstaGunlukLimit = "Günlük soru hakkınız doldu; yarın tekrar deneyin.";
        public const string UstaSohbetLimiti = "Bu sohbet için mesaj sınırına ulaşıldı; yeni sohbet açın.";
        public const string UstaSohbetBulunamadi = "Sohbet bulunamadı.";
        public const string UstaMesajBulunamadi = "Mesaj bulunamadı.";
        public const string UstaSohbetOlusturuldu = "Sohbet açıldı.";
        public const string UstaSohbetSilindi = "Sohbet silindi.";
        public const string UstaYanitHazir = "Usta yanıtladı.";
        public const string UstaYanitAlinamadi = "Usta şu anda yanıt üretemedi; birazdan tekrar deneyin.";
        public const string UstaGeriBildirimAlindi = "Geri bildiriminiz alındı.";
        public const string UstaCozumBakimiUygunDegil = "Seçilen bakım bu araca ait değil ya da 90 günden eski.";

        public const string PlanTalebiAlindi = "Plan yükseltme talebiniz destek ekibine iletildi.";
        public const string PlanZatenAktif = "Bu plan zaten aktif.";
        public const string DestekEpostasiTanimsiz = "Destek e-posta adresi yapılandırılmamış; talep gönderilemedi.";
        public const string TicariKisLastigiUyarisi = "Kış lastiği dönemindesiniz ({0}). Ticari araçlarda M+S işaretli kış ya da dört mevsim lastiği zorunludur.";

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
