using System;
using System.Collections.Generic;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Approximate coordinates for the service cities, plus the small bit of geometry the
    /// live-tracking map needs. There is no geocoder here on purpose: the map is a
    /// simulation over a fixed set of South African cities, so a lookup table is both
    /// sufficient and offline-friendly. A city we do not know falls back to the Johannesburg
    /// hub rather than throwing, so a customer-typed destination never breaks the map.
    /// </summary>
    public static class GeoService
    {
        public struct GeoPoint
        {
            public double Lat;
            public double Lng;
            public GeoPoint(double lat, double lng) { Lat = lat; Lng = lng; }
        }

        /// <summary>The depot every shipment originates from.</summary>
        public static readonly GeoPoint Hub = new GeoPoint(-26.2041, 28.0473); // Johannesburg

        private static readonly Dictionary<string, GeoPoint> Cities =
            new Dictionary<string, GeoPoint>(StringComparer.OrdinalIgnoreCase)
            {
                { "Johannesburg",   new GeoPoint(-26.2041, 28.0473) },
                { "Pretoria",       new GeoPoint(-25.7479, 28.2293) },
                { "Cape Town",      new GeoPoint(-33.9249, 18.4241) },
                { "Durban",         new GeoPoint(-29.8587, 31.0218) },
                { "Gqeberha",       new GeoPoint(-33.9608, 25.6022) },
                { "Port Elizabeth", new GeoPoint(-33.9608, 25.6022) },
                { "Bloemfontein",   new GeoPoint(-29.0852, 26.1599) },
                { "East London",    new GeoPoint(-33.0153, 27.9116) },
                { "Polokwane",      new GeoPoint(-23.9045, 29.4689) },
                { "Nelspruit",      new GeoPoint(-25.4753, 30.9694) },
                { "Kimberley",      new GeoPoint(-28.7282, 24.7499) },
            };

        /// <summary>Coordinates for a city name, falling back to the hub when unknown.</summary>
        public static GeoPoint ForCity(string city)
        {
            GeoPoint p;
            if (!string.IsNullOrWhiteSpace(city) && Cities.TryGetValue(city.Trim(), out p))
                return p;
            return Hub;
        }

        public static bool IsKnown(string city)
        {
            return !string.IsNullOrWhiteSpace(city) && Cities.ContainsKey(city.Trim());
        }

        /// <summary>A point <paramref name="fraction"/> (0..1) of the way from a to b.</summary>
        public static GeoPoint Lerp(GeoPoint a, GeoPoint b, double fraction)
        {
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;
            return new GeoPoint(
                a.Lat + (b.Lat - a.Lat) * fraction,
                a.Lng + (b.Lng - a.Lng) * fraction);
        }
    }
}
