using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class AssignmentUniqueIndexTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private readonly AppUser _birinci;
        private readonly AppUser _ikinci;
        private readonly Vehicle _arac;

        public AssignmentUniqueIndexTests()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            _birinci = _db.KullaniciEkle("surucu1@garajim.local", CompanyRole.Driver);
            _ikinci = _db.KullaniciEkle("surucu2@garajim.local", CompanyRole.Driver);
            _arac = _db.AracEkle(sahip.Id, "34IDX111");
        }

        private VehicleAssignment Zimmet(int userId, DateTime? bitis)
        {
            return new VehicleAssignment
            {
                CompanyId = _arac.CompanyId,
                VehicleId = _arac.Id,
                UserId = userId,
                StartDate = new DateTime(2026, 3, 1),
                EndDate = bitis,
                AssignedByUserId = userId,
                CreatedAt = new DateTime(2026, 3, 1)
            };
        }

        [Fact]
        public async Task IkinciAktifZimmetVeritabaniTarafindanReddedilir()
        {
            _db.Context.VehicleAssignments.Add(Zimmet(_birinci.Id, null));
            await _db.Context.SaveChangesAsync();

            _db.Context.VehicleAssignments.Add(Zimmet(_ikinci.Id, null));

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
        }

        [Fact]
        public async Task KapanmisZimmetlerAyniAracaBirdenFazlaOlabilir()
        {
            _db.Context.VehicleAssignments.Add(Zimmet(_birinci.Id, new DateTime(2026, 4, 1)));
            _db.Context.VehicleAssignments.Add(Zimmet(_ikinci.Id, new DateTime(2026, 5, 1)));
            await _db.Context.SaveChangesAsync();

            _db.Context.VehicleAssignments.Add(Zimmet(_birinci.Id, null));
            await _db.Context.SaveChangesAsync();

            Assert.Equal(3, await _db.Context.VehicleAssignments.CountAsync(a => a.VehicleId == _arac.Id));
            Assert.Equal(1, await _db.Context.VehicleAssignments.CountAsync(a => a.VehicleId == _arac.Id && a.EndDate == null));
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
