using System.Linq.Expressions;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Security;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Tests.Integration;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Garajim.Tests.Unit
{
    public class AuthManagerTests
    {
        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>(MockBehavior.Strict);
        private readonly Mock<ICompanyDal> _companyDal = new Mock<ICompanyDal>();
        private readonly SahteEpostaGonderici _eposta = new SahteEpostaGonderici();
        private readonly BellekKodGonderimSayaci _sayac = new BellekKodGonderimSayaci();

        private AuthManager CreateManager()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "test-ortami-icin-en-az-32-karakterlik-gizli-anahtar",
                    ["Jwt:Issuer"] = "Garajim",
                    ["Jwt:Audience"] = "GarajimClient",
                    ["Jwt:ExpireDays"] = "7"
                })
                .Build();

            return new AuthManager(_userDal.Object, _companyDal.Object, configuration, _eposta, _sayac, new SahteUnitOfWork());
        }

        [Theory]
        [InlineData("12345")]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("gizli12")]
        [InlineData("gizlidir")]
        [InlineData("12345678")]
        public async Task RegisterAsync_SifreKuralaUymuyorsaHataDoner(string password)
        {
            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "kullanici@garajim.local",
                FullName = "Test Kullanıcı",
                Password = password
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.SifreKuraliUymuyor, result.Message);
            _userDal.Verify(d => d.AddAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_SekizKarakterlikSifreKabulEdilir()
        {
            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>())).ReturnsAsync(false);
            _userDal.Setup(d => d.AddAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);

            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "kullanici@garajim.local",
                FullName = "Test Kullanıcı",
                Password = "gizli123"
            });

            Assert.True(result.Success);
            Assert.Equal(Messages.DogrulamaKoduGonderildi, result.Message);
            Assert.True(result.Data.DogrulamaGerekli);
            Assert.Equal("kullanici@garajim.local", result.Data.Email);
        }

        [Theory]
        [InlineData("  Test@Example.COM  ", "test@example.com")]
        [InlineData("KULLANICI@GARAJIM.LOCAL", "kullanici@garajim.local")]
        public async Task RegisterAsync_EpostaKucukHarfeCevrilipKirpilir(string girilen, string beklenen)
        {
            AppUser eklenen = null;
            string aranan = null;

            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>()))
                .Callback<string>(deger => aranan = deger)
                .ReturnsAsync(false);
            _userDal.Setup(d => d.AddAsync(It.IsAny<AppUser>()))
                .Callback<AppUser>(user => eklenen = user)
                .Returns(Task.CompletedTask);
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);

            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = girilen,
                FullName = "  Test Kullanıcı  ",
                Password = "gizli123"
            });

            Assert.True(result.Success);
            Assert.Equal(beklenen, eklenen.Email);
            Assert.Equal("Test Kullanıcı", eklenen.FullName);
            Assert.Equal(beklenen, result.Data.Email);

            Assert.Equal(beklenen, aranan);
        }

        [Fact]
        public async Task RegisterAsync_AyniEpostaIkinciKezKaydedilemez()
        {
            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>())).ReturnsAsync(true);
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AppUser { Id = 3, Email = "kullanici@garajim.local", FullName = "Test Kullanıcı" });

            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "Kullanici@Garajim.local",
                FullName = "Test Kullanıcı",
                Password = "gizli123"
            });

            Assert.True(result.Success);
            _userDal.Verify(d => d.AddAsync(It.IsAny<AppUser>()), Times.Never);
            _companyDal.Verify(d => d.AddAsync(It.IsAny<Company>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_KayitliAdresteDeAyniYanitDoner()
        {
            RegisterDto Istek() => new RegisterDto
            {
                Email = "Kullanici@Garajim.local",
                FullName = "Test Kullanıcı",
                Password = "gizli123"
            };

            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>())).ReturnsAsync(false);
            _userDal.Setup(d => d.AddAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);
            var yeni = await CreateManager().RegisterAsync(Istek());

            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>())).ReturnsAsync(true);
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AppUser { Id = 3, Email = "kullanici@garajim.local", FullName = "Test Kullanıcı" });
            var mevcut = await CreateManager().RegisterAsync(Istek());

            Assert.Equal(yeni.Success, mevcut.Success);
            Assert.Equal(yeni.Message, mevcut.Message);
            Assert.Equal(yeni.Data.DogrulamaGerekli, mevcut.Data.DogrulamaGerekli);
            Assert.Equal(yeni.Data.Email, mevcut.Data.Email);
        }

        [Fact]
        public async Task RegisterAsync_KayitliAdreseBilgilendirmeGider()
        {
            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>())).ReturnsAsync(true);
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AppUser { Id = 3, Email = "kullanici@garajim.local", FullName = "Test Kullanıcı" });

            await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "Kullanici@Garajim.local",
                FullName = "Test Kullanıcı",
                Password = "gizli123"
            });

            Assert.Equal(1, _eposta.SayiOf("kullanici@garajim.local"));
            Assert.Null(_eposta.SonKod("kullanici@garajim.local"));
        }

        [Fact]
        public async Task RegisterAsync_KayitliAdreseSaatlikSinirdanSonraSusar()
        {
            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>())).ReturnsAsync(true);
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AppUser { Id = 3, Email = "kullanici@garajim.local", FullName = "Test Kullanıcı" });

            for (var i = 0; i < DogrulamaKodu.SaatlikGonderimSiniri; i++)
            {
                _sayac.Say("kullanici@garajim.local");
            }

            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "Kullanici@Garajim.local",
                FullName = "Test Kullanıcı",
                Password = "gizli123"
            });

            Assert.True(result.Success);
            Assert.Equal(0, _eposta.SayiOf("kullanici@garajim.local"));
        }

        private AppUser GirisKullanicisi(string sifre = "gizli123")
        {
            HashingHelper.CreatePasswordHash(sifre, out var hash, out var salt);

            return new AppUser
            {
                Id = 7,
                CompanyId = 2,
                Email = "kilit@garajim.local",
                FullName = "Kilit Kullanıcı",
                IsActive = true,
                EmailDogrulandi = true,
                PasswordHash = hash,
                PasswordSalt = salt
            };
        }

        [Fact]
        public async Task LoginAsync_ArtArdaYanlisSifreHesabiKilitler()
        {
            var user = GirisKullanicisi();
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);

            var yonetici = CreateManager();

            for (var i = 0; i < GirisKilidi.MaxDeneme; i++)
            {
                await yonetici.LoginAsync(new LoginDto { Email = user.Email, Password = "yanlissifre1" });
            }

            Assert.NotNull(user.GirisKilitBitis);

            var dogruDeneme = await yonetici.LoginAsync(new LoginDto { Email = user.Email, Password = "gizli123" });

            Assert.False(dogruDeneme.Success);
            Assert.Equal(Messages.InvalidCredentials, dogruDeneme.Message);
        }

        [Fact]
        public async Task LoginAsync_KilitSuresiDolunca_DogruSifreGecer()
        {
            var user = GirisKullanicisi();
            user.GirisKilitBitis = DateTime.UtcNow.AddMinutes(-1);
            user.GirisDenemeSayisi = 0;

            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);
            _companyDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Company, bool>>>()))
                .ReturnsAsync(new Company { Id = 2, Name = "Kilit" });

            var sonuc = await CreateManager().LoginAsync(new LoginDto { Email = user.Email, Password = "gizli123" });

            Assert.True(sonuc.Success);
            Assert.Null(user.GirisKilitBitis);
        }

        [Fact]
        public async Task LoginAsync_BasariliGirisSayaciSifirlar()
        {
            var user = GirisKullanicisi();
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);
            _companyDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Company, bool>>>()))
                .ReturnsAsync(new Company { Id = 2, Name = "Kilit" });

            var yonetici = CreateManager();

            await yonetici.LoginAsync(new LoginDto { Email = user.Email, Password = "yanlissifre1" });
            Assert.Equal(1, user.GirisDenemeSayisi);

            var sonuc = await yonetici.LoginAsync(new LoginDto { Email = user.Email, Password = "gizli123" });

            Assert.True(sonuc.Success);
            Assert.Equal(0, user.GirisDenemeSayisi);
        }

        [Fact]
        public async Task LoginAsync_EpostaNormalizeEdilerekAranirVeDogruSifreTokenDoner()
        {
            HashingHelper.CreatePasswordHash("gizli123", out var hash, out var salt);
            var kayitliKullanici = new AppUser
            {
                Id = 5,
                IsActive = true,
                EmailDogrulandi = true,
                Email = "kullanici@garajim.local",
                FullName = "Test Kullanıcı",
                PasswordHash = hash,
                PasswordSalt = salt
            };

            string aranan = null;
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>()))
                .Callback<string>(deger => aranan = deger)
                .ReturnsAsync(kayitliKullanici);

            var result = await CreateManager().LoginAsync(new LoginDto
            {
                Email = "  KULLANICI@Garajim.LOCAL ",
                Password = "gizli123"
            });

            Assert.True(result.Success);
            Assert.Equal(Messages.LoginSuccess, result.Message);
            Assert.Equal("kullanici@garajim.local", aranan);
        }

        [Fact]
        public async Task LoginAsync_YanlisSifreIcinHataDoner()
        {
            HashingHelper.CreatePasswordHash("gizli123", out var hash, out var salt);
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AppUser { Id = 5, IsActive = true, Email = "kullanici@garajim.local", PasswordHash = hash, PasswordSalt = salt });
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);

            var result = await CreateManager().LoginAsync(new LoginDto
            {
                Email = "kullanici@garajim.local",
                Password = "yanlis123"
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidCredentials, result.Message);
        }

        [Fact]
        public async Task LoginAsync_KullaniciYoksaHataDoner()
        {
            _userDal.Setup(d => d.GetForAuthenticationAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null);

            var result = await CreateManager().LoginAsync(new LoginDto { Email = "yok@garajim.local", Password = "gizli123" });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidCredentials, result.Message);
        }
    }
}
