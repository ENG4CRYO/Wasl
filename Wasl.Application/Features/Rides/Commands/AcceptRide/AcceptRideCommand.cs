using MediatR;
using System;
using System.Text.Json.Serialization;

namespace Wasl.Application.Features.Rides.Commands;

public class AcceptRideCommand : IRequest<bool>
{
    public string RideId { get; set; } = default!;
    [JsonIgnore]
    public string DriverId { get; set; } = string.Empty;
}