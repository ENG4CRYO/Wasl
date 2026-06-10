using MediatR;
using System;
using System.Text.Json.Serialization;

namespace Wasl.Application.Features.Rides.Commands.RequestRide;

public class CreateRideRequestCommand : IRequest<Guid>
{
    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public double DropoffLatitude { get; set; }
    public double DropoffLongitude { get; set; }
    [JsonIgnore]
    public string RiderId { get; set; } = string.Empty;

}