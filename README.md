# Garajım

Araç bakım ve masraf takip API'si. Araçlarınızın bakım, yakıt ve diğer masraflarını kaydeder; muayene, sigorta, kasko, MTV gibi tarihleri yaklaşınca e-posta ile hatırlatır; masraf raporları ve yakıt tüketim istatistikleri üretir.

## Teknolojiler

- ASP.NET Core 8 Web API (katmanlı mimari: Core / Entity / Dal / Business / API)
- Entity Framework Core 8 + SQL Server
- JWT ile kimlik doğrulama
- Hangfire ile zamanlanmış hatırlatma job'ı (her gün 06:00)
- SMTP ile e-posta bildirimi
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

## Yol Haritası

- [x] Aşama 1: Katmanlı API iskeleti — auth, araç, bakım/yakıt/masraf, hatırlatma, rapor, Hangfire job
- [ ] Aşama 2: ML.NET ile ikinci el fiyat tahmin modülü
- [ ] Aşama 3: xUnit testleri
- [ ] Aşama 4: Docker + GitHub Actions CI
- [ ] Aşama 5: Canlıya çıkış (ücretsiz hosting) + README'ye demo linki
