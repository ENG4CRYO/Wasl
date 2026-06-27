using MediatR;
using Wasl.Application.Common;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Admin.Commands.ChangeDriverStatus
{
    public class ChangeDriverStatusCommand : IRequest<ApiResponse<bool>>
    {
        public string DriverId { get; set; } = string.Empty;
        public DriverApprovalStatus NewStatus { get; set; }
    }
}