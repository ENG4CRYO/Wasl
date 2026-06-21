using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands.CancelRideByRider
{
    public class CancelRideByRiderCommand : IRequest<ApiResponse<bool>>
    {
        public Guid RideId { get; set; }
    }
}
