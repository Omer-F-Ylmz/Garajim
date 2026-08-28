using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class AssignmentManager : IAssignmentService
    {
        private readonly IVehicleAssignmentDal _assignmentDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IUserDal _userDal;

        public AssignmentManager(IVehicleAssignmentDal assignmentDal, IVehicleAccessService vehicleAccess, IUserDal userDal)
        {
            _assignmentDal = assignmentDal;
            _vehicleAccess = vehicleAccess;
            _userDal = userDal;
        }

        public async Task<IDataResult<List<AssignmentDto>>> GetHistoryAsync(int currentUserId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(currentUserId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<AssignmentDto>>(Messages.VehicleNotFound);

            var assignments = await _assignmentDal.GetHistoryAsync(vehicleId);
            var users = await _userDal.GetListAsync();
            var list = assignments.Select(a => MapToDto(a, vehicle, users)).ToList();

            return new SuccessDataResult<List<AssignmentDto>>(list);
        }

        public async Task<IDataResult<AssignmentDto>> AssignAsync(int currentUserId, AssignmentCreateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(currentUserId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<AssignmentDto>(Messages.VehicleNotFound);

            var target = await _userDal.GetAsync(u => u.Id == dto.UserId && u.IsActive);
            if (target == null)
                return new ErrorDataResult<AssignmentDto>(Messages.UserNotFound);

            var active = await _assignmentDal.GetActiveByVehicleAsync(dto.VehicleId);
            if (active != null)
                return new ErrorDataResult<AssignmentDto>(Messages.AssignmentAlreadyActive);

            var assignment = await OlusturAsync(currentUserId, vehicle, target);
            return new SuccessDataResult<AssignmentDto>(MapToDto(assignment, vehicle, new List<AppUser> { target }), Messages.AssignmentCreated);
        }

        public async Task<IDataResult<AssignmentDto>> TransferAsync(int currentUserId, AssignmentCreateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(currentUserId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<AssignmentDto>(Messages.VehicleNotFound);

            var target = await _userDal.GetAsync(u => u.Id == dto.UserId && u.IsActive);
            if (target == null)
                return new ErrorDataResult<AssignmentDto>(Messages.UserNotFound);

            var active = await _assignmentDal.GetActiveByVehicleAsync(dto.VehicleId);
            if (active != null)
            {
                if (active.UserId == target.Id)
                    return new ErrorDataResult<AssignmentDto>(Messages.AssignmentSameDriver);

                active.EndDate = DateTime.UtcNow;
                await _assignmentDal.UpdateAsync(active);
            }

            var assignment = await OlusturAsync(currentUserId, vehicle, target);
            return new SuccessDataResult<AssignmentDto>(MapToDto(assignment, vehicle, new List<AppUser> { target }), Messages.AssignmentTransferred);
        }

        public async Task<IResult> EndAsync(int currentUserId, AssignmentEndDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(currentUserId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);

            var active = await _assignmentDal.GetActiveByVehicleAsync(dto.VehicleId);
            if (active == null)
                return new ErrorResult(Messages.AssignmentNotFound);

            active.EndDate = DateTime.UtcNow;
            await _assignmentDal.UpdateAsync(active);
            return new SuccessResult(Messages.AssignmentEnded);
        }

        private async Task<VehicleAssignment> OlusturAsync(int currentUserId, Vehicle vehicle, AppUser target)
        {
            var assignment = new VehicleAssignment
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                UserId = target.Id,
                StartDate = DateTime.UtcNow,
                EndDate = null,
                AssignedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _assignmentDal.AddAsync(assignment);
            return assignment;
        }

        private static AssignmentDto MapToDto(VehicleAssignment assignment, Vehicle vehicle, List<AppUser> users)
        {
            return new AssignmentDto
            {
                Id = assignment.Id,
                VehicleId = assignment.VehicleId,
                Plate = vehicle.Plate,
                UserId = assignment.UserId,
                UserFullName = users.FirstOrDefault(u => u.Id == assignment.UserId)?.FullName,
                StartDate = assignment.StartDate,
                EndDate = assignment.EndDate,
                AssignedByUserId = assignment.AssignedByUserId,
                IsActive = assignment.EndDate == null
            };
        }
    }
}
