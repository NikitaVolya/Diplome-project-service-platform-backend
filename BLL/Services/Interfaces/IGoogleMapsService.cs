using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IGoogleMapsService
    {
        Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(string address);
        Task<(double DistanceKm, double DurationMinutes)?> GetRouteDetailsAsync(double originLat, double originLng, double destLat, double destLng);
    }
}
