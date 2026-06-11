using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands.CompleteRide
{
    public class CompleteRideCommand : IRequest<ApiResponse<bool>>
    {
        public string RideId { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
    }
}
