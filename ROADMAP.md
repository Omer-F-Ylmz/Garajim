# Yol Haritası

Garajım bireysel araç takibinden çok kiracılı filo yönetimine dönüşüyor. Bu dosya fazların durumunu tutar.

Durum işaretleri: `[x]` bitti · `[~]` kısmen · `[ ]` başlanmadı

## Faz 1a — Mimari temel (bu oturum)

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
- [x] Demo tohumlaması yeni şemayla idempotent

Faz 1a'da bilerek yapılmayanlar (üçü de Faz 1b'de kapandı):

- `Documents` ve `VehicleAssignments` tabloları yalnızca şema olarak var; davranışları Faz 1b'de gelir
- Araç plakası tekilliği hâlâ kullanıcı başına (`UserId, Plate`); çok kullanıcılı şirkette şirket başına olmalı, Faz 1b
- Aktif zimmet için filtreli tekil indeks Faz 1b'de iş kuralıyla birlikte eklenecek

## Faz 1b — Roller, zimmet, belge, arayüz

- [x] `CompanyRole`: Owner, Manager, Driver; JWT'ye rol claim'i
- [x] Politikalar: Owner her şey; Manager araç/kayıt/zimmet; Driver yalnız zimmetli araçları
- [x] Kullanıcı yönetimi uçları (yalnız Owner): kullanıcı ekle (geçici şifre yanıtta bir kez), rol değiştir, pasifleştir
- [x] Araç-sürücü zimmeti: aynı anda tek aktif zimmet (filtreli tekil indeks + iş kuralı), zimmet geçmişi, devir
- [x] Belge yükleme: wwwroot dışında saklama, uzantı beyaz listesi, magic-byte kontrolü, boyut ve şirket kotası
- [x] SPA: şirket adı alanı, Ekip ekranı, araç kartında zimmet, bakım kaydına belge, Driver görünümü
- [x] Plaka tekilliğinin şirket başına taşınması; migration çakışan plakaları bulup açık hatayla durur
- [x] Demo tohumlaması sahip + sürücü + aktif zimmet üretir, idempotent kalır

Faz 1b sırasında yakalanan ve düzeltilenler:

- `app.js` ve `styles.css` bir saat önbellekleniyordu; yayından sonra dönen kullanıcı yeni HTML'i eski scriptle çalıştırıyordu. İkisi de artık doğrulamaya bağlı
- Belge yükleme ucu `[FromForm] IFormFile` ile ayrı parametreler aldığı için Swagger dokümanı 500 veriyordu; bağlama tek forma taşındı
- Rol sıfırlaması araçlar yüklenmeden sekmeyi tetikleyip `vehicleId=null` isteği atıyordu

Faz 1b'de bilerek yapılmayanlar:

- Geçmişe dönük zimmet devri (elle `StartDate` / `EndDate`) yok; devir her zaman "şimdi". Tarih düzeltmesi audit log ile birlikte gelecek
- Driver'a "kendi eklediği araç" istisnası yok; erişim yalnız aktif zimmetten gelir
- Belge önizlemesi yok; yalnız yükleme, listeleme, indirme ve silme var

## Faz 2 — Filo operasyonu

- [ ] Resmî tarih takibi (muayene, egzoz, trafik sigortası, kasko) ve otomatik hatırlatma
- [ ] Periyodik bakım şablonları: kilometre ve takvim bazlı
- [ ] Yakıt analizi: L/100km, filo ortalamasından sapma uyarısı
- [ ] Araç başına toplam sahip olma maliyeti (TCO) raporu
- [ ] CSV / Excel dışa aktarım
- [ ] Yönetici dashboard'u
- [ ] Sürücü belge takibi (ehliyet, SRC)

## Faz 3 — Bildirim, veri, ticarileşme

- [ ] E-posta (SMTP) ve SMS bildirimi
- [ ] Excel'den toplu içe aktarma
- [ ] Audit log
- [ ] Mobil uyumlu arayüz
- [ ] Hasar ve kaza kayıtları
- [ ] Lastik takibi
- [ ] Abonelik ve ödeme (iyzico veya PayTR), araç sayısına göre paket sınırları
- [ ] KVKK temeli: aydınlatma metni, veri silme akışı

## Faz 4 — İleri analiz ve entegrasyon

- [ ] Araç değiştirme analizi: mevcut ML fiyat tahmini ile bakım maliyeti eğrisinin birleştirilmesi
- [ ] Muhasebe entegrasyonları ve dışa açık API
- [ ] Çoklu şube

## Sıraya alınan teknik borçlar

Ayrıntısı [#2 numaralı issue](https://github.com/Omer-F-Ylmz/Garajim/issues/2)'da:

- [ ] Kayıt tarihlerinde üst sınır yok (gelecek tarih kabul ediliyor)
- [ ] Token localStorage'da, süre yönetimi istemcide yok
- [ ] Araç güncelleme ve silme arayüzde yok
- [ ] Hangfire paneli için üretimde bilinçli yetkilendirme filtresi kararı
