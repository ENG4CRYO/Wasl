using MediatR;
using System;
using System.Text.Json.Serialization;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands.RequestRide;

public class CreateRideRequestCommand : IRequest<ApiResponse<Guid>>
{
    public double pickupLat { get; set; }
    public double pickupLng { get; set; }
    public double dropoffLat { get; set; }
    public double dropoffLng { get; set; }

}