# Yayın Günü Kontrol Listesi

MonsterASP.NET üzerine Web Deploy (MSDeploy) ile yayın içindir. Adımları sırayla uygula; bir adım beklenenden farklı sonuç verirse **dur**, sonraki adıma geçme.

Yayın öncesi durum: 234 test yeşil, Release derlemesi 0 uyarı / 0 hata, CI `main` üzerinde başarılı.

## 1. Veritabanı yedeği (atlanamaz)

1. MonsterASP panelinden veritabanı yedeğini al.
2. Yedeği **bilgisayarına indir** ve dosya boyutunun sıfırdan büyük olduğunu gör. Panelde "yedek alındı" yazması yeterli değil; indirilmemiş yedek yedek sayılmaz.

Neden şart: `Down` migration'ları veri kaybettirir. `AddCompanyTenancy` geri alınırsa `CompanyId` kolonları, `AddCompanyRoles` geri alınırsa rol ve aktiflik bilgisi silinir. Geri dönüşün tek güvenli yolu yedekten dönmektir.

Ayrıca: **belgeler veritabanı yedeğinde yoktur.** Sunucuda daha önce yüklenmiş belge varsa, `App_Data/documents` klasörünü de ayrıca indir.

## 2. Panel ortam değişkenleri

Yayından **önce** ayarla:

| Değişken | Değer | Not |
|---|---|---|
| `Documents__StoragePath` | `..\data\documents` (site kökünde, wwwroot'un yanında) | Aşağıdaki açıklamayı oku |
| `ConnectionStrings__Default` | Uzak MSSQL bağlantısı | LocalDB içeriyorsa uygulama açılmayı reddeder |
| `Jwt__Key` | 32+ karakter | Boş veya kısa ise uygulama açılmayı reddeder |
| `DemoSeed__Enabled` | `true` veya `false` | Canlıda demo verisi isteniyor mu, karar ver |

`Documents__StoragePath` neden önemli: değer boş bırakılırsa uygulama belgeleri **publish klasörünün içine**, `App_Data/documents` altına yazar. Publish klasörü MSDeploy'un senkron hedefidir; oraya yazılan kullanıcı verisi her yayında risk altındadır (bkz. bölüm 3).

Göreli değerler uygulama klasörüne (`AppContext.BaseDirectory`) göre çözülür, çalışma dizinine göre değil; sonuç her ortamda aynıdır. Uygulama `wwwroot`'a publish edildiği için `..\data\documents` değeri bir üste, `wwwroot`'un yanındaki `data\documents` klasörüne gider — yani publish hedefinin dışına. Mutlak bir yol verirsen aynen kullanılır.

Bu klasörün MonsterASP'te yazılabilir olduğu bu oturumda **doğrulanmadı**; ilk yayında bölüm 4'teki belge yükleme adımıyla teyit et. Yükleme hata verirse mutlak bir yola geç veya varsayılan `App_Data` yolunda kal; o durumda bölüm 3'teki koruma devrede.

Yol yanlış veya yazılamazsa belge yükleme çalışma anında hata verir; sessiz veri kaybı olmaz.

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

1. `https://<site>/swagger/v1/swagger.json` → **200** dönüyor mu? (Doküman üretimi bozulursa 500 döner.)
2. Demo veya gerçek bir **Owner** hesabıyla giriş: cevapta `role` ve `companyName` geliyor mu?
3. **Driver** hesabıyla giriş: yalnızca zimmetli aracı görüyor mu? Zimmetsiz bir araç kimliğine `GET /api/Vehicles/{id}` → **404** mü?
4. Bir bakım kaydına **belge yükle**, sonra **indir**. Dosya doğru geliyor mu?
5. **Eski oturumlar**: yayından önce açık kalmış bir tarayıcı sekmesini yenile. Rol taşımayan oturum giriş ekranına düşmeli. Düşmüyorsa kullanıcıya çıkış-giriş yaptır.

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
