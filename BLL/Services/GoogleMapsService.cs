using BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class GoogleMapsService : IGoogleMapsService
    {
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

        public async Task<(double DistanceKm, double DurationMinutes)?> GetRouteDetailsAsync(double originLat, double originLng, double destLat, double destLng)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Google Maps API key is not configured.");
                return null;
            }

            try
            {
                var origins = $"{originLat},{originLng}";
                var destinations = $"{destLat},{destLng}";
                var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origins}&destinations={destinations}&key={_apiKey}";

                var response = await _httpClient.GetFromJsonAsync<DistanceMatrixResponse>(url);

                if (response?.Status == "OK" && response.Rows?.Count > 0)
                {
                    var element = response.Rows[0].Elements?.FirstOrDefault();
                    if (element?.Status == "OK" && element.Distance != null && element.Duration != null)
                    {
                        double distanceKm = element.Distance.Value / 1000.0;
                        double durationMinutes = element.Duration.Value / 60.0;

                        return (distanceKm, durationMinutes);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Distance Matrix request.");
                return null;
            }
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
            [property: JsonPropertyName("status")] string? Status
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
