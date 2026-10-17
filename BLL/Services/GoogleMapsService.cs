using BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class GoogleMapsService : IGoogleMapsService
    {
        // Distance Matrix limit: 25 origins or destinations per request (and 100 elements).
        private const int MaxDestinationsPerRequest = 25;

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleMapsService> _logger;

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new ArgumentNullException("Google Maps API key is not configured.");
            _logger = logger;
        }

        public async Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Google Maps API key is not configured.");
                return null;
            }

            try
            {
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";
                var response = await _httpClient.GetFromJsonAsync<GeocodeResponse>(url);

                if (response?.Status == "OK" && response.Results?.Count > 0)
                {
                    var location = response.Results[0].Geometry?.Location;
                    if (location != null)
                    {
                        return (location.Lat, location.Lng);
                    }
                }
                _logger.LogWarning("Geocoding failed for address: {Address}. Status: {Status}", address, response?.Status);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while geocoding address: {Address}", address);
                return null;
            }
        }

        public async Task<(double DistanceKm, double DurationMinutes)?> GetRouteDetailsAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng,
            CancellationToken cancellationToken = default)
        {
            var results = await GetRouteDetailsBatchAsync(
                originLat,
                originLng,
                new[] { (destLat, destLng) },
                cancellationToken);

            return results[0];
        }

        public async Task<IReadOnlyList<(double DistanceKm, double DurationMinutes)?>> GetRouteDetailsBatchAsync(
            double originLat,
            double originLng,
            IReadOnlyList<(double Lat, double Lng)> destinations,
            CancellationToken cancellationToken = default)
        {
            var results = new (double DistanceKm, double DurationMinutes)?[destinations.Count];

            if (destinations.Count == 0)
            {
                return results;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Google Maps API key is not configured.");
                return results;
            }

            var origin = FormatCoordinates(originLat, originLng);

            for (var offset = 0; offset < destinations.Count; offset += MaxDestinationsPerRequest)
            {
                var count = Math.Min(MaxDestinationsPerRequest, destinations.Count - offset);

                var destinationsParameter = string.Join(
                    "|",
                    destinations.Skip(offset).Take(count).Select(d => FormatCoordinates(d.Lat, d.Lng)));

                var url =
                    "https://maps.googleapis.com/maps/api/distancematrix/json" +
                    "?origins=" + Uri.EscapeDataString(origin) +
                    "&destinations=" + Uri.EscapeDataString(destinationsParameter) +
                    "&key=" + _apiKey;

                try
                {
                    var response = await _httpClient.GetFromJsonAsync<DistanceMatrixResponse>(url, cancellationToken);

                    if (response is null || response.Status != "OK" || response.Rows is null || response.Rows.Count == 0)
                    {
                        // REQUEST_DENIED / OVER_QUERY_LIMIT etc. end up here; the message says why.
                        _logger.LogWarning(
                            "Distance Matrix request failed. Status: {Status}. Message: {Message}",
                            response?.Status,
                            response?.ErrorMessage);
                        continue;
                    }

                    var elements = response.Rows[0].Elements;
                    if (elements is null)
                    {
                        continue;
                    }

                    for (var i = 0; i < elements.Count && i < count; i++)
                    {
                        var element = elements[i];
                        if (element.Status == "OK" && element.Distance != null && element.Duration != null)
                        {
                            results[offset + i] = (element.Distance.Value / 1000.0, element.Duration.Value / 60.0);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // The client went away: stop instead of burning the quota for nobody.
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during Distance Matrix request.");
                }
            }

            return results;
        }

        // Always a dot as the decimal separator, whatever the server culture is
        // ("50,45" with a comma would produce a broken request).
        private static string FormatCoordinates(double latitude, double longitude)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{latitude},{longitude}");
        }

        private record GeocodeResponse(
            [property: JsonPropertyName("results")] List<GeocodeResult>? Results,
            [property: JsonPropertyName("status")] string? Status
        );

        private record GeocodeResult(
            [property: JsonPropertyName("geometry")] Geometry? Geometry
        );

        private record Geometry(
            [property: JsonPropertyName("location")] Location? Location
        );

        private record Location(
            [property: JsonPropertyName("lat")] double Lat,
            [property: JsonPropertyName("lng")] double Lng
        );

        private record DistanceMatrixResponse(
            [property: JsonPropertyName("rows")] List<DistanceRow>? Rows,
            [property: JsonPropertyName("status")] string? Status,
            [property: JsonPropertyName("error_message")] string? ErrorMessage
        );

        private record DistanceRow(
            [property: JsonPropertyName("elements")] List<DistanceElement>? Elements
        );

        private record DistanceElement(
            [property: JsonPropertyName("distance")] DistanceValue? Distance,
            [property: JsonPropertyName("duration")] DistanceValue? Duration,
            [property: JsonPropertyName("status")] string? Status
        );

        private record DistanceValue(
            [property: JsonPropertyName("value")] double Value
        );
    }
}
