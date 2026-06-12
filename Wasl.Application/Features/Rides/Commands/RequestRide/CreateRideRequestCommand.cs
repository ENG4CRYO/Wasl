using MediatR;
using System;
using System.Text.Json.Serialization;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands.RequestRide;

public class CreateRideRequestCommand : IRequest<ApiResponse<Guid>>
{
    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public double DropoffLatitude { get; set; }
    public double DropoffLongitude { get; set; }

}