namespace Garajim.Tests.Unit
{
    public class BetikSozdizimiTests
    {
        private static string Kok()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok.FullName;
        }

        public static IEnumerable<object[]> Betikler()
        {
            var klasor = Path.Combine(Kok(), "Garajim.API", "wwwroot");

            foreach (var yol in Directory.GetFiles(klasor, "*.js"))
            {
                if (Path.GetFileName(yol).StartsWith("sw.", StringComparison.Ordinal))
                {
                    yield return new object[] { Path.GetFileName(yol) };
                    continue;
                }

                yield return new object[] { Path.GetFileName(yol) };
            }
        }

        [Theory]
        [MemberData(nameof(Betikler))]
        public void BetikteKacisDizgeDisindaGecmez(string dosya)
        {
            var hatalar = Tara(File.ReadAllText(Path.Combine(Kok(), "Garajim.API", "wwwroot", dosya)));

            Assert.True(hatalar.Count == 0, dosya + ": " + string.Join(" | ", hatalar));
        }

        private static List<string> Tara(string kaynak)
        {
            var hatalar = new List<string>();
            var yigin = new Stack<(char Ac, int Satir)>();
            var satir = 1;
            var i = 0;
            var oncekiAnlamli = '\0';

            while (i < kaynak.Length)
            {
                var c = kaynak[i];
                var sonraki = i + 1 < kaynak.Length ? kaynak[i + 1] : '\0';

                if (c == '\n')
                {
                    satir++;
                    i++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == '/' && sonraki == '/')
                {
                    var son = kaynak.IndexOf('\n', i);
                    i = son < 0 ? kaynak.Length : son;
                    continue;
                }

                if (c == '/' && sonraki == '*')
                {
                    var son = kaynak.IndexOf("*/", i + 2, StringComparison.Ordinal);

                    if (son < 0)
                    {
                        hatalar.Add($"satır {satir}: kapanmamış blok yorum");
                        break;
                    }

                    satir += kaynak.Substring(i, son - i).Count(h => h == '\n');
                    i = son + 2;
                    continue;
                }

                if (c == '/' && RegexBaslayabilir(oncekiAnlamli, kaynak, i))
                {
                    var basSatir = satir;
                    i++;
                    var sinifIcinde = false;

                    while (i < kaynak.Length)
                    {
                        var d = kaynak[i];

                        if (d == '\\') { i += 2; continue; }
                        if (d == '\n') { hatalar.Add($"satır {basSatir}: kapanmamış düzenli ifade"); break; }
                        if (d == '[') { sinifIcinde = true; }
                        if (d == ']') { sinifIcinde = false; }
                        if (d == '/' && !sinifIcinde) { i++; break; }

                        i++;
                    }

                    oncekiAnlamli = '/';
                    continue;
                }

                if (c == '"' || c == '\'' || c == '`')
                {
                    var tirnak = c;
                    var basSatir = satir;
                    i++;

                    while (i < kaynak.Length)
                    {
                        var d = kaynak[i];

                        if (d == '\\') { i += 2; continue; }

                        if (d == '\n')
                        {
                            if (tirnak == '`') { satir++; i++; continue; }

                            hatalar.Add($"satır {basSatir}: kapanmamış {tirnak} dizgesi");
                            break;
                        }

                        if (d == tirnak) { i++; break; }

                        i++;
                    }

                    oncekiAnlamli = tirnak;
                    continue;
                }

                if (c == '\\')
                {
                    hatalar.Add($"satır {satir}: dizge dışında kaçış karakteri (büyük olasılıkla betik üretirken sızmış \\n)");
                    i++;
                    continue;
                }

                if (c == '{' || c == '(' || c == '[')
                {
                    yigin.Push((c, satir));
                }
                else if (c == '}' || c == ')' || c == ']')
                {
                    var beklenen = c == '}' ? '{' : c == ')' ? '(' : '[';

                    if (yigin.Count == 0)
                    {
                        hatalar.Add($"satır {satir}: fazladan {c}");
                    }
                    else
                    {
                        var ust = yigin.Pop();

                        if (ust.Ac != beklenen)
                        {
                            hatalar.Add($"satır {satir}: {c} geldi ama satır {ust.Satir} içinde {ust.Ac} açılmış");
                        }
                    }
                }

                oncekiAnlamli = c;
                i++;
            }

            foreach (var kalan in yigin)
            {
                hatalar.Add($"satır {kalan.Satir}: kapanmamış {kalan.Ac}");
            }

            return hatalar;
        }

        private static readonly string[] RegexOncesiSozcukler =
        {
            "return", "typeof", "case", "in", "of", "delete", "void", "instanceof",
            "new", "do", "else", "yield", "await", "throw"
        };

        private static bool RegexBaslayabilir(char oncekiAnlamli, string kaynak, int konum)
        {
            if (oncekiAnlamli == '\0')
            {
                return true;
            }

            if ("(,=:[!&|?{};+-*%~^<>".IndexOf(oncekiAnlamli) >= 0)
            {
                return true;
            }

            if (!char.IsLetter(oncekiAnlamli))
            {
                return false;
            }

            var son = konum - 1;

            while (son >= 0 && char.IsWhiteSpace(kaynak[son]))
            {
                son--;
            }

            var bas = son;

            while (bas >= 0 && (char.IsLetterOrDigit(kaynak[bas]) || kaynak[bas] == '_'))
            {
                bas--;
            }

            var sozcuk = kaynak.Substring(bas + 1, son - bas);

            return RegexOncesiSozcukler.Contains(sozcuk);
        }
    }
}
