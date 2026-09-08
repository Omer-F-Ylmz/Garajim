namespace Garajim.Dal.Sorgular
{
    public static class YinelenenTemizligi
    {
        public const string UstaOzetiTopla = @"
UPDATE UstaCozumOzetleri
SET Sayi = (
        SELECT SUM(x.Sayi)
        FROM UstaCozumOzetleri x
        WHERE x.Marka = UstaCozumOzetleri.Marka
          AND x.Model = UstaCozumOzetleri.Model
          AND ((x.Motor IS NULL AND UstaCozumOzetleri.Motor IS NULL) OR x.Motor = UstaCozumOzetleri.Motor)
          AND x.BelirtiKategori = UstaCozumOzetleri.BelirtiKategori
          AND x.ParcaTuru = UstaCozumOzetleri.ParcaTuru
    ),
    GuncellemeTarihi = (
        SELECT MAX(x.GuncellemeTarihi)
        FROM UstaCozumOzetleri x
        WHERE x.Marka = UstaCozumOzetleri.Marka
          AND x.Model = UstaCozumOzetleri.Model
          AND ((x.Motor IS NULL AND UstaCozumOzetleri.Motor IS NULL) OR x.Motor = UstaCozumOzetleri.Motor)
          AND x.BelirtiKategori = UstaCozumOzetleri.BelirtiKategori
          AND x.ParcaTuru = UstaCozumOzetleri.ParcaTuru
    )
WHERE Id IN (
    SELECT MIN(Id)
    FROM UstaCozumOzetleri
    GROUP BY Marka, Model, Motor, BelirtiKategori, ParcaTuru
    HAVING COUNT(*) > 1
);";

        public const string UstaOzetiKopyalariSil = @"
DELETE FROM UstaCozumOzetleri
WHERE Id NOT IN (
    SELECT MIN(Id)
    FROM UstaCozumOzetleri
    GROUP BY Marka, Model, Motor, BelirtiKategori, ParcaTuru
);";

        public const string HasarFotoSiraGeciciOlustur = @"
CREATE TABLE HasarFotoSiraGecici (
    Id int NOT NULL PRIMARY KEY,
    YeniSira int NOT NULL
);";

        public const string HasarFotoSiraGeciciDoldur = @"
INSERT INTO HasarFotoSiraGecici (Id, YeniSira)
SELECT f.Id,
       (
           SELECT COUNT(*)
           FROM HasarFotograflari x
           WHERE x.HasarDosyasiId = f.HasarDosyasiId
             AND (x.Sira < f.Sira OR (x.Sira = f.Sira AND x.Id <= f.Id))
       )
FROM HasarFotograflari f
WHERE f.HasarDosyasiId IN (
    SELECT HasarDosyasiId
    FROM HasarFotograflari
    GROUP BY HasarDosyasiId, Sira
    HAVING COUNT(*) > 1
);";

        public const string HasarFotoSiraUygula = @"
UPDATE HasarFotograflari
SET Sira = (SELECT g.YeniSira FROM HasarFotoSiraGecici g WHERE g.Id = HasarFotograflari.Id)
WHERE Id IN (SELECT Id FROM HasarFotoSiraGecici);";

        public const string HasarFotoSiraGeciciSil = "DROP TABLE HasarFotoSiraGecici;";
    }
}
