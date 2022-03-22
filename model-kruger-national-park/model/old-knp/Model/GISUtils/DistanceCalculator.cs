//  /*******************************************************
//   * Copyright (C) Christian Hüning - All Rights Reserved
//   * Unauthorized copying of this file, via any medium is strictly prohibited
//   * Proprietary and confidential
//   * This file is part of the MARS LIFE project, which is part of the MARS System
//   * More information under: http://www.mars-group.org
//   * Written by Christian Hüning <christianhuening@gmail.com>, 07.02.2016
//  *******************************************************/

using System;

namespace GISUtils {

    /// <summary>
    ///     Utility class which offers some handy methods for GPS distance calculations
    /// </summary>
    public static class DistanceCalculator {
        /// <summary>
        ///     Gets the decimal degrees from meters.
        ///     The default distance of one arc second is set to 30.9 meters,
        ///     which is good for the equator. Set accordingly for other
        ///     degree of latitude
        /// </summary>
        /// <returns>The decimal degrees by meters.</returns>
        /// <param name="distanceInMeters">Distance in meters.</param>
        /// <param name="distanceOfOneArcSecond">Distance of one arc second.</param>
        public static double GetDecimalDegreesByMeters(double distanceInMeters, double distanceOfOneArcSecond = 31.1) {
            var arcSeconds = distanceInMeters/distanceOfOneArcSecond;
            return arcSeconds/60/60;
        }

        public static double GetMetersByDecimalDegrees(double decDegrees, double distanceOfOneArcSecond = 31.1) {
            var arcSeconds = decDegrees*60*60;
            return arcSeconds*distanceOfOneArcSecond;
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
            var R = 6371;
            var dLat = Deg2rad(lat2 - lat1);
            var dLon = Deg2rad(lon2 - lon1);
            var a =
              Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
              Math.Cos(Deg2rad(lat1)) * Math.Cos(Deg2rad(lat2)) *
              Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
              ;
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        public static double GetDistanceFromLatLonInM(double lat1, double lon1, double lat2, double lon2)
        {
            return GetDistanceFromLatLonInKm(lat1, lon1, lat2, lon2) * 1000.0;
        }

        public static double Deg2rad(double deg)
        {
            return deg * (Math.PI / 180);
        }

        /// <summary>
        ///     Calculates a new geocoordinate based on a current position and a directed movement.
        /// </summary>
        /// <param name="lat1">Origin latitude [in degree].</param>
        /// <param name="long1">Origin longitude [in degree].</param>
        /// <param name="bearing">The bearing (compass heading, 0 - lt.360°) [in degree].</param>
        /// <param name="distance">The travelling distance [in m].</param>
        /// <param name="lat2">Output of destination latitude.</param>
        /// <param name="long2">Output of destination longitude.</param>
        public static void CalculateNewCoordinates
            (double lat1, double long1, double bearing, double distance, out double lat2, out double long2) {
            const double deg2Rad = 0.0174532925; // Degree to radians conversion.
            const double rad2Deg = 57.2957795; // Radians to degree factor.
            const double radius = 6371; // Radius of the Earth.

            // Distance is needed in kilometers, angles in radians.
            distance /= 1000;
            bearing *= deg2Rad;
            lat1 *= deg2Rad;
            long1 *= deg2Rad;

            // Perform calculation of new coordinate.
            var dr = distance/radius;
            lat2 = Math.Asin
                (Math.Sin(lat1)*Math.Cos(dr) +
                 Math.Cos(lat1)*Math.Sin(dr)*Math.Cos(bearing));
            long2 = long1 + Math.Atan2
                (Math.Sin(bearing)*Math.Sin(dr)*Math.Cos(lat1),
                    Math.Cos(dr) - Math.Sin(lat1)*Math.Sin(lat2));

            // Convert results back to degrees.
            lat2 *= rad2Deg;
            long2 *= rad2Deg;
        }
    }

}