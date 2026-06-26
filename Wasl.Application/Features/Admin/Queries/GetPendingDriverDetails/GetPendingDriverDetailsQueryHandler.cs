using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Admin.Queries.GetPendingDriverDetails
{
    public class GetPendingDriverDetailsQueryHandler : IRequestHandler<GetPendingDriverDetailsQuery, ApiResponse<PendingDriverDetailsDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPendingDriverDetailsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PendingDriverDetailsDto>> Handle(GetPendingDriverDetailsQuery request, CancellationToken cancellationToken)
        {
            var driverDetails = await _context.DriverProfiles
                .Include(dp => dp.User)
                .Where(dp => dp.UserId == request.DriverId && dp.ApprovalStatus == DriverApprovalStatus.UnderReview)
                .Select(dp => new PendingDriverDetailsDto
                {
                    DriverId = dp.UserId,
                    FullName = dp.User.FirstName + " " + dp.User.LastName,
                    Email = dp.User.Email,
                    PhoneNumber = dp.User.PhoneNumber,
                    City = dp.User.City,
                    Address = dp.User.Address,
                    VehicleModel = dp.VehicleModel,
                    VehicleYear = dp.VehicleYear,
                    VinNumber = dp.VinNumber,
                    VehicleImagesUrl = dp.VehicleImagesUrl,
                    LicenseFrontUrl = dp.LicenseFrontUrl,
                    LicenseBackUrl = dp.LicenseBackUrl,
                    SelfieUrl = dp.SelfieUrl,
                    SubmittedAt = dp.CreatedAt
                })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (driverDetails == null)
            {
                return ApiResponse<PendingDriverDetailsDto>.Failure("السائق غير موجود أو تمت معالجة طلبه مسبقاً.");
            }

            return ApiResponse<PendingDriverDetailsDto>.Success(driverDetails, "تم جلب تفاصيل السائق بنجاح.");
        }
    }
}