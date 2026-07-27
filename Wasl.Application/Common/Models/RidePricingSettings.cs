using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Common.Models
{
    public class RidePricingSettings
    {
        public decimal BaseFare { get; set; }
        public decimal PerKmRate { get; set; }
        public decimal PerMinuteRate { get; set; }
        public decimal MinimumFare { get; set; }
        public double AverageCitySpeedKmh { get; set; }
        public decimal CompanyCommissionRate { get; set; }
    }
}
