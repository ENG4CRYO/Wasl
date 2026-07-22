using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Dtos.Rides
{
    public class ReviewRideRequestDto
    {
        public int Rating { get; set; } 
        public string? Comment { get; set; }
    }
}
