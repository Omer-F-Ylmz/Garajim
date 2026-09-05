# Yayın Günü Kontrol Listesi

MonsterASP.NET üzerine Web Deploy (MSDeploy) ile yayın içindir. Adımları sırayla uygula; bir adım beklenenden farklı sonuç verirse **dur**, sonraki adıma geçme.

Yayın öncesi durum: 1388 test yeşil, Release derlemesi 0 uyarı / 0 hata, CI `main` üzerinde başarılı.

## 1. Veritabanı yedeği (atlanamaz)

1. MonsterASP panelinden veritabanı yedeğini al.
2. Yedeği **bilgisayarına indir** ve dosya boyutunun sıfırdan büyük olduğunu gör. Panelde "yedek alındı" yazması yeterli değil; indirilmemiş yedek yedek sayılmaz.

Neden şart: `Down` migration'ları veri kaybettirir. `AddCompanyTenancy` geri alınırsa `CompanyId` kolonları, `AddCompanyRoles` geri alınırsa rol ve aktiflik bilgisi silinir. Sprint 3-6 ve AI Usta migration'larının `Down` gövdeleri de tablo ve kolon düşürür (evrak, takvim aboneliği, içe aktarma kayıtları, yolculuk defteri, lastik setleri, davet kodları, AI Usta sohbet ve mesajları). **Geri alma yok, yedekten dönülür.** Geri dönüşün tek güvenli yolu yedekten dönmektir.

Yayın penceresinde uygulanacak migration'ların tamamı **yalnız eklemelidir** — `AddColumn`, `CreateTable`, `CreateIndex`:

| Migration | Yaptığı |
|---|---|
| `HasarDosyasi` | `HasarDosyalari` ve `HasarFotograflari` tablolarını açar; `KarnePaylasimlari`'na `HasarGecmisi bit NOT NULL DEFAULT 0` kolonu ekler |
| `AracDeger` | `AracDegerleri` tablosunu açar; `KarnePaylasimlari`'na `BeyanDegeri bit NOT NULL DEFAULT 0` kolonu ekler |
| `AracKasaTipi` | `Vehicles`'a `KasaTipi int NULL` kolonu ekler |
| `EmailDogrulama` | `Users`'a beş doğrulama kolonu ekler ve **mevcut satırları doğrulanmış yapar** (`UPDATE Users SET EmailDogrulandi = 1`), böylece eski kullanıcılar giriş yapmaya devam eder |
| `LastikTekTakiliSet` | `LastikSetleri` üzerinde araç başına tek takılı seti garantileyen filtreli tekil indeks açar |
| `SifreSifirlama` | `Users`'a şifre sıfırlama kolonlarını ve `SifreDegisimTarihi` kolonunu ekler |
| `GeciciSifreBayragi` | `Users`'a `GeciciSifre bit NOT NULL DEFAULT 0` kolonu ekler |
| `YabanciPlakaVeNormalizasyon` | `Vehicles`'a `YabanciPlaka` kolonu ekler ve **mevcut plakaları normalize eder** (büyük harf, boşluk/tire silme); normalize hali şirkette zaten varsa satıra dokunulmaz |
| `TamDolumVeSupheliKm` | `FuelRecords`'a `TamDolum` (mevcut satırlarda **true**) ve `SupheliKm` kolonlarını ekler |
| `KmDuzeltmeLog` | `KmDuzeltmeLoglari` tablosunu açar |
| `AracArsivleme` | `Vehicles`'a `Arsivli`, `ArsivTarihi`, `ArsivNedeni` kolonlarını ekler |
| `HesapSilme` | `Users`'a hesap silme kodu kolonlarını, `Companies`'e `SilinmePlanlanan` kolonunu ekler |
| `AiTokenVeFisTokenlari` | `AiTokenSayaclari` tablosunu açar, `ReceiptDrafts`'a token kolonlarını ekler |
| `KmTazeligiVeKarneVarsayilani` | `Vehicles`'a `SonKmGuncelleme` kolonu ekler |
| `AracModelEslesmedi` | `Vehicles`'a `ModelEslesmedi bit NOT NULL DEFAULT 0` kolonu ekler |
| `KurulumGizlendi` | `Users`'a `KurulumGizlendi bit NOT NULL DEFAULT 0` kolonu ekler |
| `TurTamamlandi` | `Users`'a `TurTamamlandi bit NOT NULL DEFAULT 0` kolonu ekler |
| `OrnekArac` | `Vehicles`'a `Ornek bit NOT NULL DEFAULT 0` kolonu ekler |
| `GeriBildirim` | `GeriBildirimler` tablosunu ve iki indeksini açar |
| `ProfilVeBildirimTercihleri` | `Users`'a `BildirimEvrak` ve `BildirimHatirlatma` (ikisi de **varsayılan true**) ile e-posta değişikliği kolonlarını ekler |
| `YonetimKotaHatasi` | `AiTokenSayaclari`'na `KotaHatasi int NOT NULL DEFAULT 0` kolonu ekler |

