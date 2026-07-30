using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Profile;

namespace Wasl.Application.Features.Profile.Queries.GetRiderProfile
{
    public class GetRiderProfileQuery : IRequest<ApiResponse<RiderProfileDto>>
    {
    }
}
