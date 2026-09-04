using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class KurulumManager : IKurulumService
    {
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IFuelDal _fuelDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IReceiptDraftDal _receiptDraftDal;
        private readonly IEvrakDal _evrakDal;

        public KurulumManager(
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IFuelDal fuelDal,
            IMaintenanceDal maintenanceDal,
            IReceiptDraftDal receiptDraftDal,
            IEvrakDal evrakDal)
        {
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _fuelDal = fuelDal;
            _maintenanceDal = maintenanceDal;
            _receiptDraftDal = receiptDraftDal;
            _evrakDal = evrakDal;
        }

        public async Task<IDataResult<KurulumDurumDto>> DurumAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<KurulumDurumDto>(Messages.UserNotFound);

            var aracVar = await _vehicleDal.AnyAsync(v => !v.Arsivli);
            var ilkKayitVar = aracVar &&
                (await _fuelDal.AnyAsync(f => f.Id > 0)
                 || await _maintenanceDal.AnyAsync(m => m.Id > 0)
                 || await _receiptDraftDal.AnyAsync(r => r.Id > 0));
            var evrakVar = aracVar && await _evrakDal.AnyAsync(e => e.Id > 0);

            var tamamlanan = (aracVar ? 1 : 0) + (ilkKayitVar ? 1 : 0) + (evrakVar ? 1 : 0);

            return new SuccessDataResult<KurulumDurumDto>(new KurulumDurumDto
            {
                AracVar = aracVar,
                IlkKayitVar = ilkKayitVar,
                EvrakVar = evrakVar,
                Yuzde = tamamlanan * 100 / KurulumAdimlari.Sayi,
                Gizlendi = user.KurulumGizlendi
            });
        }

        public async Task<IResult> GizleAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            if (!user.KurulumGizlendi)
            {
                user.KurulumGizlendi = true;
                await _userDal.UpdateAsync(user);
            }

            return new SuccessResult(Messages.KurulumGizlendi);
        }
    }
}
