# Garajım

Araç bakım ve masraf takip API'si. Araçlarınızın bakım, yakıt ve diğer masraflarını kaydeder; muayene, sigorta, kasko, MTV gibi tarihleri yaklaşınca e-posta ile hatırlatır; masraf raporları ve yakıt tüketim istatistikleri üretir.

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
2. `Garajim.API/appsettings.json` içindeki `ConnectionStrings:Default` değerini kendi ortamınıza göre düzenleyin (varsayılan LocalDB ile çalışır).
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
6. Swagger: https://localhost:7200/swagger — Hangfire paneli: https://localhost:7200/hangfire

## Kullanım

1. `POST /api/auth/register` ile kayıt olun, dönen token'ı kopyalayın.
2. Swagger'da sağ üstteki **Authorize** butonuna token'ı yapıştırın.
3. `POST /api/vehicles` ile araç ekleyin, ardından bakım / yakıt / masraf / hatırlatma uçlarını kullanın.

E-posta bildirimlerinin gerçekten gönderilmesi için `appsettings.json` içindeki `Smtp` alanlarını doldurun (Gmail için uygulama şifresi gerekir). Boş bırakılırsa job çalışır ama e-posta göndermeden geçer.

## Güvenlik

`appsettings.json` içindeki `Jwt:Key` ve `Smtp` değerleri **yalnızca geliştirme ortamı içindir**; repoda açık durdukları için gizli kabul edilmezler.

Canlı ortamda bu değerleri ortam değişkeniyle geçin — ASP.NET Core'da iç içe anahtarlar `__` (iki alt çizgi) ile ayrılır:

```
Jwt__Key=<en az 32 karakterlik, rastgele üretilmiş anahtar>
Smtp__Host=smtp.ornek.com
Smtp__User=...
Smtp__Password=...
Smtp__From=...
```

Ortam değişkenleri `appsettings.json` içindeki değerlerin üzerine yazar, dosyayı düzenlemeniz gerekmez. Prod'a çıkarken `Jwt__Key` mutlaka yeni ve rastgele bir değerle verilmelidir; varsayılan anahtarla üretilen token'lar herkes tarafından taklit edilebilir.

## Uç Noktalar

- `POST /api/auth/register`, `POST /api/auth/login`
- `GET|POST /api/vehicles`, `GET|PUT|DELETE /api/vehicles/{id}`
- `GET /api/maintenance?vehicleId=`, `POST /api/maintenance`, `DELETE /api/maintenance/{id}`
- `GET /api/fuel?vehicleId=`, `POST /api/fuel`, `DELETE /api/fuel/{id}`
- `GET /api/expenses?vehicleId=`, `POST /api/expenses`, `DELETE /api/expenses/{id}`
- `GET /api/reminders?vehicleId=`, `GET /api/reminders/upcoming?days=30`, `POST /api/reminders`, `PUT /api/reminders/{id}/complete`, `DELETE /api/reminders/{id}`
- `GET /api/reports/summary?vehicleId=&start=&end=`, `GET /api/reports/monthly?vehicleId=`, `GET /api/reports/fuel-stats?vehicleId=`
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
| Özellikler | marka, seri, yıl, kilometre, yakıt tipi, vites tipi, kasa tipi |
| Kategorik kodlama | OneHotEncoding (59 marka, 423 seri) |
| Ayrım | %80 eğitim (41.101 satır) / %20 test (10.210 satır) |

Test setindeki metrikler:

| Metrik | Değer |
|---|---|
| R² | 0,6534 |
| MAE | 95.762 TL |
| RMSE | 460.661 TL |

MAE ile RMSE arasındaki fark, veri setindeki az sayıdaki çok yüksek fiyatlı ilanın karesel hatayı yukarı çekmesinden kaynaklanır.

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
    "tahminiFiyat": 704170,
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
- [ ] Aşama 3: xUnit testleri
- [ ] Aşama 4: Docker + GitHub Actions CI
- [ ] Aşama 5: Canlıya çıkış (ücretsiz hosting) + README'ye demo linki
