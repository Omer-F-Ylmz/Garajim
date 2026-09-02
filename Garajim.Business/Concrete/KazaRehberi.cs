using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public static class KazaRehberi
    {
        public const string KaynakNotu =
            "Kaynak: Karayolları Trafik Kanunu ve Maddi Hasarlı Trafik Kazası Tespit Tutanağı düzenleme esasları " +
            "ile Karayolları Motorlu Araçlar Zorunlu Mali Sorumluluk Sigortası Genel Şartları (bildirim süresi).";

        public static KazaRehberiDto Olustur()
        {
            return new KazaRehberiDto
            {
                Ozet = "Önce can güvenliği: aracı güvenli yere çek, dörtlüleri yak, reflektörü koy. " +
                       "Yaralı varsa 112'yi ara ve aracı yerinden oynatma.",

                AnlasmaliTutanakKosullari = new List<string>
                {
                    "Kazada yalnızca maddi hasar var; yaralı ya da ölü yok.",
                    "Sürücülerin ikisi de olay yerinde ve tutanağı imzalıyor.",
                    "Sürücülerin ikisi de geçerli sürücü belgesine sahip.",
                    "Sürücülerde alkol ya da uyuşturucu etkisi yok.",
                    "Kamu malına (bariyer, aydınlatma direği, trafik levhası) zarar gelmemiş.",
                    "Araçların ikisinin de geçerli zorunlu trafik sigortası var.",
                    "Araçlardan biri kamu aracı, resmi araç ya da sürücüsü belirsiz değil."
                },

                PolisGerekliHaller = new List<string>
                {
                    "Yaralı ya da ölü varsa (112 ve 155 aranır, araçlar yerinden oynatılmaz).",
                    "Sürücülerden biri olay yerinde değilse ya da kaçtıysa.",
                    "Sürücülerden birinde alkol/uyuşturucu şüphesi varsa.",
                    "Sürücülerden birinin sürücü belgesi ya da trafik sigortası yoksa.",
                    "Kamu malına zarar verilmişse.",
                    "Taraflar anlaşamıyor ya da biri tutanağı imzalamıyorsa.",
                    "Kazaya karışan araçlardan biri kamu/resmi araç ya da tek taraflı park hâlindeki araca çarpma ise."
                },

                FotografListesi = new List<string>
                {
                    "Kaza yerinin genel görünümü: araçların son durumu, en az iki farklı açıdan.",
                    "Hasarlı bölgelerin yakın çekimi (kendi aracınız ve karşı araç).",
                    "Her iki aracın plakası okunacak şekilde.",
                    "Yol, şerit çizgileri, trafik levhaları ve varsa fren izleri.",
                    "Karşı tarafın ruhsat ve sigorta poliçesi (tutanağa yazılacak bilgiler için).",
                    "Doldurulup imzalanmış tutanağın kendisi."
                },

                AlinacakBilgiler = new List<string>
                {
                    "Karşı aracın plakası.",
                    "Karşı tarafın sigorta şirketi ve poliçe numarası.",
                    "Kazanın tarihi, saati ve tam konumu.",
                    "Varsa tarafsız tanık bilgisi.",
                    "Polis çağrıldıysa tutanak numarası."
                },

                BildirimSuresi =
                    "Kazayı sigorta şirketine öğrendiğiniz tarihten itibaren en geç 5 iş günü içinde bildirin; " +
                    "gecikme tazminatın eksik ödenmesine yol açabilir. Kasko poliçenizin süresi ayrıca kontrol edilmelidir.",

                Adimlar = new List<KazaRehberiAdimiDto>
                {
                    new KazaRehberiAdimiDto
                    {
                        Baslik = "1. Güvenliği sağla",
                        Maddeler = new List<string>
                        {
                            "Dörtlüleri yak.",
                            "Yaralı var mı bak; varsa 112'yi ara ve araçları oynatma.",
                            "Reflektörü uygun mesafeye koy.",
                            "Trafiği tıkıyorsa ve yaralı yoksa fotoğrafları çektikten sonra aracı kenara al."
                        }
                    },
                    new KazaRehberiAdimiDto
                    {
                        Baslik = "2. Tutanak türüne karar ver",
                        Maddeler = new List<string>
                        {
                            "Yukarıdaki anlaşmalı tutanak koşullarının tamamı sağlanıyorsa anlaşmalı tutanak doldurulur.",
                            "Koşullardan biri bile sağlanmıyorsa 155'i (şehirlerarası yolda 156) ara."
                        }
                    },
                    new KazaRehberiAdimiDto
                    {
                        Baslik = "3. Fotoğrafları çek",
                        Maddeler = new List<string>
                        {
                            "Araçları oynatmadan önce genel görünümü çek.",
                            "Hasarları yakından, plakaları okunacak şekilde çek.",
                            "Yolu ve levhaları çerçeveye al."
                        }
                    },
                    new KazaRehberiAdimiDto
                    {
                        Baslik = "4. Bilgileri al ve bildir",
                        Maddeler = new List<string>
                        {
                            "Karşı aracın plakası, sigorta şirketi ve poliçe numarasını not et.",
                            "Tutanağı imzala, kendi nüshanı al.",
                            "Sigorta şirketine 5 iş günü içinde bildir."
                        }
                    }
                },

                Kaynak = KaynakNotu
            };
        }
    }
}
