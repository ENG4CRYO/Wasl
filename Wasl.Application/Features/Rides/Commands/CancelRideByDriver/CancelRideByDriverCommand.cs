using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands.CancelRideByDriver
{
    public class CancelRideByDriverCommand : IRequest<ApiResponse<bool>>
    {
        public Guid RideId { get; set; }
    }
}
