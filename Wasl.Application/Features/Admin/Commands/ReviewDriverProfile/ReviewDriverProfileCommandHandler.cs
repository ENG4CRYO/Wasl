using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Admin.Commands.ReviewDriverProfile
{
    public class ReviewDriverProfileCommandHandler : IRequestHandler<ReviewDriverProfileCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _emailService; 

        public ReviewDriverProfileCommandHandler(
            IApplicationDbContext context,
            ICacheService cacheService,
            IEmailService emailService)
        {
            _context = context;
            _cacheService = cacheService;
            _emailService = emailService;
        }

        public async Task<ApiResponse<bool>> Handle(ReviewDriverProfileCommand request, CancellationToken cancellationToken)
        {
            var driverProfile = await _context.DriverProfiles
                .Include(dp => dp.User) 
                .FirstOrDefaultAsync(dp => dp.UserId == request.DriverId, cancellationToken);

            if (driverProfile == null)
            {
                return ApiResponse<bool>.Failure("حساب السائق غير موجود.");
            }

            if (driverProfile.ApprovalStatus != DriverApprovalStatus.UnderReview)
            {
                return ApiResponse<bool>.Failure($"لا يمكن مراجعة الطلب. حالة السائق الحالية هي: {driverProfile.ApprovalStatus}");
            }

            if (!request.IsApproved && string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                return ApiResponse<bool>.Failure("يجب كتابة سبب الرفض ليتم إرساله للسائق.");
            }

            driverProfile.ApprovalStatus = request.IsApproved
                ? DriverApprovalStatus.Approved
                : DriverApprovalStatus.Rejected;

            await _context.SaveChangesAsync(cancellationToken);

            
            await _cacheService.RemoveAsync($"DriverStatus:{request.DriverId}", cancellationToken);

            string subject = request.IsApproved ? "🎉 مبروك! تم تفعيل حسابك كسائق في وصل" : "⚠️ تحديث بخصوص طلب انضمامك لتطبيق وصل";

            string body = request.IsApproved
                ? $"مرحباً {driverProfile.User.FirstName}،\nلقد تمت مراجعة مستمسكاتك وقبولك ككابتن في وصل. افتح التطبيق الآن للبدء باستقبال الرحلات!"
                : $"مرحباً {driverProfile.User.FirstName}،\nنعتذر، لم نتمكن من الموافقة على طلبك للأسباب التالية:\n{request.RejectionReason}\nيمكنك تصحيح البيانات وإعادة التقديم.";

            await _emailService.SendEmailAsync(driverProfile.User.Email, subject, body,cancellationToken);

            return ApiResponse<bool>.Success(true, request.IsApproved ? "تم قبول السائق بنجاح." : "تم رفض السائق وإرسال الإشعار.");
        }
    }
}