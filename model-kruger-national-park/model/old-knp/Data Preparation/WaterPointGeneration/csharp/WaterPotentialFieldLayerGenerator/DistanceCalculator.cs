using System;

namespace WaterPotentialFieldLayerGenerator
{
    public class DistanceCalculator
    {
        /// <summary>
        ///     Gets the decimal degrees from meters.
        ///     The default distance of one arc second is set to 30.9 meters,
        ///     which is good for the equator. Set accordingly for other
        ///     degree of latitude.
        /// 	Default value for distance of one arc secound: http://www.esri.com/news/arcuser/0400/wdside.html
        /// </summary>
        /// <returns>The decimal degrees by meters.</returns>
        /// <param name="distanceInMeters">Distance in meters.</param>
        /// <param name="distanceOfOneArcSecond">Distance of one arc second.</param>
        public static double GetDecimalDegreesByMetersForLat(double distanceInMeters,
            double distanceOfOneArcSecond = 30.87)
        {
            var arcSeconds = distanceInMeters / distanceOfOneArcSecond;
            return arcSeconds / 60 / 60;
        }

        /// <summary>
        ///     Gets the decimal degrees from meters.
        ///     The default distance of one arc second is set to 30.9 meters,
        ///     which is good for the equator. Set accordingly for other
        ///     degree of latitude.
        /// 	Default value for distance of one arc secound: http://www.esri.com/news/arcuser/0400/wdside.html
        /// </summary>
        /// <returns>The decimal degrees by meters.</returns>
        /// <param name="distanceInMeters">Distance in meters.</param>
        /// <param name="decimalLat">Lat value of the coordinate</param>
        public static double GetDecimalDegreesByMetersForLong(double distanceInMeters, double decimalLat)
        {
            var arcSeconds = distanceInMeters / (30.87 * Math.Cos(decimalLat * Math.PI / 180));
            return arcSeconds / 60 / 60;
        }


        /// <summary>
        /// Calculates the distance between two positions in km.
        /// 
        /// Copying and Pasting from stack overflow for dummies: http://stackoverflow.com/questions/27928/calculate-distance-between-two-latitude-longitude-points-haversine-formula
        /// </summary>
        /// <returns>The distance from lat lon in km.</returns>
        /// <param name="lat1">Lat value from position one</param>
        /// <param name="lon1">Lon value from position one</param>
        /// <param name="lat2">Lat value from position two</param>
        /// <param name="lon2">Lon value from position two</param>
        public static double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            // Radius of the earth in km
            const int r = 6371;
            var dLat = Deg2Rad(lat2 - lat1);
            var dLon = Deg2Rad(lon2 - lon1);
            var a =
                    Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
                ;
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = r * c; // Distance in km
            return d;
        }

        public static double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}