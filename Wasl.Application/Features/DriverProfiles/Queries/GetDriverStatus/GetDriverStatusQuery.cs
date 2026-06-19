using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.DriverProfiles.Queries.GetDriverStatus
{
    public class GetDriverStatusQuery : IRequest<ApiResponse<DriverApprovalStatus>>
    {
    }
}
