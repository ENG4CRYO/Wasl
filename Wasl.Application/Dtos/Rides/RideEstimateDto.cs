namespace Wasl.Application.Dtos.Rides
{
    public class RideEstimateDto
    {
        public decimal EstimatedPrice { get; set; } 
        public double DistanceInKm { get; set; }   
        public string Currency { get; set; } = "IQD"; 
    }
}