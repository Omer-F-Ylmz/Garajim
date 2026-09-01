# CLAUDE.md

## Proje

Garajım, araç bakım ve masraf takibi yapan ASP.NET Core 8 Web API'sidir. Çözüm yedi projeden oluşur: `Garajim.Core` (Result tipleri, generic EF repository, JWT ve hash yardımcıları), `Garajim.Entity` (entity / DTO / enum), `Garajim.Dal` (`GarajimDbContext`, migration'lar, `Ef*Dal` sınıfları), `Garajim.Business` (Manager'lar, `Messages`, Hangfire job'ı), `Garajim.API` (controller'lar, `Program.cs`, Swagger) ve ikinci el fiyat tahmini için `Garajim.ML` + `Garajim.ML.Trainer`.
Kimlik doğrulama JWT ile yapılır; controller'lar `SecureControllerBase`'ten türeyip kullanıcıyı `CurrentUserId` üzerinden alır. Veri SQL Server (LocalDB) üzerinde EF Core 8 ile tutulur, hatırlatma e-postaları Hangfire recurring job'ı ile günlük gönderilir.
Fiyat tahmini tarafında `Garajim.ML.Trainer` modeli eğitip `Garajim.API/MLModels/price-model.zip` dosyasına yazar, API bu modeli `PredictionEnginePool` ile servis eder; eğitim verisi (`Garajim.ML/Data/*.csv`) repoya dahil değildir.

## Katmanlar

Katmanlar: Core → Entity → Dal → Business → API. Entity'ler flat, navigation property yok. Manager'lar IResult/IDataResult döner; kullanıcı mesajları Constants/Messages.cs'te; sahiplik kontrolü her zaman JWT'deki userId ile yapılır.

## Kurallar

Kodda yorum satırı yazma. Her görevin sonunda dotnet build al, varsa testleri çalıştır, anlamlı Türkçe commit at. dotnet build almadan önce çalışan API sürecini durdur.

### Kiracı izolasyonu — SystemScope tek kapıdır

Tenant filtresini aşmanın tek yolu `SystemScope.For(tenantContext, companyId)`'dir; blok bitince önceki bağlam geri gelir. Yeni kodda `IgnoreQueryFilters()` **yasaktır**.

Mevcut iki istisna korunur, üçüncüsü eklenmez:

- `EfUserDal.GetForAuthenticationAsync` — giriş öncesi kullanıcı arama
- `EfUserDal.ExistsForRegistrationAsync` — kayıt sırasında e-posta tekilliği

`EfKarnePaylasimiDal` token aramasında filtreyi atlar; bu bilinçlidir çünkü anonim istekte tenant bağlamı yoktur ve arama tek satırı token özetinden bulur, sonrası `SystemScope` içinde okunur.

### Migration yalnız eklemelidir

Canlı veritabanı doludur. `Up()` içinde yalnız `AddColumn`, `CreateTable`, `CreateIndex` bulunur; `DropColumn`, `DropTable`, `AlterColumn` ve `RenameColumn` kullanılmaz. Kolon daraltma ya da tip değiştirme gerekiyorsa yeni kolon açılır, veri taşınır, eski kolon bir sonraki sürümde ele alınır. Migration ekledikten sonra `Up()` içeriğini doğrula.

### Service worker kabuk listesi

`wwwroot/sw.js` içindeki `KABUK_DOSYALARI` yalnız uygulama kabuğunu tutar: `/`, `/index.html`, `/styles.css`, `/app.js`, `/garajim-logo.svg`, `/garajim-icon-180.png`, `/garajim-icon-512.png`, `/manifest.json`.

`karne.html`, `karne.js`, `karne.css` ve `/api/karne/*` **önbelleğe girmez** — `fetch` işleyicisi `/karne` ile başlayan yolları doğrudan ağa geçirir. Karne anonim ve anlık veri gösterir; bayat kopya paylaşılan araç hakkında yanlış bilgi verir.

### Otomatik onay üç şartı

`POST /api/Receipts?otoOnay=true` yalnız üçü birden sağlanırsa aynı transaction'da onaylar:

1. `GuvenSkoru >= Receipts__OtoOnayGuven` (varsayılan 0,85)
2. `Tarih`, `ToplamTutar` ve `TahminiTur` dolu (`TahminiTur != Bilinmiyor`)
3. Fişteki plaka şirkette erişilebilir tek araca eşleşiyor (Driver için yalnız aktif zimmetli araç)

Biri eksikse taslak `Bekliyor` kalır ve `atlamaNedeni` Türkçe döner. Oto onaylı taslakta `DuzeltilenAlanlar` boş bırakılır; doğruluk ölçümü yalnız elle onaylananlardan hesaplanır.

## Komutlar

Çalıştır:

```
dotnet run --project Garajim.API
```

Migration:

```
dotnet ef migrations add <Ad> -p Garajim.Dal -s Garajim.API
```

Kalibrasyon (fiş çıkarım doğruluğu; kimlik yalnız ortam değişkeninden):

```
dotnet run --project tools/Garajim.Calibration -- --dir <klasör>
```

Klasörde fiş görüntüleri ve `cevap-anahtari.csv` bulunur. `GARAJIM_URL`, `GARAJIM_EMAIL` ve `GARAJIM_PASS` ortam değişkenleri zorunludur; şifre argümanla verilmez, repoya yazılmaz. Rapor konsola ve `--dir` içine `kalibrasyon-<tarih>.md` olarak yazılır, bu dosya gitignore'dadır.
