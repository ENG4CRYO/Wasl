using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Profile;

namespace Wasl.Application.Features.Profile.Queries.GetDriverProfile
{
    public class GetDriverProfileQuery : IRequest<ApiResponse<DriverProfileDto>>
    {
    }
}
