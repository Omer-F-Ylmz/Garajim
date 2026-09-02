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

            return new AuthManager(_userDal.Object, _companyDal.Object, configuration, new SahteEpostaGonderici(), new BellekKodGonderimSayaci());
        }

        [Theory]
        [InlineData("12345")]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData(null)]
        public async Task RegisterAsync_SifreAltiKarakterdenKisaysaHataDoner(string password)
        {
            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "kullanici@garajim.local",
                FullName = "Test Kullanıcı",
                Password = password
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _userDal.Verify(d => d.AddAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_AltiKarakterlikSifreKabulEdilir()
        {
            _userDal.Setup(d => d.ExistsForRegistrationAsync(It.IsAny<string>())).ReturnsAsync(false);
            _userDal.Setup(d => d.AddAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);
            _userDal.Setup(d => d.UpdateAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);

            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "kullanici@garajim.local",
                FullName = "Test Kullanıcı",
                Password = "123456"
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

            var result = await CreateManager().RegisterAsync(new RegisterDto
            {
                Email = "Kullanici@Garajim.local",
                FullName = "Test Kullanıcı",
                Password = "gizli123"
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.EmailAlreadyExists, result.Message);
            _userDal.Verify(d => d.AddAsync(It.IsAny<AppUser>()), Times.Never);
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
