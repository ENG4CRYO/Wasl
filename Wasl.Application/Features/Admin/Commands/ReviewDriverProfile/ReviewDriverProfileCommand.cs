using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Admin.Commands.ReviewDriverProfile
{
    public class ReviewDriverProfileCommand : IRequest<ApiResponse<bool>>
    {
        public string DriverId { get; set; } = string.Empty;

        public bool IsApproved { get; set; }

        public string? RejectionReason { get; set; }
    }
}