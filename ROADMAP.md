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

- [x] `Company` entity'si: Id, Name, PlanType, CreatedAt (Sprint 5'te `Bireysel`/`Filo` ayrımı ve `AracLimiti` eklendi)
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

## Sprint 1 — TAMAMLANDI

Fiş okuma, PWA ve e-posta altyapısı.

| Özellik | Commit |
|---|---|
| E-posta altyapısı: MailKit SMTP, yapılandırma yoksa loglayıp atlama, hatırlatma job'ına bağlı | `efe642d` |
| `IReceiptExtractor` + Gemini/OpenAI sağlayıcıları, 30 sn zaman aşımı, tek tekrar | `9e033d4` |
| Taslak kayıt akışı: yükle → çıkar → onayla → Yakıt/Bakım/Masraf + belge bağı, tek transaction | `af5cb1d` |
| SPA fiş ekranı + PWA (manifest, service worker, ana ekrana ekle) | `98fe6bc` |
| Ölçüm: çağrı logu, alan doluluk ve düzeltme oranı, `GET /api/Receipts/stats` | `b8e54e3` |

Aylık AI çağrı limiti (`Receipts__AylikLimit`, varsayılan 100) taslak akışıyla birlikte geldi.

## Sprint 2 — TAMAMLANDI

Toplu yükleme, koşullu otomatik onay, parça hafızası ve araç karnesi.

| Özellik | Commit |
|---|---|
| Koşullu oto onay: güven + alan + plaka üçlüsü tamsa kayıt anında açılır | `3ec8a56` |
| Toplu fiş yükleme: sıralı gönderim, ilerleme, dosya bazında özet | `d2daf10` |
| Kalibrasyon aracı: cevap anahtarına karşı alan bazında doğruluk | `b63159a` |
| Parça hafızası: 24 parça türü, aralık kataloğu, Iyi/Yaklasiyor/Gecti durumu | `8c8cb3e` |
| Fişten parça çıkarımı + bakım formu ve parça hafızası arayüzü | `5ed99e9` |
| Araç karnesi: token ile anonim paylaşım, kapsam denetimi, SystemScope | `7f6d9dd` |
| Karne sayfası, istemci tarafı QR ve yazdırma düzeni | `b966145` |

## Sprint 3 — TAMAMLANDI

Türkiye evrak takvimi, ICS aboneliği ve acil durum kartı.

| Özellik | Commit |
|---|---|
| `EvrakKurallari`: muayene/egzoz aralıkları, kış lastiği penceresi, uyarı günleri (tek sınıf, ayarlardan ezilebilir) | `23b7744` |
| Evrak CRUD + aylık takvim + yenileme zinciri; Driver yazamaz | `9e1123b` |
| Hatırlatma job'ına evrak taraması: talep-önce-gönder deseni, araç ve kişi alıcıları | `2441502` |
| ICS takvim aboneliği: SHA-256 token, `SystemScope`, VALARM -P7D | `f16d76d` |
| Acil durum kartı: anonim sayfa, `tel:` bağlantısı, kartvizit yazdırma düzeni | `196e22f` |
| SPA Evrak sekmesi, takvim aboneliği ve karne kapsamına acil kart | `cb147b1` |

### Mevzuat değerlerinin kaynağı

Muayene ve egzoz aralıkları, kış lastiği penceresi ve uyarı günleri **yalnız** `Garajim.Business/Concrete/Evraklar/EvrakKurallari.cs` içinde durur; kod başka hiçbir yerde bu sayıları tekrar etmez. Değerler `appsettings` üzerinden ezilebilir:

| Ayar | Varsayılan | Anlamı |
|---|---|---|
| `Evrak:KisLastigi` | `01-12..01-04` | Kış lastiği zorunluluk penceresi (gg-AA..gg-AA) |
| `Evrak:UyariGunleri` | `30,7` | Bitişten kaç gün önce e-posta gönderileceği |

Kod içindeki sabitler: hususi muayene 2 yıl / ticari 1 yıl, egzoz aynı ayrım, ilk muayene tescilden 3 yıl sonra, "yaklaşıyor" eşiği 30 gün. Mevzuat değişirse tek dosya ve tek test dosyası (`EvrakKurallariTests`) güncellenir.

## Sprint 4 — TAMAMLANDI

Rakip uygulamalardan geçiş ve maliyet analizi.

