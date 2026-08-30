# Yol Haritası

## Konumlandırma

**"Fişi fotoğrafla, gerisini biz halledelim; sattığında karnesi yanında gider."**

Ürün "Aracının Belgeli Hafızası" olarak konumlanır. Çekirdek çark üç parçalıdır ve her parça bir sonrakini besler:

1. **Fiş okuma veri toplar** — kullanıcı fişi fotoğraflar, kayıt kendiliğinden oluşur; veri girme sürtünmesi ortadan kalkar.
2. **Karne değere çevirir** — biriken kayıtlar aracın belgeli geçmişine dönüşür; satışta alıcıya gösterilen karne, veriyi kullanıcının malı yapar.
3. **Evrak takvimi kullanıcıyı geri çağırır** — MTV, muayene, sigorta tarihleri kullanıcıyı düzenli aralıklarla uygulamaya döndürür; dönen kullanıcı yeni fiş yükler.

Hedef kitle: önce bireysel araç sahibi. Filo tarafı "pull" modunda kalır — landing'de "Filonuz mu var? İletişime geçin" satırı durur, aktif B2B satış yapılmaz; talep gelirse değerlendirilir.

Durum işaretleri: `[x]` bitti · `[~]` kısmen · `[ ]` başlanmadı

## Faz 1 — TAMAMLANDI — canlıda

### Faz 1a — Mimari temel

- [x] `Company` entity'si: Id, Name, PlanType (tek değer: Standart), CreatedAt
- [x] Sekiz entity'de `CompanyId`: Users, Vehicles, MaintenanceRecords, FuelRecords, ExpenseRecords, Reminders, Documents, VehicleAssignments
- [x] EF Core global query filter; `CompanyId` JWT claim'inden gelir, istek gövdesinden asla okunmaz
- [x] Tenant sağlayıcı (`ITenantProvider` / `TenantContext`) ve `TenantResolutionMiddleware`
- [x] JWT'ye `companyId` claim'i (rol claim'i Faz 1b'de)
- [x] `AddCompanyTenancy` migration'ı: kullanıcı başına kişisel şirket, kayıtların join ile geri doldurulması; boş ve dolu veritabanında doğrulandı
- [x] Kayıt sırasında kişisel şirket açılması; demo kullanıcı "Garajım Demo" şirketi
- [x] Kiracı izolasyonu testleri: yabancı kayıtta GET/PUT/DELETE 404, liste uçları yalnız kendi şirketi
- [x] Denormalizasyon değişmezi (`çocuk.CompanyId == aracın CompanyId`) testle sabitlendi
- [x] Hangfire job'ı şirketleri tek tek dolaşıyor; şirketler arası tek sorgu yok
- [x] Demo tohumlaması yeni şemayla idempotent (yayın sonrası artımlı hale getirildi: eksik öğe tamamlanır, mevcut veriye dokunulmaz)

### Faz 1b — Roller, zimmet, belge, arayüz

- [x] `CompanyRole`: Owner, Manager, Driver; JWT'ye rol claim'i
- [x] Politikalar: Owner her şey; Manager araç/kayıt/zimmet; Driver yalnız zimmetli araçları
- [x] Kullanıcı yönetimi uçları (yalnız Owner): kullanıcı ekle (geçici şifre yanıtta bir kez), rol değiştir, pasifleştir
- [x] Araç-sürücü zimmeti: aynı anda tek aktif zimmet (filtreli tekil indeks + iş kuralı), zimmet geçmişi, devir
- [x] Belge yükleme: wwwroot dışında saklama, uzantı beyaz listesi, magic-byte kontrolü, boyut ve şirket kotası
- [x] SPA: şirket adı alanı, Ekip ekranı, araç kartında zimmet, bakım kaydına belge, Driver görünümü
- [x] Plaka tekilliğinin şirket başına taşınması; migration çakışan plakaları bulup açık hatayla durur
- [x] Demo tohumlaması sahip + sürücü + aktif zimmet üretir, idempotent kalır

Faz 1'de bilinçli sınırlar: geçmişe dönük zimmet devri yok (audit log ile gelecek); Driver'a "kendi eklediği araç" istisnası yok; belge önizlemesi yok.

## 6 Aylık Plan

### Ay 1 — Fiş okuma + PWA + e-posta (bu sprint)

- [ ] E-posta bildirim altyapısı: MailKit SMTP, yapılandırma yoksa loglayıp atlama, hatırlatma job'ına bağlı
- [ ] `IReceiptExtractor` + Gemini/OpenAI sağlayıcıları (görüntüden yapılandırılmış JSON)
- [ ] Taslak kayıt akışı: fiş yükle → AI çıkarımı → kullanıcı onayı → Yakıt/Bakım/Masraf kaydı + belge bağı
- [ ] Onayda düzeltilen alanların ölçümü (pratik doğruluk metriği)
- [ ] PWA: manifest + service worker + ana ekrana ekle; mobil öncelikli fiş yükleme ekranı
- [ ] Aylık AI çağrı limiti (maliyet koruması)

