# CLAUDE.md

## Proje

Garajım, araç bakım ve masraf takibi yapan ASP.NET Core 8 Web API'sidir. Çözüm yedi projeden oluşur: `Garajim.Core` (Result tipleri, generic EF repository, JWT ve hash yardımcıları), `Garajim.Entity` (entity / DTO / enum), `Garajim.Dal` (`GarajimDbContext`, migration'lar, `Ef*Dal` sınıfları), `Garajim.Business` (Manager'lar, `Messages`, Hangfire job'ı), `Garajim.API` (controller'lar, `Program.cs`, Swagger) ve ikinci el fiyat tahmini için `Garajim.ML` + `Garajim.ML.Trainer`.
Kimlik doğrulama JWT ile yapılır; controller'lar `SecureControllerBase`'ten türeyip kullanıcıyı `CurrentUserId` üzerinden alır. Veri SQL Server (LocalDB) üzerinde EF Core 8 ile tutulur, hatırlatma e-postaları Hangfire recurring job'ı ile günlük gönderilir.
Fiyat tahmini tarafında `Garajim.ML.Trainer` modeli eğitip `Garajim.API/MLModels/price-model.zip` dosyasına yazar, API bu modeli `PredictionEnginePool` ile servis eder; eğitim verisi (`Garajim.ML/Data/*.csv`) repoya dahil değildir.

## Katmanlar

Katmanlar: Core → Entity → Dal → Business → API. Entity'ler flat, navigation property yok. Manager'lar IResult/IDataResult döner; kullanıcı mesajları Constants/Messages.cs'te; sahiplik kontrolü her zaman JWT'deki userId ile yapılır.

## Kurallar

Kodda yorum satırı yazma. Her görevin sonunda dotnet build al, varsa testleri çalıştır, anlamlı Türkçe commit at. dotnet build almadan önce çalışan API sürecini durdur.

## Komutlar

Çalıştır:

```
dotnet run --project Garajim.API
```

Migration:

```
dotnet ef migrations add <Ad> -p Garajim.Dal -s Garajim.API
```
