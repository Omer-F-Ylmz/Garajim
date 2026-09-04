# Değişiklikler

Garajım'ın sürüm geçmişi. En yeni sürüm en üstte.

## Onboarding — 5 Eylül 2026

- Yeni hesaplarda üç adımlı kurulum çubuğu: araç ekle, ilk kaydı gir, evrakı tanımla.
- İlk girişte altı adımlık ürün turu; Ayarlar'dan tekrar izlenebiliyor.
- Her sekmede boş durum: ne işe yaradığını anlatan bir cümle ve doğrudan forma götüren düğme.
- Yardım sayfası: 30 sıkça sorulan soru, anında arama ve paylaşılabilir başlık bağlantıları.
- Örnek araç: tek tıkla altı aylık gerçekçi kayıtlarla dolu deneme aracı; plan limitine sayılmaz, istenince temizce silinir.
- Tanıtım sayfası genişledi: nasıl çalışır, uygulamadan görüntüler, paketler, sık sorulanlar ve veri güvenliği.
- Geri bildirim düğmesi: sayfa ve sürüm bilgisiyle birlikte destek ekibine ulaşıyor.
- Profil bölümü: ad değişikliği, iki adımlı e-posta değişimi ve bildirim tercihleri.
- Telefona kurulum ipucu: Android'de yükle düğmesi, iPhone'da Ana Ekrana Ekle şeridi.
- Yönetim paneli: şirket, kullanıcı, araç, fiş doğruluğu, AI maliyeti ve bellek sayaçları.

## Düzeltme turu — 4 Eylül 2026

- Fiş okuma hızlı modele geçti; düşünme bütçesi ve zamanaşımı yeniden ayarlandı.
- Kota dolduğunda boş taslak üretilmiyor, açık bir hata dönüyor.
- Aynı formun çift gönderilmesi tek kayıt üretiyor.
- Form hataları Türkçe ve anlaşılır: tarih, sayı ve seçim alanları için ayrı metinler.
- Çevrimdışı kuyruk görünür oldu; Kaza anı düğmesi telefonda ekranın üstünde kalıyor.
- Bellek tabanı düşürüldü ve ölçülebilir hale geldi.

## Marka ve model kataloğu — 4 Eylül 2026

- Araç eklerken marka ve model listeden seçiliyor; 56 marka, 391 seri.
- Katalog dışı model "Listede yok" ile serbest yazılabiliyor, o araçta değer tahmini kapanıyor.
- Mevcut kayıtlar katalog yazımına bir kez çekildi.
- Paylaşılan metinler uygunsuz ifade filtresinden geçiyor.

## Mantık turu düzeltmeleri — 4 Eylül 2026

- Fişten okunan plaka normalize edilerek araca eşleşiyor.
- Bakım, evrak ve yolculuk kayıtları düzenlenebiliyor.
- Yabancı plaka ve acil durum kartı arayüzden yönetilebiliyor.
- Yolculuk kilometre aralıkları çakışamıyor; filo maliyeti araç maliyetiyle aynı mesafeyi ölçüyor.
- Karne belgeleri hasar fotoğraflarını paylaşmıyor.
- Yönetici ekranları sürücü rolünde gizleniyor.

## Kotalar, silme ve demo — 3 Eylül 2026

- Plana bağlı aylık fiş limiti ve aylık AI token tavanı.
- Hesap ve şirket silme yedi gün bekliyor, bu sürede iptal edilebiliyor.
- Araç arşivleme: plan limitine sayılmıyor, paylaşılmış karne bağlantısı çalışmaya devam ediyor.
- Demo verisi her gece sıfırlanıyor.
- Gün sınırları Türkiye saatine göre hesaplanıyor.
- Kilometre düşürme açık onay ve neden istiyor.
- Tüketim yalnız tam dolumlar arasında ölçülüyor, şüpheli kayıtlar işaretleniyor.
- Plaka doğrulaması Türkiye kuralına göre; yabancı plaka ayrı bayrakla.

## Şifre akışları ve güvenlik — 3 Eylül 2026

- Şifremi unuttum: e-posta koduyla sıfırlama; şifre değişince eski oturumlar düşüyor.
- Güvenlik yanıt başlıkları, CSV formül enjeksiyonu koruması ve üretimde kapalı Swagger.
- Hız sınırlayıcı kullanıcı başına sayıyor.

## Fiş okuma ve PWA — 1 Eylül 2026

- Fiş taslağı akışı: yükle, yapay zekâ ile oku, kontrol edip onayla ya da reddet.
- Uygulama telefona kurulabilir hale geldi; yeni sürüm çıkınca şerit uyarıyor.

## Çok kiracılılık ve ekip — 28 Ağustos 2026

- Şirket bazlı veri ayrımı, Owner / Manager / Driver rolleri.
- Araç-sürücü zimmeti, ekip belgeleri ve belge yükleme.

## İlk sürüm — 18 Ağustos 2026

- Araç, bakım, yakıt, masraf ve hatırlatma kayıtları.
- İkinci el fiyat tahmini.
- JWT ile kimlik doğrulama ve tek sayfalık web arayüzü.
