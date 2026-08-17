using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Profile;

namespace Wasl.Application.Features.Profile.Commands.UpdateDriverProfile
{
    public class UpdateDriverProfileCommand : IRequest<ApiResponse<DriverProfileDto>>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}