using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands.ReviewRide
{
    public class ReviewRideCommand : IRequest<ApiResponse<bool>>
    {
        public Guid RideId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
