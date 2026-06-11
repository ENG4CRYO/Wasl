using MediatR;
using System;
using System.Text.Json.Serialization;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands;

public class AcceptRideCommand : IRequest<ApiResponse<bool>>
{
    public string RideId { get; set; } = default!;
    [JsonIgnore]
    public string DriverId { get; set; } = string.Empty;
}