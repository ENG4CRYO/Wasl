using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Admin.Commands.ChangeDriverStatus
{
    public class ChangeDriverStatusCommandHandler : IRequestHandler<ChangeDriverStatusCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public ChangeDriverStatusCommandHandler(IApplicationDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<ApiResponse<bool>> Handle(ChangeDriverStatusCommand request, CancellationToken cancellationToken)
        {
            var driverProfile = await _context.DriverProfiles
                .FirstOrDefaultAsync(dp => dp.UserId == request.DriverId, cancellationToken);

            if (driverProfile == null)
            {
                return ApiResponse<bool>.Failure("لم يتم العثور على ملف السائق.");
            }


            if (driverProfile.ApprovalStatus == DriverApprovalStatus.Pending)
            {
                return ApiResponse<bool>.Failure("لا يمكن تغيير حالة هذا السائق لأنه (Pending) ولم يقم بتقديم مستمسكاته بعد.");
            }

            if (request.NewStatus == DriverApprovalStatus.Pending)
            {
                return ApiResponse<bool>.Failure("لا يمكن إرجاع حالة السائق إلى (Pending).");
            }

            if (driverProfile.ApprovalStatus == request.NewStatus)
            {
                return ApiResponse<bool>.Failure($"حالة السائق هي بالفعل ({request.NewStatus}).");
            }
            if (request.NewStatus == DriverApprovalStatus.UnderReview)
            {
                if (driverProfile.ApprovalStatus != DriverApprovalStatus.Rejected)
                {
                    return ApiResponse<bool>.Failure("لا يمكن إرجاع السائق للمراجعة إلا إذا كان مرفوضاً مسبقاً.");
                }
            }
            driverProfile.ApprovalStatus = request.NewStatus;
            await _context.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync($"DriverStatus:{request.DriverId}", cancellationToken);

            return ApiResponse<bool>.Success(true, $"تم تغيير حالة السائق إلى {request.NewStatus} بنجاح.");
        }
    }
}