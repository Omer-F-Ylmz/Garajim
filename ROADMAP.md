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
| `Evrak:KisLastigi` | `15-11..15-04` | Kış lastiği zorunluluk penceresi (gg-AA..gg-AA) |
| `Evrak:UyariGunleri` | `30,7` | Bitişten kaç gün önce e-posta gönderileceği |

Kod içindeki sabitler: hususi muayene 2 yıl / ticari 1 yıl, egzoz aynı ayrım, ilk muayene tescilden 3 yıl sonra, "yaklaşıyor" eşiği 30 gün. Mevzuat değişirse tek dosya ve tek test dosyası (`EvrakKurallariTests`) güncellenir.

Kış lastiği penceresinin kaynağı: 4 Ekim 2025 tarihli Resmî Gazete tebliği pencereyi **15 Kasım – 15 Nisan** olarak belirledi (önceki düzenlemede 1 Aralık – 1 Nisan'dı). Valilikler pencereyi bir ay öne veya arkaya uzatabildiği için bu değer koda gömülü değil; il bazında `Evrak:KisLastigi` ezmesiyle karşılanır (örn. `15-10..15-05`). Tebliğ M+S işaretli lastiği kabul ettiği için panel uyarısında dört mevsim seti de yeterli sayılır.

### Kaza anı rehberinin kaynağı

Kaza anında ne yapılacağına dair metin **yalnız** `Garajim.Business/Concrete/KazaRehberi.cs` içinde durur; SPA ve API bu tek sınıftan okur, metin başka dosyada tekrar edilmez. `GET /api/Hasar/rehber` bu sınıfı döner.

Rehberin dayandığı kaynaklar:

- **Anlaşmalı tutanak koşulları ve polis çağrılması gereken haller:** 2918 sayılı Karayolları Trafik Kanunu ile Maddi Hasarlı Trafik Kazası Tespit Tutanağı düzenleme esasları. Yalnız maddi hasar, iki sürücünün de olay yerinde ve ehliyetli olması, alkol/uyuşturucu bulunmaması, kamu malına zarar gelmemiş olması ve iki araçta da geçerli ZMSS bulunması koşullarının tamamı aranır.
- **Sigortaya bildirim süresi:** Karayolları Motorlu Araçlar Zorunlu Mali Sorumluluk Sigortası Genel Şartları — kazanın öğrenildiği tarihten itibaren **5 iş günü**.

Mevzuat değişirse tek dosya ve tek test dosyası (`KazaRehberiHttpTests`) güncellenir. Metin bilgilendirme amaçlıdır, resmî tutanak yerine geçmez; `GET /api/Hasar/{id}/tutanak.html` çıktısı da bu notu taşır.

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

## Sprint AI Usta — TAMAMLANDI

Aracın kendi kayıtlarını okuyup olasılık sıralayan yardımcı; teşhis koymaz.

| Özellik | Commit |
|---|---|
| Bilgi tabanı: 5 JSON dosyası, şema doğrulayan yükleyici, DTC + anahtar puanlı seçici, kırmızı çizgi tablosu | `3fdfa39` |
| Veri modeli ve uçlar: onay kapısı, günlük kota, sohbet sınırı, şema denetimi, geri bildirim | `da3b62c` |
| Prompt sözleşmesi: sürümlü `SistemPromptu.md`, sıralama/kırpma/şema testleri | `d59557b` |
| Arayüz: Usta sekmesi, onay akışı, kademeli yanıt kartı, sesli sorma | `6827f2d` |
| Ölçüm ve anonim çözüm özeti: `GET /api/usta/stats`, günlük job, eşikli Garajım verisi | `293a250` |
| Kullanım Şartları ve Aydınlatma Metni, onay sürüm kapısı | `269fc7d` |
| Üretim kapısı: `Usta__SahteYanit` açıkken uygulama başlamaz | `70d6c6a` |
| Bilgi tabanı 1. parti: belirtiler (60) + TÜVTÜRK (22) | `ca21390` |
| Saklama job'ı: 24 aydan eski sohbetler silinir, çözüm özeti birikimli kalır | `44bfb73` |

Bilinçli sınırlar: model çağrısı yalnız Gemini üzerinden yapılır (sağlayıcı seçimi yok); yanıtlar önbelleğe alınmaz; sesli soru yalnız tarayıcıda metne çevrilir, ses kaydı sunucuya gitmez; anonim çözüm özeti `n >= 30` eşiğinin altında prompta girmez ve varsayılan olarak kapalıdır.

## Sprint 7 — TAMAMLANDI

Kaza ve hasar dosyası, beyan bazlı değer takibi ve fiyat tahmini bağı.

| Özellik | Commit |
|---|---|
| Hasar dosyası: veri modeli, CRUD uçları, etiketli fotoğraflar, tutanak özeti, panel sayacı, CSV, karne bayrağı | `412bc72` |
| Kaza anı rehberi: tek kaynak metin, `GET /api/Hasar/rehber`, mobil tek dokunuşluk akış | `fec438d` |
| Değer takibi: beyan serisi, model kapsamlı tahmin (422 kapsam dışı), sahiplik maliyeti, filo toplam değeri | `1746ea3` |
| Arayüz: Hasar sekmesi ve üç adımlı sihirbaz, Değer kartı ve grafiği, iki yeni karne kutusu | `a3d88ea` |

Bilinçli sınırlar: karşı tarafın adı, telefonu, kimlik ve sürücü belgesi bilgisi **saklanmaz** — yalnız yazdırılan tutanak özetinde elle doldurulacak boş alan olarak yer alır. Dosya başına en fazla 20 fotoğraf ve fotoğraflar mevcut belge kotasından düşer. Değer tahmini araç başına günde 3 kez alınabilir; Kaynak=Tahmin elle girilemez, yalnız modelden üretilir. Karne bayrakları (`HasarGecmisi`, `BeyanDegeri`) varsayılan kapalıdır; açıkken hasar satırı yalnız tarih + tür + onarıldı/açık, değer satırı yalnız son beyan/ekspertiz taşır — tutar, konum, karşı taraf, fotoğraf ve tahmin paylaşılmaz. AI Usta araç bağlamına değer verisi girmez.

Aracın kasa tipi araç kartında tutulmadığı için tahmin modeline boş geçilir; uydurma bir kasa tipi göndermek yerine modelin bilinmeyen-kategori davranışına bırakıldı. Kapsam denetimi ayrı bir liste tutmaz, model zip'inin kendi `MarkaEncoded` / `SeriEncoded` slot adlarından okunur — model yeniden eğitildiğinde kapsam kendiliğinden güncellenir.

## Launch Hazırlık — TAMAMLANDI

Yayın öncesi son tur: modelin eksik girdisi, demo hesabının doluluğu, kaza anının çevrimdışı çalışması ve giriş öncesi tanıtım.

| Madde | Commit |
|---|---|
| Kasa tipi: araç kartında seçim, tahminde zorunlu alan, model sözlüğüne bağlı enum | `b2729c4` |
| Demo veri: evrak, lastik, parçalı bakım, hasar, değer ve yolculuk artımlı eklendi | `ac50568` |
| Çevrimdışı kaza akışı: rehber önbellekte, dosya açma kuyrukta | `cf9ff76` |
| Tanıtım sayfası: değer önerisi, altı özellik kartı, demo girişi ve davet kodu | `6c4b126` |

Kasa tipi bulgusu: değer tahmini bugüne kadar modele **boş kasa tipi** gönderiyordu; OneHotEncoding bunu sıfır vektöre çevirdiği için tahmin kasa bilgisi olmadan üretiliyordu. Aynı araçta ölçülen fark: boş kasa 1.011.072 TL, gerçek Hatchback/5 894.777 TL (%12). Artık kasa tipi boşken model hiç çağrılmıyor, 422 dönüyor. Kasa kümesi ayrı bir listede tutulmuyor; model zip'inin `KasaTipiEncoded` slot adlarından okunuyor ve `KasaTipiSozlukTests` ile enum'a bağlanıyor.

Çevrimdışı kararı: CLAUDE.md anonim `acil.html` ve `/api/karne/*` yollarının önbelleğe girmesini yasaklıyor, çünkü bayat kopya paylaşılan araç hakkında yanlış bilgi verir. Bu kural korundu; acil kart bunun yerine giriş yapmış kullanıcının **kendi** aracının verisinden localStorage'a yazılıp Kaza anı ekranında çevrimdışı gösteriliyor. Kaza rehberi kişisel veri taşımadığı ve herkes için aynı olduğu için service worker'da önbelleğe alınıyor.

## Sıradaki

Yayın sonrası ilk iki ölçüm, ikisi de Kill Criteria tablosundaki kaynaklardan okunacak:

- [ ] **Kalibrasyon sonucu** — ilk 30 Türk fişinde `tools/Garajim.Calibration` çalıştırılıp `alanDogruluk` raporlanır; %85 eşiğinin altındaysa prompt revizyonu turu açılır.
- [ ] **Kill-criteria ölçümü** — tutunma (`GET /api/Receipts/stats`) ve karne paylaşım oranı (`GET /api/Vehicles/karne-stats`) 30. günde okunur ve eşiklerle karşılaştırılır.

## Denetim ve düzeltmeler

| Tur | Commit'ler |
|---|---|
| DÜZELTME-3 (davet ödülü → araç hakkı, ekip yetkileri, plan talebi + kış lastiği uyarısı) | `f5507dc` `c735883` `f0f3526` |
| DÜZELTME-3b (kış lastiği penceresi 15 Kas–15 Nis, dört mevsim M+S sayılır) | `1b9788d` |
| DENETİM-2 bulguları | `ea3e7e0` `75423f6` `7d9a7b0` `5d4d993` `6f819b7` `5281b7b` `1964557` |

## Sonraki (planlanmamış)

- [ ] Abonelik ve ödeme sağlayıcı entegrasyonu — bugün plan yükseltme talebi destek kutusuna e-posta olarak düşer, faturalandırma yok
- [ ] Apple/Google Wallet kartı — sertifika ve geliştirici hesabı gerektiriyor
- [ ] Ana ekran widget'ı (yerel uygulama kabuğu gerektiriyor)
- [ ] AI Usta için ikinci sağlayıcı (OpenAI) ve yanıt önbelleği — tek sağlayıcı maliyeti kabul edilebilir kaldığı sürece açılmaz
- [ ] Bilgi tabanının yönetim arayüzünden düzenlenmesi — bugün JSON dosyaları repoda tutuluyor

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

Sprint 7'de gelen hasar dosyası ve fotoğraf altyapısı bilinçli olarak **genel** yazıldı: `HasarDosyasi` bir araca bağlı, tarihli, durumlu, etiketli fotoğraf taşıyan bir olay kaydıdır; kiralama sözleşmesine ya da müşteriye bağlı değildir. Teslim-iade tutanağı bu parçaların üstüne oturur — aynı `HasarFoto` etiket kümesi (genel görünüm, yakın çekim, plakalar, belge), aynı 20 fotoğraf sınırı, aynı belge kotası, aynı `TutanakSayfasi` yazdırma çıktısı ve aynı "kişisel veriyi kayda değil çıktıya bırak" kuralı kullanılacak. Teslim-iade için eklenecek olan yalnızca kiralama bağlamı (sözleşme no, teslim/iade yönü, km ve yakıt seviyesi) olacak; hasar kaydının kendisi yeniden yazılmayacak.

## Kırmızı Takım Denetimi — açık kalan bulgular

Yayın öncesi dört bağımsız ajan (güvenlik, veri bütünlüğü, mobil/UX, performans) repoyu sıfırdan okudu; 58 bulgu döküldü, yinelenenler ayıklanınca 37 ayrı madde kaldı. Kapatılanlar `e67d174`…`0486f9f` arasındaki on dört commit'te. Aşağıdakiler bilinçli olarak ertelendi; her biri kaynağıyla birlikte duruyor ki yeniden keşfedilmesin.

### Sıradaki turda alınacaklar

- [ ] **Fiş taslağı dosyaları şirket kotasına sayılmıyor** (`ReceiptManager.cs:104-123`) — `Document` satırı ancak onayda açılıyor, `Bekliyor` taslakların dosyası kotanın dışında diskte duruyor. Aylık 100 fiş × 5 MB kotaya yansımadan yazılabilir. Reddedilen taslaklar siliniyor ama bekleyenleri temizleyen job yok.
- [ ] **`/api/Evrak` listesinde N+1 ve üst sınır yok** (`EvrakManager.cs:43,221-227,250-251`) — `vehicleId` verilmezse şirketin tüm evrakı çekiliyor, `MapAsync` kayıt başına iki sorgu atıyor, Driver rolünde üç sorgu daha ekleniyor. 200 evraklı şirkette tek istekte ~1000 sorgu.
- [ ] **`/api/Hasar` listesinde fotoğraf sayısı dosya başına ayrı sorgu** (`HasarManager.cs`) — liste kendisi sınırlı ama sayım N+1.
- [ ] **AI Usta kota kapısı ile yazma arasında model çağrısı var** (`UstaManager.cs:181-187` → `:222`) — sayacı artıran kullanıcı mesajı model yanıtından sonra yazılıyor; paralel istekler aynı sayaçla geçip hepsi Gemini'ye gidiyor. Pencere saniyeler sürüyor.
- [ ] **Gemini çağrısında toplam süre sınırı yok** (`UstaIstemci.cs:28,111-114`) — 40 sn timeout iki denemeyle 80 sn'ye çıkıyor, yanıt boyutu sınırsız tamponlanıyor. Aynı sınırsız tamponlama fiş çıkarımının yanıt okumasında da var (`ReceiptExtractorBase.cs:56`).
- [ ] **Usta araç bağlamı "hepsini yükle sonra Take"** — kayıtlar belleğe alındıktan sonra kırpılıyor, SQL'e inmiyor.
- [ ] **Fiş istatistikleri tüm taslakları belleğe alıyor** (`ReceiptManager.GetStatsAsync`) — sayım ve ortalama SQL'de yapılabilir.
- [ ] **20 fotoğraf sınırı ve `Sira` üretimi yarışa açık** (`HasarManager.cs:190-215`) — sayım ile insert arasında tam bir dosya yükleme var; `(HasarDosyasiId, Sira)` indeksi tekil değil.
- [ ] **Belge kotası kontrol-sonra-yaz** (`DocumentManager.cs:57-82`) — eşzamanlı yüklemeler aynı toplamı okuyup kotayı aşabilir; veritabanı tarafında kısıt yok.
- [ ] **`UstaCozumOzeti` doğal anahtarında tekil indeks yok** (`GarajimDbContext.cs:213`) — `BulAsync` beş alanla arıyor, indeks dört alanlı ve tekil değil; job iki kez koşarsa çift satır oluşup sayım bölünür.
- [ ] **Okuma sorguları izlemeli** (`EfEntityRepositoryBase.GetAsync`, `EfVehicleAssignmentDal.GetActiveByVehicleAsync`) — `AsNoTracking` yok; yalnız okunan kayıtlar da değişiklik izleyicisinde birikiyor.
- [ ] **`capture="environment"` galeriden seçimi engelliyor** (`index.html:143,1003,1152`) — kullanıcı e-postayla gelen PDF fişi ya da daha önce çektiği fotoğrafları yükleyemiyor, `multiple` etkisiz kalıyor.
- [ ] **Ağ hatası mesajları Türkçeleştirilmemiş** — iOS Safari'nin "Load failed" metni kullanıcıya olduğu gibi çıkıyor, doğrulama hatalarında alan adları PascalCase geliyor.
- [ ] **Acil durum kartının kullanıcıya ulaşan bağlantısı yok** (`KarneManager.cs:98`) — karne kapsamında kutu var ama üretilen URL yalnız `karne.html`; `acil.html`'e hiçbir yerden gidilemiyor.
- [ ] **Anonim karne ucunda bakım/yakıt/belge listeleri sınırsız** (`KarneManager.cs:141,163,175`) — hasar dalı sınırlı, diğer üçü değil.
- [ ] **ICS takviminde araç kısıtı SQL'e inmiyor** (`TakvimManager.cs:96-97`) — tek araçlı Driver için bile şirketin tüm evrak ve hatırlatması belleğe alınıyor.
- [ ] **Modal odak yönetimi ve tab rolleri eksik** (`index.html:1138`, `app.js:1729`) — Kaza modalı açıkken arka plana sekme yapılabiliyor, Escape kapatmıyor, `role="tablist"` altındaki düğmelerde `role="tab"`/`aria-selected` yok, doğrulama kutularının 2-6'sının erişilebilir adı yok.
- [ ] **`sw.js` kabuk listesi eksik** — `/garajim-icon-32.png` ve `/vendor/qr.js` önbelleğe alınmıyor, çevrimdışı ilk açılışta QR üretilemiyor.
- [ ] **Kayıt ucu kullanıcı numaralandırmasına izin veriyor** (`AuthManager.cs:47-48`) — kayıtlı e-posta 400, kayıtsız 201 dönüyor; `kod-gonder` deseni doğru uygulanmış, register'a uygulanmamış.
- [ ] **Parola politikası 6 karakter, hesap kilitleme yok** (`AuthManager.cs:43-44`) — başarısız deneme sayacı yalnız e-posta kodunda var, parolada yok.
- [ ] **Güvenlik yanıt başlıkları yok** (`Program.cs:337-365`) — CSP, X-Frame-Options, nosniff, Referrer-Policy hiçbiri ayarlı değil; JWT `localStorage`'da.
- [ ] **CSV dışa aktarımında formül enjeksiyonu** (`ExportManager.cs:183-197`) — baştaki `= + - @` nötrleştirilmiyor.
- [ ] **Usta geri bildirimi sohbet sahipliğini denetlemiyor** (`UstaManager.cs:323-364`) — okuma ucunda olan `sohbet.UserId` kontrolü geri bildirim ve çözüm uçlarında yok.
- [ ] **Paylaşım token'ları URL yolunda** (`KarneController.cs:24,33,42`) — erişim loglarında düz metin duruyor; takvim aboneliğinin son kullanma tarihi hiç yok.
- [ ] **Hatırlatma job'ında iki boş `catch {}`** (`ReminderNotificationJob.cs:112-114`, `:199-201`) — e-posta gönderimi sessizce yutuluyor, hangi bildirimin düştüğü hiçbir yere yazılmıyor.
- [ ] **`SwaggerHttpTests` sınıf temizliğinde ara sıra NullReferenceException** — bir koşuda görüldü, tekrarında yok; test altyapısı kaynaklı, ürünü etkilemiyor.
- [ ] **Türkçe metin tutarsızlıkları** — `Messages.cs:138` "lastigi" (ğ eksik), "Owner" ile "Sahip" karışık, "kütüphane"/"kitaplık", "jpg, png" ile "jpg, jpeg, png" farkı.
- [ ] **Üç eski migration `Up()` içinde `AlterColumn`/`DropIndex` taşıyor** (`AddCompanyTenancy`, `AddPerformanceIndexes`, `PlakaSirketBazindaTekil`) — üçü de canlıya uygulanmış, risk yok; kuralı doğrulayan test yok.
- [ ] **`Companies.DavetEdenCompanyId` üzerinde indeks ve FK yok** (`DavetProgrami.cs:13-17`) — her araç ekleme isteğinde tam tablo taraması.

### İncelenip bulgu sayılmayanlar

- **Demo hesabın parolası repoda** — bulgu olarak açılmıştı, kusur değil. Parola zaten `app.js` içinde tarayıcıya gönderiliyor; "Demo ile dene" düğmesinin çalışması için gönderilmek zorunda ve `DEPLOY.md` demo verisini bilinçli bir dağıtım kararı olarak tanımlıyor. Guard'a eklemek belgelenmiş bir özelliği kırardı. Gerçek artık risk parola değil, canlıda herkese açık bir demo kiracısının kota ve AI Usta maliyetini tüketebilmesi; bu ayrı bir madde olarak ele alınmalı.

## Güvenlik Taraması — açık kalan bulgular

`b966145..HEAD` (87 commit) ile `Garajim.API`, `Garajim.Business` ve `Garajim.Dal`'ın tamamı veri akışı izlenerek tarandı. Enjeksiyon, XSS, kimlik/yetki, dosya yükleme, kriptografi, loglama, yapılandırma ve üçüncü taraf çağrıları kapsandı. İki Orta bulgu kapatıldı (`fd1cbd9`, `ff1cc75`), bir Düşük erken kapatıldı (`0e43216`). Aşağıdakiler ertelendi.

- [ ] **Ekip üyesi ekleme e-postayı kanıtsız sahipleniyor** (`TeamManager.cs:120-135`) — Sahip herhangi bir adresle `EmailDogrulandi = true` hesap açabiliyor. Adres genel tekil olduğu için o kişi bir daha kendi şirketini kuramaz; adresin sahibine hiçbir bildirim gitmez. Doğru çözüm davet-ve-onay akışı; bu bir özellik, yama değil.
- [ ] **Parola değiştirme ve sıfırlama akışı yok** — hiçbir uçta yok. Ekip üyesine verilen geçici parola kalıcı ve Sahip tarafından biliniyor; üye kendi kimliğini döndüremiyor, Sahip süresiz olarak üye adına işlem yapabiliyor.
- [ ] **Tutanak açılır penceresi uygulamayla aynı kaynakta çalışıyor** (`app.js:2038-2047`) — `window.open("")` + `document.write` kullanıldığı için sayfa `about:blank` olarak açılıp kaynağı devralıyor. Bugün sömürülebilir değil: `TutanakSayfasi.Kacir` `& < > " '` karakterlerinin hepsini kaçırıyor. Ancak sunucu kaçışındaki ileride oluşacak tek bir açık, JWT `localStorage`'da durduğu için doğrudan token hırsızlığına döner. `srcdoc` taşıyan `sandbox`'lı bir iframe ya da ayrı bir uç daha güvenli olur. CSP eklenmesi (`ff1cc75`) etkiyi azalttı, kökü kapatmadı.
- [ ] **Usta geri bildirimi sohbetin sahibini denetlemiyor** (`UstaManager.cs:323-355`) — araç erişimi denetleniyor, dolayısıyla kiracılar arası sızma yok; aynı şirkette aracı gören başka bir kullanıcı, meslektaşının sohbet mesajına geri bildirim yazabiliyor.
- [ ] **Vekil sunucu arkasında istemci IP'si çözülmüyor** (`Program.cs:181-207`) — `KnownProxies`/`KnownNetworks` temizleniyor ve yapılandırma boşsa `X-Forwarded-For` hiç uygulanmıyor. Güvenlik açısından doğru yön (başlık sahteciliği kapalı) ama `ForwardedHeaders__KnownProxies` tanımlanmadan IIS arkasına konursa tüm istemciler tek IP bölümüne düşer ve giriş hız sınırı ile anonim uç sınırı ortaklaşır. `DEPLOY.md` bunu söylemiyor.
- [ ] **`DEPLOY.md` doğrulama adımı Swagger'dan 200 bekliyor** (`DEPLOY.md:98`) — Swagger üretimde kapatıldıktan sonra (`0486f9f`) bu adım artık 404 döner; dağıtımı yapan kişi hatalı olarak başarısızlık sanır.

### İncelenip temiz çıkanlar

Ham SQL, komut çalıştırma, XML ayrıştırma ve güvensiz deserialization hiç yok. Parola `HMACSHA512` + `FixedTimeEquals`; doğrulama kodu, paylaşım ve takvim token'ları, davet kodu ve geçici parola `RandomNumberGenerator` ile üretiliyor; token'lar veritabanında SHA-256 özetiyle duruyor. Global sorgu filtresi 22 entity'yi kapsıyor, rol değişimi ve hesap kapatma hedefi filtre üzerinden okuduğu için kiracılar arası yetki yükseltme kapalı. `AppUser.CompanyId` oluşturulduktan sonra hiç değişmiyor, dolayısıyla JWT'deki kiracı iddiası bayatlamıyor. Dosya yükleme uzantı beyaz listesi, sihirli bayt denetimi ve boyut sınırı taşıyor; kayıtlı ad sunucuda üretilen GUID olduğu için yol geçişi yok. Anonim karne belge ucu belgeyi paylaşılan araca bağlıyor. İndirmeler `Content-Disposition: attachment` ile dönüyor. Loglarda parola, token ya da doğrulama kodu yok. CORS hiç açılmamış. Üretimde geliştirici hata sayfası yok. `dotnet list package --vulnerable --include-transitive` dokuz projede de temiz.

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
