using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IGoogleMapsService
    {
        Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(string address);

        Task<(double DistanceKm, double DurationMinutes)?> GetRouteDetailsAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Distance and travel time from ONE origin to MANY destinations using as few Distance Matrix
        /// requests as possible (25 destinations per request). The result has exactly one entry per
        /// destination, in the same order; an entry is null when no route could be determined.
        /// </summary>
        Task<IReadOnlyList<(double DistanceKm, double DurationMinutes)?>> GetRouteDetailsBatchAsync(
            double originLat,
            double originLng,
            IReadOnlyList<(double Lat, double Lng)> destinations,
            CancellationToken cancellationToken = default);
    }
}