İki karne kolonu da varsayılan `0` ile gelir; mevcut karne bağlantıları hasar ve değer bilgisini **paylaşmadan** çalışmaya devam eder. Kolon ya da tablo düşürülmez, tip değiştirilmez. Hasar fotoğrafları mevcut belge deposuna yazılır ve şirket kotasından düşer; yedek alırken `documents` klasörünü de indir.

**Ücretsiz katman kotası model başına günde 20 istektir** (`GenerateRequestsPerDayPerProjectPerModel-FreeTier`). Fiş ve Usta aynı anahtarı paylaşır ama farklı model adları ayrı kotadan sayılır; kota dolunca uç 503 "AI hizmeti geçici olarak dolu" döner, taslak oluşmaz ve aylık fiş limiti düşmez.

İlk açılışta `KatalogEslemeJob` bir kez çalışır: mevcut araçların marka ve modelini katalog yazımına çeker, eşleşmeyenlere `ModelEslesmedi = 1` yazar. İş şirket şirket ilerler, kendini tekrar ettiğinde hiçbir satırı değiştirmez ve `Katalog__BaslangictaEsle=false` ile kapatılabilir. Kapatırsan mevcut araçlar eski yazımıyla kalır ve değer tahmini isteyene kadar sorun çıkmaz.

Ayrıca: **belgeler veritabanı yedeğinde yoktur.** Sunucuda daha önce yüklenmiş belge varsa, `App_Data/documents` klasörünü de ayrıca indir.

### Publish öncesi migration sayımı

Yayından **önce** sunucudaki geçmişi oku ve repodaki sayıyla karşılaştır:

```sql
SELECT COUNT(*) FROM __EFMigrationsHistory;
```

Repoda bugün **45** migration var. Canlı Sprint 2 şemasındaysa (son uygulanan `KarnePaylasimi`, yani 12 satır) bu yayında **27 migration** uygulanacak:

| Tur | Adet |
|---|---|
| Sprint 3-6 ve AI Usta | 12 |
| Sprint 7 (`HasarDosyasi`, `AracDeger`) | 2 |
| Launch hazırlık (`AracKasaTipi`) | 1 |
| E-posta doğrulama (`EmailDogrulama`) | 1 |
| Kırmızı takım (`LastikTekTakiliSet`) | 1 |
| Sprint Şifre (`SifreSifirlama`, `GeciciSifreBayragi`) | 2 |
| İnce ayar 1 (plaka, tam dolum, km düzeltme, arşiv, hesap silme, AI token, km tazeliği) | 7 |
| Marka/model (`AracModelEslesmedi`) | 1 |

Ölçülen süre (LocalDB, 3 Eylül 2026, `dotnet ef database update --no-build`, boş veritabanı): sıfırdan 38 migration **4,0 sn**; 39'uncu migration tek kolon eklediği için bu süreyi ölçülebilir biçimde değiştirmez. Uzak MSSQL'de ağ gecikmesi ve dolu tablolar eklendiğinde bu sürenin birkaç katına çıkmasını bekle, yine de **bir dakikanın altında** kalmalı. `ApplyMigrationsAtStartup` açıksa ilk istek bu kadar gecikir; tercihen kapalı tutulup migration ayrı çalıştırılır.

Sayım beklenenden farklıysa **dur**: canlı şema tahmin ettiğinden eski ya da yeni demektir. Hangi migration'ların uygulanacağını görmeden yayına çıkma; `dotnet ef migrations list` ile karşılaştır.

## 2. Panel ortam değişkenleri

### Sprint ONBOARDING ile gelen değişken