| Özellik | Commit |
|---|---|
| CSV içe aktarma çekirdeği: ayraç/kodlama sezme, Fuelio bölüm başlıkları, Drivvo TR/EN eşanlam tablosu, satır hash'iyle idempotency | `49046e3` |
| SPA geçiş sihirbazı: dosya → eşleme → deneme çalıştırma → aktarım, hatalı satır indirme | `47a2fef` |
| Maliyet analizi: `GET /api/Vehicles/{id}/maliyet`, `GET /api/Reports/filo-maliyet` | `e1ad5e0` |
| Raporlarda maliyet ekranı: kırılım grafiği, tüketim eğrisi, filo tablosu | `32174ab` |

## Sprint 5 — TAMAMLANDI

Filo paketi, sürücü belgeleri ve yolculuk defteri.

| Özellik | Commit |
|---|---|
| Plan paketleri (Bireysel/Filo) ve araç limiti; limit aşımında 402 | `39aa108` |
| Özet panel ucu: `GET /api/Reports/dashboard` | `50f2660` |
| CSV dışa aktarma: `GET /api/Export/{yakit,bakim,masraf,evrak}.csv` | `90fff79` |
| Sürücü belge takibi: `GET /api/Team/belgeler`, en kötü durum sıralaması | `ba24b36` |
| Yolculuk defteri: iş/özel km ayrımı, mesafe değişmezi | `5bc3b2d` |
| SPA: Yolculuk sekmesi, ekip belgeleri tablosu, CSV indirme | `37f404b` |

Plan değerleri `Garajim.Business/Concrete/Planlar/PlanKurallari.cs` içinde tek yerde durur:

| Ayar | Varsayılan | Anlamı |
|---|---|---|
| `Plan:BireyselAracLimiti` | `3` | Bireysel pakette araç üst sınırı |
| `Plan:FiloAracLimiti` | `25` | Filo paketinde araç üst sınırı |
| `Plan:DavetMaxEkArac` | `3` | Davetle kazanılabilecek en fazla ek araç (yalnız Bireysel) |

`Company.AracLimiti` doluysa plan varsayılanını ezer (şirkete özel anlaşma).

## Sprint 6 — TAMAMLANDI

Elektrikli/hibrit araçlar, lastik takibi ve davet programı.

| Özellik | Commit |
|---|---|
| EV/hibrit: `FuelRecord.Kwh` + `SarjTuru`, yakıt türüne göre doğrulama, kWh/100km tüketimi | `04bf6d0` |
| Lastik setleri: takma/sökme geçmişi, otomatik sökme, kış lastiği ve diş derinliği uyarısı | `4573457` |
| Davet programı: şirkete özel kod, davetli listesi | `830588a` |

## Sonraki (planlanmamış)

- [ ] Abonelik ve ödeme sağlayıcı entegrasyonu — bugün plan yükseltme talebi destek kutusuna e-posta olarak düşer, faturalandırma yok
- [ ] Apple/Google Wallet kartı — sertifika ve geliştirici hesabı gerektiriyor
- [ ] Ana ekran widget'ı (yerel uygulama kabuğu gerektiriyor)

## Kill Criteria

Her ölçütün okunacağı kaynak sabittir; başka yerden okunmaz.

| Ölçüt | Eşik | Ölçüm kaynağı |
|---|---|---|
| Fiş çıkarım doğruluğu | İlk 30 Türk fişinde tarih+tutar+km doğruluğu %85'in altındaysa prompt/sağlayıcı revizyonu; ikinci turda da altındaysa fotoğraf-önce stratejisi sorgulanır | `tools/Garajim.Calibration` çıktısındaki `alanDogruluk`; çapraz kontrol `GET /api/Receipts/stats` → `alanDuzeltmeOrani` |
| Tutunma | İlk 100 kullanıcının %25'inden azı 30. günde hâlâ fiş yüklüyorsa tez yeniden değerlendirilir | `GET /api/Receipts/stats` → `toplamCagri` (şirket başına, aylık) |
| Karne paylaşımı | Paylaşım oranı %15'in altındaysa davet programı öne çekilir | `GET /api/Vehicles/karne-stats` → `aktifOran` ve `toplamGoruntulenme` |

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

Hiçbir sprinte bağlanmamış işler. Sprint 3-6'ya taşınanlar buradan çıkarıldı; her madde tek yerde durur.

- [ ] Periyodik bakım şablonları: kilometre ve takvim bazlı
- [ ] Filo ortalamasından tüketim sapması uyarısı (L/100km ve kWh/100km Sprint 4-6'da geldi)
- [ ] Excel'den toplu içe aktarma (CSV dışındaki kaynaklar)
- [ ] Audit log
- [ ] Ödeme sağlayıcı entegrasyonu (iyzico veya PayTR) — plan ve bonus gün alanları hazır, faturalandırma yok
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
