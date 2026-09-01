# Garajım

[![CI](https://github.com/Omer-F-Ylmz/Garajim/actions/workflows/ci.yml/badge.svg)](https://github.com/Omer-F-Ylmz/Garajim/actions/workflows/ci.yml)

Araç bakım ve masraf takip API'si. Araçlarınızın bakım, yakıt ve diğer masraflarını kaydeder; muayene, sigorta, kasko, MTV gibi tarihleri yaklaşınca e-posta ile hatırlatır; masraf raporları ve yakıt tüketim istatistikleri üretir.

## Canlı demo

**<https://garajim.runasp.net>** — Swagger: <https://garajim.runasp.net/swagger>

Hazır demo hesabıyla girebilirsiniz:

| | |
|---|---|
| E-posta | `demo@garajim.app` |
| Şifre | `Demo1234!` |

Hesapta bir araç (`34DEMO34`), iki bakım, üç yakıt, iki masraf kaydı ve yaklaşan bir muayene hatırlatması hazır gelir; raporlar ve fiyat tahmini ilk girişte doluyken görünür. Kendi hesabınızı açıp sıfırdan da deneyebilirsiniz.

### Bilinen sınırlar

Ücretsiz paylaşımlı hosting (MonsterASP.NET, 256 MB) üzerinde çalışıyor. Portföy demosu olduğu için aşağıdakiler bilinçli kabul edilmiş sınırlardır:

- **İlk açılışta gecikme.** Uygulama havuzu bir süre istek almazsa durduruluyor; sonraki ilk istek uygulamayı yeniden başlattığı için birkaç saniye sürebilir. Ayrıca fiyat tahmini modeli ilk tahmin isteğinde yükleniyor: o istek ölçümlerde ~385 ms, sonrakiler 1-2 ms.
- **Hatırlatma e-postaları uykuya bağlı.** Günlük Hangfire job'ı 06:00 için kayıtlı, ama uygulama havuzu o saatte uykudaysa job tetiklenmeyebilir. Bu, ücretsiz katmanın doğal sonucu; kodda buna karşı bir zorlama yapılmadı. Hatırlatmaların kendisi arayüzde her zaman görünür, yalnızca e-posta gönderimi garanti değildir.
- **SMTP kapalı.** `Smtp` ayarları boş bırakıldığı için e-posta gönderilmiyor; job çalışır ama gönderim adımını atlar. Demo hesabıyla e-posta beklemeyin.
- **Veri kalıcı ama demo.** Demo hesabına eklediğiniz kayıtlar veritabanında kalır; başkaları da aynı hesabı kullanabilir.

## Teknolojiler

- ASP.NET Core 8 Web API (katmanlı mimari: Core / Entity / Dal / Business / API / ML)
- Entity Framework Core 8 + SQL Server
- JWT ile kimlik doğrulama
- Hangfire ile zamanlanmış hatırlatma job'ı (her gün 06:00)
- SMTP ile e-posta bildirimi
- ML.NET FastTree regresyon ile ikinci el fiyat tahmini
- Swagger

## Kurulum

1. Gereksinimler: .NET 8 SDK ve SQL Server (LocalDB yeterli).
2. Geliştirme değerleri `Garajim.API/appsettings.Development.json` dosyasındadır ve varsayılan olarak LocalDB ile çalışır; başka bir sunucu kullanacaksanız `ConnectionStrings:Default` değerini orada düzenleyin. `appsettings.json` içindeki aynı alanlar bilerek boştur — üretimde ortam değişkeninden gelirler.
3. EF aracını kurun (bir kez):
   ```
   dotnet tool install --global dotnet-ef
   ```
4. Veritabanını oluşturun:
   ```
   dotnet ef migrations add InitialCreate -p Garajim.Dal -s Garajim.API
   dotnet ef database update -p Garajim.Dal -s Garajim.API
   ```
5. Çalıştırın:
   ```
   dotnet run --project Garajim.API
   ```
6. Arayüz: https://localhost:7200 — Swagger: https://localhost:7200/swagger — Hangfire paneli: https://localhost:7200/hangfire

Swagger **her ortamda** açıktır (canlı demo için bilinçli tercih). Hangfire paneli **yalnızca `Development` ortamında** map'lenir; `Production`'da `/hangfire` adresi 404 döner.

## Docker

Çok aşamalı `Dockerfile`, API'yi `sdk:8.0` imajında publish edip `aspnet:8.0` imajında 8080 portunda çalıştırır.

```
docker build -t garajim-api .
```

Bağlantı cümlesi ve diğer gizli değerler ortam değişkeniyle verilir (bkz. [Güvenlik](#güvenlik)):

```
docker run -d -p 8080:8080 \
  -e "ConnectionStrings__Default=Server=host.docker.internal,1433;Database=GarajimDb;User Id=sa;Password=Guclu_Parola1;TrustServerCertificate=True" \
  -e "Jwt__Key=en-az-32-karakterlik-rastgele-uretilmis-anahtar" \
  --name garajim-api garajim-api
```

Dört not:

- Konteyner `Production` ortamında başlar. Kök adresteki web arayüzü ve <http://localhost:8080/swagger> her ortamda çalışır; Hangfire paneli yalnızca `Development`'ta açılır.
- `Production`'da `ConnectionStrings__Default` ve `Jwt__Key` zorunludur: boş, yer tutucu ya da LocalDB'yi gösteren bir bağlantı cümlesinde uygulama hangi değişkenin eksik olduğunu söyleyip başlamayı reddeder.
- LocalDB konteynerden erişilemez; gerçek bir SQL Server örneği (örneğin `mcr.microsoft.com/mssql/server:2022-latest`) kullanın.
- Şema kurulumu için iki yol var: `dotnet ef database update` komutunu hedef sunucuya karşı bir kez çalıştırmak ya da aşağıdaki açılışta migration seçeneği.

### Açılışta migration

Paylaşımlı hosting gibi CLI erişiminin olmadığı ortamlarda uygulama açılışta `Database.Migrate()` çalıştırabilir. Varsayılan **kapalıdır**; açmak için:

```
docker run -d -p 8080:8080 \
  -e "ApplyMigrationsAtStartup=true" \
  -e "ConnectionStrings__Default=Server=sunucu,1433;Database=GarajimDb;User Id=garajim;Password=...;TrustServerCertificate=True" \
  -e "Jwt__Key=en-az-32-karakterlik-rastgele-uretilmis-anahtar" \
  --name garajim-api garajim-api
```

Bayrak açıkken bekleyen tüm migration'lar uygulanır, veritabanı yoksa oluşturulur. Birden fazla örnek aynı anda açılıyorsa migration'ı tek örnekte çalıştırmak daha güvenlidir; şema kurulduktan sonra bayrağı kapatmanız önerilir.

## Yayına alma (IIS / MonsterASP.NET)

Yayın çıktısı, hosting modeli **InProcess**, platform **x64**, framework-dependent (sunucuda .NET 8 kurulu):

```
dotnet publish Garajim.API -c Release -r win-x64 -o publish
```

Hedef platform bilinçli olarak csproj'da değil yayın komutunda belirtilir; proje dosyası platformdan bağımsızdır ve aynı proje `-r linux-x64` ile konteyner için de yayınlanabilir (`Dockerfile` bunu yapar). Hosting modeli `InProcess` csproj'da sabittir, üretilen `web.config` bu değerle çıkar. `publish` klasörünün içeriği site kök dizinine kopyalanır.

Sunucuda tanımlanması gereken ortam değişkenleri:

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=<uzak MSSQL bağlantı cümlesi>
Jwt__Key=<en az 32 baytlık rastgele anahtar>
ApplyMigrationsAtStartup=true
DemoSeed__Enabled=true
```

Bağlantı cümlesi anahtarının adı **`Default`**'tur (`DefaultConnection` değil). `Production`'da bu iki değer boş, yer tutucu ya da LocalDB'yi gösteriyorsa uygulama başlamaz ve hangi değişkenin eksik olduğunu log'a yazar.

`ApplyMigrationsAtStartup=true` iken uygulama açılışta bekleyen migration'ları uygular; boş veritabanına şema kurulumu için CLI erişimi gerekmez. Hangfire kendi tablolarını ilk açılışta zaten oluşturur. `DemoSeed__Enabled=true` demo hesabını bir kez oluşturur, ikinci açılışta hiçbir şey eklemez.

## Kullanım

1. `POST /api/auth/register` ile kayıt olun, dönen token'ı kopyalayın.
2. Swagger'da sağ üstteki **Authorize** butonuna token'ı yapıştırın.
3. `POST /api/vehicles` ile araç ekleyin, ardından bakım / yakıt / masraf / hatırlatma uçlarını kullanın.

E-posta bildirimlerinin gerçekten gönderilmesi için `appsettings.json` içindeki `Smtp` alanlarını doldurun (Gmail için uygulama şifresi gerekir). Boş bırakılırsa job çalışır ama e-posta göndermeden geçer.

## Güvenlik

`appsettings.json` içindeki `ConnectionStrings:Default` ve `Jwt:Key` alanları **boştur**; üretimde ortam değişkeninden gelmeleri beklenir. `appsettings.Development.json` ve `Smtp` alanlarındaki değerler **yalnızca geliştirme ortamı içindir**; repoda açık durdukları için gizli kabul edilmezler.

`Production` ortamında bağlantı cümlesi veya imzalama anahtarı boş, yer tutucu ya da LocalDB'yi gösteriyorsa uygulama sessizce bir varsayılana düşmez; hangi ortam değişkeninin eksik olduğunu söyleyip başlamayı reddeder.

Canlı ortamda bu değerleri ortam değişkeniyle geçin — ASP.NET Core'da iç içe anahtarlar `__` (iki alt çizgi) ile ayrılır:

```
Jwt__Key=<en az 32 karakterlik, rastgele üretilmiş anahtar>
Smtp__Host=smtp.ornek.com
Smtp__User=...
Smtp__Password=...
Smtp__From=...
```

Ortam değişkenleri `appsettings.json` içindeki değerlerin üzerine yazar, dosyayı düzenlemeniz gerekmez. Prod'a çıkarken `Jwt__Key` mutlaka yeni ve rastgele bir değerle verilmelidir; varsayılan anahtarla üretilen token'lar herkes tarafından taklit edilebilir.

### Panel değişkenleri

| Değişken | Durum | Varsayılan | Not |
|---|---|---|---|
| `ConnectionStrings__Default` | Zorunlu | — | Uzak MSSQL. LocalDB içerirse uygulama üretimde başlamayı reddeder |
| `Jwt__Key` | Zorunlu | — | En az 32 karakter, rastgele |
| `Documents__StoragePath` | Zorunlu sayılır | `App_Data/documents` (publish klasörünün içi) | `..\private\documents` önerilir; varsayılan yol publish hedefinin içindedir |
| `App__BaseUrl` | Karne için zorunlu | boş | Boşsa karne bağlantısı göreli üretilir ve paylaşılamaz; hatırlatma e-postasındaki link de düşer |
| `Receipts__ApiKey` | Fiş okuma için zorunlu | boş | Boşsa akış çalışır ama her fiş boş taslak ve sıfır güvenle döner |
| `Receipts__Provider` | Opsiyonel | `Gemini` | `Gemini` veya `OpenAI` |
| `Receipts__Model` | Opsiyonel | `gemini-2.5-flash` / `gpt-4.1-nano` | Sağlayıcıya göre |
| `Receipts__AylikLimit` | Opsiyonel | `100` | Şirket başına aylık çıkarım çağrısı |
| `Receipts__OtoOnayGuven` | Opsiyonel | `0.85` | Otomatik onay güven eşiği (0-1) |
| `Documents__MaxFileSizeBytes` | Opsiyonel | `5242880` | Dosya başına sınır |
| `Documents__CompanyQuotaBytes` | Opsiyonel | `262144000` | Şirket başına toplam belge alanı |
| `Smtp__Host` `Smtp__Port` `Smtp__User` `Smtp__Pass` `Smtp__From` | Opsiyonel | boş | Eksikse gönderim loglanıp atlanır, uygulama çökmez |
| `DemoSeed__Enabled` | Opsiyonel | `false` | Açıkken eksik demo verisi tamamlanır, mevcut veriye dokunulmaz |
| `ApplyMigrationsAtStartup` | Opsiyonel | `false` | Açıkken açılışta migration uygular |

## Fişten otomatik kayıt

Fişin fotoğrafını yükleyin; alanlar bir görüntü modeli tarafından okunup taslak olarak döner, siz kontrol edip onaylayınca Yakıt / Bakım / Masraf kaydı açılır ve fiş o kayda belge olarak bağlanır.

- **Tek fiş**: `POST /api/receipts` (multipart). Uzantı beyaz listesi, magic-byte doğrulaması ve 5 MB sınırı belge yüklemeyle aynıdır.
- **Toplu yükleme**: arayüzde birden fazla dosya seçilir, sırayla gönderilir (paralel değil), ilerleme `7/30` biçiminde yazılır ve hatalı dosya zinciri durdurmaz. Bitişte onaylandı / bekliyor / hata özeti çıkar.
- **Koşullu otomatik onay**: `POST /api/receipts?otoOnay=true` üç şart birden sağlanırsa yüklemeyle onayı aynı transaction'da bitirir — okuma güveni `Receipts__OtoOnayGuven` eşiğinde (varsayılan 0,85), tarih + tutar + tür dolu, fişteki plaka erişilebilir tek araca eşleşiyor. Biri eksikse taslak `Bekliyor` kalır ve `atlamaNedeni` Türkçe döner.
- **Maliyet koruması**: şirket başına aylık çağrı `Receipts__AylikLimit` ile sınırlıdır (varsayılan 100), aşımda 429 döner.
- **Ölçüm**: `GET /api/receipts/stats` (yalnız Owner) toplam çağrı, onay/red oranı, oto onaylanan sayısı, alan doluluk yüzdeleri ve alan bazında düzeltme oranını verir. Düzeltme oranı yalnız elle onaylananlardan hesaplanır.

Sağlayıcı `Receipts__Provider` ile seçilir: `Gemini` (varsayılan) veya `OpenAI`. Anahtar yoksa uygulama çalışmayı sürdürür, çıkarım boş sonuç ve sıfır güvenle döner.

## Parça hafızası

Bakım kaydına parça satırları eklenebilir (tür, açıklama, adet, tutar, marka); fişten okunan satır kalemleri de deterministik bir eşleştiriciyle parça türüne çevrilir, işçilik ve kargo satırları atlanır.

`GET /api/vehicles/{id}/parca-hafizasi` tür başına son değişim tarihi ve kilometresi, değişim sayısı, toplam tutar ve bir sonraki tahmini değişimi döner. Durum Türkiye servis pratiğine göre tanımlı aralık kataloğundan hesaplanır: **Iyi**, **Yaklasiyor** (kalan ≤ aralığın %10'u ya da ≤ 30 gün), **Gecti**. `POST /api/vehicles/{id}/parca-hafizasi/{parcaTuru}/hatirlatma` tahminden hatırlatma açar.

## Araç karnesi

Aracın belgeli geçmişi tek bağlantıyla paylaşılır — satışta alıcıya gösterilecek karne.

- `POST /api/vehicles/{id}/karne` (Owner / Manager) kapsam ve isteğe bağlı süre alıp bağlantı üretir. Araç başına tek aktif bağlantı vardır; yenisi eskisini pasifleştirir.
- Kapsam bayrakları bağımsızdır: bakım geçmişi, parça hafızası, yakıt özeti, belgeler, plaka gösterimi, tutar gösterimi. Plaka kapalıysa `34 *** 217` biçiminde maskelenir; tutar kapalıysa bakım tutarları, bakım toplamı ve parça toplamları yanıta hiç girmez.
- `GET /api/karne/{token}` giriş istemez. Kayıt yok, pasif ya da süresi dolmuşsa hepsi aynı 404'ü döndürür. Uç IP başına dakikada 30 istekle sınırlıdır.
- Token'ın yalnız SHA-256 özeti saklanır; ham değer sadece oluşturma yanıtındaki bağlantıda görünür.
- `karne.html` ana uygulamadan bağımsız çalışır, yazdırma düzeni taşır ve QR kodu istemcide üretilir (dış servise istek gitmez). Bu sayfa service worker önbelleğine alınmaz.
- `GET /api/vehicles/karne-stats` (Owner) araç sayısı, karnesi aktif araç oranı ve toplam görüntülenmeyi verir.

## Kalibrasyon aracı

`tools/Garajim.Calibration`, fiş çıkarımının gerçek doğruluğunu bir cevap anahtarına karşı ölçer.

Klasörde fiş görüntüleri ve `cevap-anahtari.csv` bulunur (noktalı virgül ayraç, UTF-8 BOM, başlık `dosya;zorluk;tur;tarih;tutar;km;plaka;litre;aciklama`; tarih `gg.AA.yyyy`, sayı `1.484,36`, boş hücre = değer yok).

Kimlik yalnız ortam değişkeninden okunur, argümanla şifre verilmez:

```powershell
$env:GARAJIM_URL="https://garajim.runasp.net"; $env:GARAJIM_EMAIL="demo@garajim.app"; $env:GARAJIM_PASS="<sifre>"
```

```
dotnet run --project tools/Garajim.Calibration -- --dir <klasör>
```

Her dosya yüklenir, taslak cevap anahtarıyla alan alan karşılaştırılır (tutar ve litre ±0,01, km tam, plaka boşluk ve büyük-küçük duyarsız), sonra doğru değerlerle onaylanır — böylece sunucudaki düzeltme oranı gerçek hatayı yansıtır. Aylık limite takılırsa durur ve o ana kadarki raporu üretir. Rapor konsola ve `--dir` içine `kalibrasyon-<tarih>.md` olarak yazılır; bu dosya gitignore'dadır.

## Uç Noktalar

- `POST /api/auth/register`, `POST /api/auth/login`
- `GET|POST /api/vehicles`, `GET|PUT|DELETE /api/vehicles/{id}`
- `GET /api/maintenance?vehicleId=`, `POST /api/maintenance`, `PUT /api/maintenance/{id}`, `DELETE /api/maintenance/{id}`
- `GET /api/fuel?vehicleId=`, `POST /api/fuel`, `DELETE /api/fuel/{id}`
- `GET /api/expenses?vehicleId=`, `POST /api/expenses`, `DELETE /api/expenses/{id}`
- `GET /api/reminders?vehicleId=`, `GET /api/reminders/upcoming?days=30`, `POST /api/reminders`, `PUT /api/reminders/{id}/complete`, `DELETE /api/reminders/{id}`
- `GET /api/reports/summary?vehicleId=&start=&end=`, `GET /api/reports/monthly?vehicleId=`, `GET /api/reports/fuel-stats?vehicleId=`
- `POST /api/receipts?otoOnay=`, `GET /api/receipts?durum=`, `GET /api/receipts/{id}`, `POST /api/receipts/{id}/confirm`, `POST /api/receipts/{id}/reject`, `GET /api/receipts/stats`
- `GET /api/vehicles/{id}/parca-hafizasi`, `POST /api/vehicles/{id}/parca-hafizasi/{parcaTuru}/hatirlatma`
- `POST|DELETE /api/vehicles/{id}/karne`, `GET /api/vehicles/karne-stats`, `GET /api/karne/{token}`, `GET /api/karne/{token}/belge/{documentId}`
- `GET|POST /api/documents`, `GET /api/documents/{id}/download`, `DELETE /api/documents/{id}`
- `GET|POST /api/team`, `PUT /api/team/{id}/role`, `PUT /api/team/{id}/deactivate`
- `GET|POST /api/assignments`, `PUT /api/assignments/transfer`, `PUT /api/assignments/end`
- `POST /api/price/estimate`

## Fiyat Tahmini (ML.NET)

`Garajim.ML` katmanı, Türkiye ikinci el ilan verisiyle eğitilen bir FastTree regresyon modeliyle araç bilgisinden tahmini satış fiyatı üretir. `Garajim.ML.Trainer` konsol projesi eğitimi çalıştırır, API modeli `PredictionEnginePool` ile servis eder.

### Veriyi hazırlama

Eğitim verisi repoda tutulmaz (`Garajim.ML/Data/*.csv` git dışıdır). Modeli yeniden eğitmek için:

1. Veri setini indirin: <https://www.kaggle.com/datasets/oguzarar/turkey-used-car-prices-august-2025>
2. İnen `cars.csv` dosyasını `Garajim.ML/Data/` klasörüne koyun.
3. Eğitimi çalıştırın:
   ```
   dotnet run -c Release --project Garajim.ML.Trainer
   ```

Eğitim, modeli `Garajim.API/MLModels/price-model.zip` olarak yazar; API açılışta bu dosyayı okur.

### Veri temizliği

- `fiyat` (`1.169.000 TL`) ve `kilometre` (`124.000 km`) kolonları metinden sayıya çevrilir
- Kopya satırlar atılır
- Fiyatı 100.000 TL altındaki / 50.000.000 TL üstündeki, kilometresi 2.000.000'un üstündeki, yılı 1990'dan eski ilanlar elenir

52.256 ilanın 51.311'i eğitime kalır: 73 eksik/bozuk, 610 aralık dışı, 262 kopya satır elenir.

### Model

| | |
|---|---|
| Algoritma | FastTree regresyon (400 ağaç, 64 yaprak) |
| Hedef | `log(fiyat)` — tahmin `exp` ile TL'ye geri çevrilir |
| Özellikler | marka, seri, yıl, kilometre, yakıt tipi, vites tipi, kasa tipi |
| Kategorik kodlama | OneHotEncoding (59 marka, 423 seri) |
| Ayrım | %80 eğitim (41.101 satır) / %20 test (10.210 satır) |

Fiyat dağılımı sağa çok çarpık olduğu için model doğrudan TL yerine `log(fiyat)` üzerinde eğitilir. Aşağıdaki iki kolon, aynı veri ve aynı ayrım üzerinde yalnızca hedef kolonu değiştirilerek eğitilmiş iki modelin test seti sonuçlarıdır (`Garajim.ML.Trainer` her çalıştığında ikisini de eğitip karşılaştırır, kaydettiği model `log(fiyat)` hedefli olandır):

| Metrik | hedef = fiyat (TL) | hedef = log(fiyat) |
|---|---|---|
| R² (hedefin kendi ölçeğinde) | 0,6534 | 0,9444 |
| MAE | 95.762 TL | **81.941 TL** |
| RMSE | 460.661 TL | **450.541 TL** |

İki R² değeri farklı ölçeklerde olduğu için doğrudan kıyaslanamaz; kıyaslanabilir olanlar TL'ye geri çevrilmiş MAE ve RMSE. Log hedefi tipik hatayı %14,4 düşürüyor (95.762 → 81.941 TL), RMSE'deki iyileşme ise %2,2'de kalıyor: karesel hata hâlâ veri setindeki az sayıdaki çok yüksek fiyatlı ilan tarafından belirleniyor.

Aynı iki örnek için iki modelin tahmini:

| Araç | hedef = fiyat (TL) | hedef = log(fiyat) |
|---|---|---|
| 2018 Renault Clio, 120.000 km, Benzin, Düz, Hatchback/5 | 704.170 TL | 684.420 TL |
| 2015 Volkswagen Passat, 200.000 km, Dizel, Otomatik, Sedan | 1.325.954 TL | 1.256.138 TL |

Tahmin log-uzayında yapılıp geri çevrildiği için medyan piyasa fiyatına yakındır; bu, uç ilanlara dirençli olması için bilinçli bir tercihtir.

### Kullanım

`POST /api/price/estimate` — token gerektirir.

```json
{
  "marka": "Renault",
  "seri": "Clio",
  "yil": 2018,
  "kilometre": 120000,
  "yakitTipi": "Benzin",
  "vitesTipi": "Düz",
  "kasaTipi": "Hatchback/5"
}
```

Yanıt:

```json
{
  "data": {
    "tahminiFiyat": 684420,
    "paraBirimi": "TL",
    "marka": "Renault",
    "seri": "Clio",
    "yil": 2018,
    "kilometre": 120000
  },
  "success": true,
  "message": "Fiyat tahmini üretildi."
}
```

Alan değerleri veri setindeki yazımla aynı olmalıdır:

- **yakitTipi:** `Benzin`, `Dizel`, `LPG & Benzin`, `Hibrit`, `Elektrik`
- **vitesTipi:** `Düz`, `Otomatik`, `Yarı Otomatik`
- **kasaTipi:** `Sedan`, `Hatchback/5`, `Hatchback/3`, `Station wagon`, `MPV`, `Coupe`, `SUV`, `Cabrio`, `Roadster`

## Yol Haritası

- [x] Aşama 1: Katmanlı API iskeleti — auth, araç, bakım/yakıt/masraf, hatırlatma, rapor, Hangfire job
- [x] Aşama 2: ML.NET ile ikinci el fiyat tahmin modülü — FastTree regresyon, `POST /api/price/estimate`
- [x] Aşama 3: xUnit testleri — birim (Moq) + entegrasyon (SQLite in-memory), 63 test
- [x] Aşama 4: Docker + GitHub Actions CI
- [x] Aşama 4b: wwwroot altında tek sayfalık web arayüzü (framework yok, koyu tema)
- [x] Aşama 5: Canlıya çıkış — MonsterASP.NET, <https://garajim.runasp.net>