| Değişken | Durum | Not |
|---|---|---|
| `App__YoneticiEposta` | Yönetim paneli için zorunlu | Tek adres. Bu hesap `/api/Yonetim/*` uçlarını ve `yonetim.html` sayfasını açar; boş bırakılırsa panel herkese 403 döner ve uygulamanın geri kalanı etkilenmez. Bu yayında `omery3899@gmail.com` girilecek |

Yeni bir zorunlu değişken yok. Onboarding akışının kalanı (kurulum çubuğu, ürün turu, boş durumlar, örnek araç, yardım sayfası, geri bildirim, profil, PWA ipucu) yapılandırma istemez. Geri bildirim e-postası mevcut `App__DestekEposta` adresine gider; boşsa kayıt yine tutulur, yalnız e-posta gönderilmez.



Yayından **önce** ayarla:

| Değişken | Değer | Not |
|---|---|---|
| `Documents__StoragePath` | `..\private\documents` (site kökünde, wwwroot'un yanında) | Aşağıdaki açıklamayı oku |
| `ConnectionStrings__Default` | Uzak MSSQL bağlantısı | LocalDB içeriyorsa uygulama açılmayı reddeder |
| `Jwt__Key` | 32+ karakter | Boş veya kısa ise uygulama açılmayı reddeder |
| `DemoSeed__Enabled` | `true` veya `false` | Canlıda demo verisi isteniyor mu, karar ver |
| `App__BaseUrl` | `https://<site>` | Karne, ICS takvim ve davet bağlantıları bu değerle kurulur; boşsa paylaşılamaz |
| `App__DestekEposta` | Destek kutusu adresi | Plan yükseltme talepleri buraya düşer; boşsa form 400 döner |
| `Smtp__Host` | SMTP sunucusu (ör. Brevo: `smtp-relay.brevo.com`) | **Zorunlu.** Kayıt doğrulama kodu buradan gider; eksikse uygulama açılışta durur |
| `Smtp__User` | SMTP kullanıcısı | **Zorunlu** |
| `Smtp__Pass` | SMTP parolası / API anahtarı | **Zorunlu.** Panelde saklanır, repoya yazılmaz |
| `Smtp__From` | Gönderen adresi (doğrulanmış alan adı) | **Zorunlu.** Alan adı doğrulanmamışsa sağlayıcı reddeder, kimse kayıt olamaz |
| `Smtp__Port` | Varsayılan `587` | Sağlayıcı farklı port istiyorsa |
| `Receipts__Model` | `gemini-3.5-flash-lite` | Kodun varsayılanı da budur. **2.5 modelleri yeni anahtarlara kapalı**, `generateContent` 404 döner ve uç 502 verir |
| `Usta__Model` | ayarlanmaz | Boşsa `Receipts__Model`'e düşer |
| `Usta__Enabled` | `true` ya da `false` | `false` iken Usta sekmesi gizlenir, `/api/Usta/*` 503 döner, özet job'ı atlar |
| `Usta__ApiKey` | Gemini anahtarı | AI Usta için; boşsa `Receipts__ApiKey` kullanılır, o da boşsa uç 502 döner |
| `Receipts__ApiKey` | Gemini/OpenAI anahtarı | **Fiş okuma için zorunlu.** Boşsa akış çalışır ama her fiş boş taslak ve sıfır güvenle döner; kullanıcı bunu hata sanar |
| `Usta__SahteYanit` | **canlıda ayarlanmaz** | Yalnız geliştirmede `true`; üretimde açık bırakılırsa uygulama açılışta açık hatayla durur |
| `Swagger__Enabled` | **ayarlanmaz** | Üretimde varsayılan kapalı. Açılırsa tüm uç ve DTO yüzeyi yayında olur; şemayı görmek gerekirse geçici aç, iş bitince kapat |
| `Katalog__BaslangictaEsle` | ayarlanmaz | Varsayılan açık; açılışta mevcut araçların marka/modelini katalog yazımına çeker, fikir sabitidir. Kapatılırsa eski yazımlar kalır |
| `Hangfire__WorkerCount` | `1` | Job'lar tüm şirketleri tarar; 256 MB'lık sunucuda paralellik bellek riskidir |
| `Ai__AylikTokenTavani` | Aylık token bütçesi ya da ayarlanmaz | Fiş + AI Usta toplamı; aşılınca iki uç 503 döner ve destek adresine tek e-posta gider. `0` ya da boş = sınırsız |
| `ForwardedHeaders__KnownProxies` | Vekil sunucunun IP'si | Boşsa `X-Forwarded-For` hiç uygulanmaz ve **tüm istemciler tek IP** sayılır; giriş hız sınırı ile anonim uç sınırı ortaklaşır |
| `Security__ScriptKaynaklari` | ayarlanmaz | CSP `script-src` listesi; varsayılan `https://cdn.jsdelivr.net` (Chart.js). CDN değişmedikçe dokunma |
| `RateLimiting__PahaliUcPerMinute` | ayarlanmaz | Varsayılan 20; fiyat tahmini, içe/dışa aktarma, belge, fiş ve AI Usta uçlarında kullanıcı başına dakikalık sınır |

Sprint 3-6'da gelen `Evrak__*` ve `Plan__*` değişkenleri opsiyoneldir; ayarlanmazsa koddaki varsayilanlar (muayene 2/1 yıl, kış lastiği 15-11..15-04, uyarı 30 ve 7 gün, bireysel 3 / filo 25 araç, davet başına en fazla 3 ek araç) geçerlidir. Tam liste README'deki panel değişkenleri tablosundadır.

`Documents__StoragePath` neden önemli: değer boş bırakılırsa uygulama belgeleri **publish klasörünün içine**, `App_Data/documents` altına yazar. Publish klasörü MSDeploy'un senkron hedefidir; oraya yazılan kullanıcı verisi her yayında risk altındadır (bkz. bölüm 3).

Göreli değerler uygulama klasörüne (`AppContext.BaseDirectory`) göre çözülür, çalışma dizinine göre değil; sonuç her ortamda aynıdır. Uygulama `wwwroot`'a publish edildiği için `..\private\documents` değeri bir üste, `wwwroot`'un yanındaki `private\documents` klasörüne gider — yani publish hedefinin dışına. Mutlak bir yol verirsen aynen kullanılır.

MonsterASP hesap kökünde hazır bir `private` sistem klasörü sunuyor; bu klasör publish kapsamının dışındadır, yayın onu görmez ve silmez. Belgeler `private/documents` altında toplanır; alt klasörü uygulama ilk yüklemede kendisi açar.

İlk yayında bölüm 4'teki belge yükleme adımıyla yazma iznini teyit et. Yükleme hata verirse mutlak bir yola geç veya varsayılan `App_Data` yolunda kal; o durumda bölüm 3'teki koruma devrede.

Yol yanlış veya yazılamazsa belge yükleme çalışma anında hata verir; sessiz veri kaybı olmaz.

### Sprint REHBER ile gelen değişiklik

Yeni panel değişkeni **yok**. Dikkat edilecek iki nokta:

- **Migration**: `KayitKaynagi` (eklemeli, `Companies` tablosuna iki nullable kolon).
- **Build çıktısı**: `wwwroot/rehber/` (393 sayfa + `index.json`) ve `wwwroot/sitemap.xml` artık üretilen dosyalardır ve repoda yoktur. Publish bunları `UretilenleriYayinaEkle` hedefiyle taşır; `SkipExtraFilesOnServer=true` ile yayınlarken sunucuda eski sayfa kalabilir, bu zararsızdır — kayıt silindiğinde sayfası sunucuda kalır ama sitemap'ten düşer. Kayıt silinip sayfası da kalkacaksa o publish'i `SkipExtraFilesOnServer` olmadan yapın.
- Yayın sonrası `https://garajim.runasp.net/rehber/`, `/sitemap.xml` ve örnek bir konu sayfası 200 dönmelidir.

## 3. Publish (IISProfile)


Profil bugün `SkipExtraFilesOnServer=false` ile çalışıyor; yani **"hedefteki fazla dosyaları sil" açık**. Bu ayarla MSDeploy, pakette olmayan her şeyi sunucudan siler — yüklenmiş belgeler dahil.

Bunun için `Garajim.API.csproj` içine iki MSDeploy skip kuralı eklendi (`AppDataKlasoruSilinmesin`, `AppDataDosyalariSilinmesin`). Kurallar `App_Data` klasörünü senkron dışında bırakır. Kurallar publish profilinde değil csproj'da durur; çünkü `Properties/PublishProfiles/` gitignore'dadır (Web Deploy parolası içerir) ve profil yeniden oluşturulduğunda kaybolur.

**Kuralların sınırı:** yerelde klasörden klasöre MSDeploy senkronuyla doğrulandı (kuralsız senkron belgeyi sildi, kurallı senkron 0 silmeyle bıraktı). Uzak WMSVC yayınında aynı davranışı bu oturumda **doğrulayamadım**. Bu yüzden ilk yayında şu sırayı izle:

1. Yayını yap.
2. Bir belge yükle, indiğini gör.
3. **İkinci bir yayın daha yap** (kod değişmeden).
4. Aynı belgeyi tekrar indir. Geliyorsa kural uzakta da çalışıyor demektir.

Belge ikinci yayından sonra kayıpsa: `Documents__StoragePath`'i site kökü dışına almak zorunludur, skip kuralına güvenilemez.

## 4. Yayın sonrası doğrulama (bu sırayla)

1. `https://<site>/swagger/v1/swagger.json` → **404** dönüyor mu? Üretimde Swagger kapalıdır; 200 dönüyorsa `Swagger__Enabled` açık kalmış demektir ve tüm API yüzeyi yayında. Şemayı görmek gerekirse değişkeni geçici olarak açın, iş bitince kapatın.
2. Demo veya gerçek bir **Owner** hesabıyla giriş: cevapta `role` ve `companyName` geliyor mu?
3. **Driver** hesabıyla giriş: yalnızca zimmetli aracı görüyor mu? Zimmetsiz bir araç kimliğine `GET /api/Vehicles/{id}` → **404** mü?
4. Bir bakım kaydına **belge yükle**, sonra **indir**. Dosya doğru geliyor mu?
5. **Eski oturumlar**: yayından önce açık kalmış bir tarayıcı sekmesini yenile. Rol taşımayan oturum giriş ekranına düşmeli. Düşmüyorsa kullanıcıya çıkış-giriş yaptır.
6. **Anonim uçlar**: bir karne bağlantısı ve bir takvim aboneliği oluştur; `GET /api/karne/{token}` ve `GET /api/takvim/{token}.ics` girişsiz **200** dönüyor mu, ICS `text/calendar` mi? Bu iki bağlantı tam URL içermiyorsa `App__BaseUrl` ayarlanmamıştır.
7. **Plan limiti**: bireysel bir şirkette limit üstü araç eklemeyi dene; **402** dönmeli.
8. **AI Usta**: Hangfire panelinde `usta-cozum-ozeti` (04:00) ve `usta-saklama` (05:00) işleri kayıtlı mı? `/sartlar.html` girişsiz açılıyor mu? Onaysiz `POST /api/Usta/sohbet` **403** ve `ONAY_GEREKLI` dönüyor mu? Onaydan sonra bir soru sorup kademeli yanıt geldiğini ve `Usta__SahteYanit` değerinin **ayarlı olmadığını** doğrula.

9. **Hasar dosyası**: bir araca hasar dosyası aç, bir fotoğraf ekle, `GET /api/Hasar/{id}/tutanak.html` yazdırılabilir sayfayı **200** ve `text/html` olarak dönüyor mu? Dosyayı sil ve fotoğrafın belgesinin de silindiğini (`GET /api/Documents/{id}/download` → **404**) doğrula.
10. **Değer tahmini**: kasa tipi seçili, kapsam içi bir araçta `POST /api/Vehicles/{id}/deger/tahmin` **200** ve uyarı metni dönüyor mu? Kasa tipi boş araçta **422** ve "kasa tipini seçin" dönüyor mu? Kapsam dışı bir model adı taşıyan araçta **422** dönüyor mu? Aynı araçta dördüncü tahmin **400** ile reddediliyor mu?
11. **Tanıtım ve demo**: çıkış yapıp `https://<site>` açıldığında tanıtım bölümü ve altı özellik kartı görünüyor mu? `DemoSeed__Enabled` açıksa **Demo ile dene** düğmesi giriş yapıyor mu? Kapalıysa hata yerine "kendi hesabınızı açın" yönlendirmesi mi veriyor?
12. **Çevrimdışı**: mobil tarayıcıda siteyi açıp uçak moduna al; **Kaza anı** düğmesi rehberi gösteriyor mu, "Hasar dosyası aç" kuyruğa alındığını söylüyor mu? Bağlantı geri gelince dosya listede beliriyor mu?
13. **E-posta doğrulama** (SMTP açıldıktan sonra, gerçek bir adresle): kayıt ol, kod e-postası **gerçekten geldi mi** ve spam'e mi düştü? Kodu gir, uygulamaya giriliyor mu? Doğrulamadan giriş denemesi 403 verip doğrulama ekranına mı düşüyor? Eski bir kullanıcıyla giriş **kod istemeden** çalışıyor mu (migration doğru çalıştıysa çalışmalı)?
14. **Şifremi unuttum**: giriş ekranından kod iste. Kayıtlı **ve** kayıtsız bir adres için **aynı metnin** döndüğünü gör; kayıtlı adrese gelen kodla şifreyi değiştir, eski şifreyle girişin **401** verdiğini doğrula. Yanıtta token dönmemeli, kullanıcı yeniden giriş yapmalı.
15. **Şifre değiştir**: Ayarlar'dan şifreyi değiştir. Yanlış mevcut şifre **400** vermeli; doğrusundan sonra başka bir cihazda açık kalan oturum bir sonraki istekte **401** almalı.
16. **Plaka kuralı**: `34ABCD12` gibi kural dışı bir plakayla araç eklemeyi dene; **400** dönmeli. Aynı plakayı "yabancı plaka" işaretiyle ekleyince kabul edilmeli.
17. **Arşiv**: bir aracı arşivle; aktif listeden düşmeli, plan limiti bir azalmalı, arşivli araca masraf eklemek **409** dönmeli, paylaşılmış karne bağlantısı hâlâ **200** vermeli.
18. **Hangfire panosu**: `demo-sifirlama` (03:30), `hesap-silme` (03:00), `usta-cozum-ozeti` (04:00), `fis-temizleme` (04:30), `usta-saklama` (05:00) ve `reminder-notifications` (06:00) kayıtlı mı ve **Türkiye saatinde** mi görünüyor?
19. **Sürüm şeridi**: yayından sonra açık kalmış bir sekmede en fazla 15 dakika içinde "Yeni sürüm var" şeridi çıkmalı; `curl -I https://<site>/` yanıtında `X-App-Version` bulunmalı.

20. **Onboarding**: yeni bir hesapla gir — üstte üç adımlı kurulum çubuğu ve ürün turu açılıyor mu? Turu Esc ile kapat, Ayarlar → "Turu tekrar göster" yeniden açıyor mu? "Örnek araçla dene" düğmesi altı aylık kayıtlarla dolu aracı oluşturuyor mu, Ayarlar'dan temizce siliniyor mu?
21. **Yardım ve geri bildirim**: `https://<site>/yardim.html` girişsiz açılıyor, arama süzüyor ve destek adresi görünüyor mu? Uygulamada sağ alttaki "Geri bildirim" düğmesinden mesaj gönder — destek kutusuna e-posta düştü mü?
22. **Yönetim**: `App__YoneticiEposta` girildikten sonra o hesapla `https://<site>/yonetim.html` açılıyor mu? Başka bir hesapla 403 mü dönüyor?
23. **SEO ve yenilikler**: `https://<site>/robots.txt`, `https://<site>/sitemap.xml` ve `https://<site>/yenilikler.html` 200 dönüyor mu? Ana sayfanın kaynağındaki `og:image` adresi (`/img/og.png`) 200 mü?

Adım 1 veya 2 başarısızsa devam etme, bölüm 5'e geç.

## 5. Geri dönüş planı

Sorun kodda ise (uygulama açılıyor, davranış yanlış):

1. Bir önceki commit'i publish et. Veritabanı şeması geriye uyumlu kalır.

Sorun veritabanındaysa (migration hatalı uygulandı, veri bozuldu):

1. Siteyi panelden durdur, yazma trafiğini kes.
2. `App_Data/documents` klasörünü indir (o anki belgeler yedekte yok).
3. Panelden **bölüm 1'de indirdiğin yedeği** geri yükle. `Down` migration'ı **çalıştırma** — veri kaybettirir.
4. Bir önceki commit'i publish et.
5. Belgeleri geri yükle.
6. Adım 4'teki doğrulama sırasını tekrarla.

`PlakaSirketBazindaTekil` migration'ı, aynı şirkette mükerrer plaka bulursa çakışan `şirketId/plaka` çiftlerini yazıp Türkçe hatayla durur ve işlemi geri sarar; kısmi uygulama bırakmaz. Bu hatayı görürsen veritabanındaki plakaları düzeltmeden yayına devam etme.