### Ay 2 — Araç karnesi + parça hafızası

- [ ] Araç karnesi: kayıtlardan üretilen paylaşılabilir belgeli geçmiş
- [ ] Parça hafızası: hangi parça ne zaman, kaç km'de değişti

### Ay 3 — Türkiye evrak takvimi

- [ ] MTV Ocak/Temmuz otomatik hatırlatma
- [ ] Muayene araç türüne göre: hususi 2 yıl / ticari 1 yıl
- [ ] Egzoz emisyon, zorunlu trafik, kasko takvimleri
- [ ] Cüzdan kartı / ICS takvim aboneliği
- [ ] Acil durum kartı

### Ay 4 — Geçiş sihirbazı + gelir

- [ ] Rakipten geçiş sihirbazı: Drivvo / Fuelio CSV içe aktarma
- [ ] Maliyet analizi
- [ ] Pro paketleme

### Ay 5-6 — Genişleme

- [ ] EV / şarj kayıtları
- [ ] Lastik ve sarf takibi
- [ ] Davet programı

## Kill Criteria

- **Fiş çıkarım doğruluğu:** İlk 30 Türk fişinde tarih+tutar+km çıkarım doğruluğu %85'in altındaysa prompt/sağlayıcı revizyonu yapılır; ikinci turda da altında kalırsa fotoğraf-önce stratejisi sorgulanır.
- **Tutunma:** İlk 100 kullanıcının %25'inden azı 30. günde hâlâ fiş yüklüyorsa tez yeniden değerlendirilir.
- **Paylaşım:** Karne paylaşım oranı %15'in altındaysa davet programı öne çekilir.

## Tetikleyiciye Bağlı

Takvime değil koşula bağlı işler; koşul oluşmadan başlanmaz:

- **AI usta ve AI ilan asistanı** — karne verisi olgunlaşınca
- **Usta köprüsü** — aktif kullanıcı kütlesi oluşunca
- **WhatsApp botu** — API maliyeti bütçeye girince; kullanıcı ayarından açılıp kapanabilir olacak
- **E-arşiv email-in** (faturayı e-postayla iletme) — fotoğraf kanalı kanıtlanınca
- **Kaza/hasar dosyası** — rent a car ile ortak geliştirilecek
- **Sesli giriş**

## Sınırlı / Durduruldu

- **Sigorta yönlendirme** — yalnız lisanslı acente ortaklığıyla; ortaklık yoksa yapılmaz
- **Değer takibi** — yalnız beyan bazlı basit amortisman; piyasa fiyatı iddiası yok
- **Recall takibi** — Türkiye'de merkezi kaynak yok; durduruldu
- **Servis pazaryeri** — süresiz ertelendi

## Gelecek Ürün: Rent a Car SaaS

Tetikleyiciler (herhangi biri): 2027 yetki belgesi düzenlemesi kesinleşirse VEYA filo kanalından 5+ kiralamacı talebi gelirse.

Garajım'dan taşınacak ortak çekirdek: belge deposu, fotoğraflı tutanak, takvim, çok kiracılılık. Bu modüller ilerideki ayrıştırma kolay olsun diye genel/taşınabilir yazılır.

## Birikim (planlanmamış)

Eski fazlardan devralınan, 6 aylık plana girmeyen işler:

- [ ] Periyodik bakım şablonları: kilometre ve takvim bazlı
- [ ] Yakıt analizi: L/100km, filo ortalamasından sapma uyarısı
- [ ] CSV / Excel dışa aktarım
- [ ] Yönetici dashboard'u
- [ ] Sürücü belge takibi (ehliyet, SRC)
- [ ] Excel'den toplu içe aktarma
- [ ] Audit log
- [ ] Abonelik ve ödeme (iyzico veya PayTR), araç sayısına göre paket sınırları — Ay 4 Pro paketlemenin altyapısı
- [ ] KVKK temeli: aydınlatma metni, veri silme akışı
- [ ] Araç değiştirme analizi: ML fiyat tahmini + bakım maliyeti eğrisi
- [ ] Muhasebe entegrasyonları ve dışa açık API
- [ ] Çoklu şube

## Sıraya Alınan Teknik Borçlar

Ayrıntısı [#2 numaralı issue](https://github.com/Omer-F-Ylmz/Garajim/issues/2)'da:

- [ ] Kayıt tarihlerinde üst sınır yok (gelecek tarih kabul ediliyor)
- [ ] Token localStorage'da, süre yönetimi istemcide yok
- [ ] Araç güncelleme ve silme arayüzde yok
- [ ] Hangfire paneli için üretimde bilinçli yetkilendirme filtresi kararı
