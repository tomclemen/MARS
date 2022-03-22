using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using LIFE.API.Environment.GeoCommon;
using LIFE.Components.ObstacleLayer;

namespace ObstacleGenerator
{
    public class ObstacleMapCopy : IObstacleMap {

        #region PrivateInstanceVariables

        public ConcurrentDictionary<int, double> _grid;
        private long _currentTick;

        private readonly int _numberOfGridCellsX;
        private readonly int _numberOfGridCellsY;

        private static double _topLat;
        private static double _bottomLat;

        private static double _leftLong;
        private static double _rightLong;
        private readonly int _cellSizeInM;

        private readonly double _latDistance;
        private readonly double _longDistance;

        private readonly double _latDistancePerCell;
        private readonly double _longDistancePerCell;

        #endregion PrivateInstanceVariables

        public ObstacleMapCopy(double topLat, double bottomLat, double leftLong, double rightLong, int cellSizeInM) {
            _topLat = topLat;
            _bottomLat = bottomLat;
            _leftLong = leftLong;
            _rightLong = rightLong;
            _cellSizeInM = cellSizeInM;

            _latDistance = Math.Abs(_bottomLat - _topLat);
            _longDistance = Math.Abs(_rightLong - _leftLong);

            var tempDistanceTop = GetDistanceFromLatLonInKm(topLat, leftLong, topLat, rightLong);
            var missingHorizontalDistance = GetDecimalDegreesByMetersForLong
                (_cellSizeInM - (int)((tempDistanceTop * 1000) % _cellSizeInM), topLat);

            var tempDistanceLeft = GetDistanceFromLatLonInKm(topLat, leftLong, bottomLat, leftLong);
            var missingVerticalDistance = GetDecimalDegreesByMetersForLat
                (_cellSizeInM - (int)((tempDistanceLeft * 1000) % _cellSizeInM));

            rightLong = rightLong + missingHorizontalDistance;
            bottomLat = bottomLat - missingVerticalDistance;

            _latDistance = Math.Abs(bottomLat - topLat);
            _longDistance = Math.Abs(rightLong - leftLong);

            var distanceTop = GetDistanceFromLatLonInKm(topLat, leftLong, topLat, rightLong);
            //Console.WriteLine("length top: " + distanceTop);
            var distanceLeft = GetDistanceFromLatLonInKm(topLat, leftLong, bottomLat, leftLong);
            //Console.WriteLine("length left: " + distanceLeft);
            //add 1 to avoid exception based on rounding
            _numberOfGridCellsX = (int)Math.Round(((distanceTop * 1000) / _cellSizeInM), 0);
            //Console.WriteLine("Number of Grid cells x " + _numberOfGridCellsX);
            _numberOfGridCellsY = (int)Math.Round(((distanceLeft * 1000) / _cellSizeInM), 0);
            //Console.WriteLine("Number of Grid cells in y " + _numberOfGridCellsY);

            _grid = new ConcurrentDictionary<int, double>();//new double[_numberOfGridCellsX * _numberOfGridCellsY];

            _latDistancePerCell = Math.Abs(_latDistance / _numberOfGridCellsY);
            _longDistancePerCell = Math.Abs(_longDistance / _numberOfGridCellsX);
        }

        public void WriteObstacleMapToFile(StreamWriter writer)
        {
            for (var i = 0; i < _numberOfGridCellsX*_numberOfGridCellsY; i++) {
                if (i > 0 && i%_numberOfGridCellsX == 0) 
                    writer.WriteLine();
                else if(i>0)
                    writer.Write(";");
                
                double dummy;
                _grid.TryGetValue(i, out dummy);
                writer.Write(dummy);
            }
        }

        public void WriteObstacleMapToFile()
        {
            using (StreamWriter file =
                new StreamWriter(new FileStream("obstacleMap.csv", FileMode.OpenOrCreate))) {
                
                file.WriteLine("LatitudeTop="+_topLat);
                file.WriteLine("LongitudeLeft=" + _leftLong);
                file.WriteLine("LatitudeBottom=" + _bottomLat);
                file.WriteLine("LongitudeRight=" + _rightLong);
                file.WriteLine("CellSizeInM=" + _cellSizeInM);
                
                for (int i = 0; i < _numberOfGridCellsX*_numberOfGridCellsY; i++) {
                    if (i > 0 && i%_numberOfGridCellsX == 0) {
                        file.WriteLine();
                    }
                    double dummy;
                    _grid.TryGetValue(i, out dummy);
                    file.Write(dummy + ";");
                }
            }
        }

        /*
         * Raytracing on grid:
         * http://playtechs.blogspot.de/2007/03/raytracing-on-grid.html
         */
        public double GetAccumulatedPathRating(IGeoCoordinate start, IGeoCoordinate destination, double failFastThreshold = double.MaxValue)
        {
            return GetPathValueViaRaycastInGrid(start.Latitude, start.Longitude, destination.Latitude, destination.Longitude, failFastThreshold);
        }

        public double GetAccumulatedPathRating(IGeoCoordinate start, double speed, double bearing, double failFastThreshold = double.MaxValue) {
            var destination = CalculateNewCoordinates(start.Latitude, start.Longitude, bearing, speed);
            return GetPathValueViaRaycastInGrid(start.Latitude, start.Longitude, destination.Latitude, destination.Longitude, failFastThreshold);
        }

        public void AddCellRating(IGeoCoordinate position, double ratingValue)
        {
            var cell = GetCellForGps(position.Latitude, position.Longitude);
            _grid.TryAdd(cell, ratingValue);
        }

        public void ReduceCellRating(IGeoCoordinate position, double ratingValue)
        {
            var cell = GetCellForGps(position.Latitude, position.Longitude);
            _grid.AddOrUpdate(cell, 0.0, (key, oldValue) => oldValue - ratingValue);
        }

        public void SetCellRating(IGeoCoordinate position, double cellRating) {
            var cell = GetCellForGps(position.Latitude, position.Longitude);
            _grid.AddOrUpdate(cell, cellRating, (k, v) => cellRating);
        }

        public double TryTakeFromCell(IGeoCoordinate position, double amount)
        {
            throw new NotImplementedException();
        }

        public double GetCellRating(IGeoCoordinate position)
        {
            throw new NotImplementedException();
        }

        public IGeoCoordinate GetMaxValueNeighbourCoordinate(IGeoCoordinate position)
        {
            throw new NotImplementedException();
        }

        public ObstacleMapToAscDto ToAscDto()
        {
            throw new NotImplementedException();
        }

        #region privateGeoMethods

        private static double GetDecimalDegreesByMetersForLat(double distanceInMeters, double distanceOfOneArcSecond = 30.87)
        {
            var arcSeconds = distanceInMeters / distanceOfOneArcSecond;
            return arcSeconds / 60 / 60;
        }

        private static double GetDecimalDegreesByMetersForLong(double distanceInMeters, double decimalLat)
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
        private static double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            // Radius of the earth in km
            var R = 6371;
            var dLat = Deg2Rad(lat2 - lat1);
            var dLon = Deg2Rad(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        private static double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }

        private static GeoCoordinate CalculateNewCoordinates(double originLat, double originLong, double bearing, double distance) {
            const double deg2Rad = 0.0174532925; // Degree to radians conversion.
            const double rad2Deg = 57.2957795; // Radians to degree factor.
            const double radius = 6371; // Radius of the Earth.

            // Distance is needed in kilometers, angles in radians.
            distance /= 1000;
            bearing *= deg2Rad;
            originLat *= deg2Rad;
            originLong *= deg2Rad;

            // Perform calculation of new coordinate.
            double dr = distance / radius;
            var lat2 = Math.Asin(Math.Sin(originLat) * Math.Cos(dr) +
                                 Math.Cos(originLat) * Math.Sin(dr) * Math.Cos(bearing));
            var long2 = originLong + Math.Atan2(Math.Sin(bearing) * Math.Sin(dr) * Math.Cos(originLat),
                            Math.Cos(dr) - Math.Sin(originLat) * Math.Sin(lat2));

            // Convert results back to degrees.
            lat2 *= rad2Deg;
            long2 *= rad2Deg;
            return new GeoCoordinate(lat2, long2);
        }

        #endregion

        #region privateGridMethods

        /// <summary>
        /// Returns the cell for the given GPS coordinates. Returns -1 if the GPS coordinate is not in the grid
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns>Cell or -1 if the given GPS coordinate is not in the grid</returns>
        private int GetCellForGps(double lat, double lon)
        {
            var lonInGrid = Math.Abs(lon - _leftLong);
            var xPosition = (int)(lonInGrid / (_longDistance / _numberOfGridCellsX));

            var latInGrid = Math.Abs(lat - _topLat);
            var yPosition = (int)(latInGrid / (_latDistance / _numberOfGridCellsY));

            var currentCell = yPosition * _numberOfGridCellsX + xPosition;

            if (currentCell < 0 || currentCell > _numberOfGridCellsX * _numberOfGridCellsY)
            {
                return -1;
            }

            return currentCell;
        }

        private double GetPathValueViaRaycastInGrid(double latStart, double lonStart, double latDest, double lonDest, double failFastThreshold = double.MaxValue)
        {
            // get 2D Grid coords for Start
            var lonInGrid = Math.Abs(lonStart - _leftLong);
            var StartXPosition = (int)(lonInGrid / (_longDistance / _numberOfGridCellsX));

            var latInGrid = Math.Abs(latStart - _topLat);
            var StartYPosition = (int)(latInGrid / (_latDistance / _numberOfGridCellsY));

            // get 2D Grid coords for Destination
            var lonInGrid2 = Math.Abs(lonDest - _leftLong);
            var DestXPosition = (int)(lonInGrid2 / (_longDistance / _numberOfGridCellsX));

            var latInGrid2 = Math.Abs(latDest - _topLat);
            var DestYPosition = (int)(latInGrid2 / (_latDistance / _numberOfGridCellsY));

            return GetAccumulatedValueByRaytrace(StartXPosition, StartYPosition, DestXPosition, DestYPosition, failFastThreshold);
        }

        private double GetAccumulatedValueByRaytrace(int x0, int y0, int x1, int y1, double failFastThreshold = double.MaxValue)
        {
            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var x = x0;
            var y = y0;

            var x_inc = (x1 > x0) ? 1 : -1;
            var y_inc = (y1 > y0) ? 1 : -1;
            var error = dx - dy;
            dx *= 2;
            dy *= 2;

            var value = 0.0;
            for (var n = 1 + dx + dy; n > 0; --n)
            {
                if (value >= failFastThreshold) {
                    return value;
                }
                value += GetValueFromCell(x, y);

                if (error > 0)
                {
                    x += x_inc;
                    error -= dy;
                }
                else
                {
                    y += y_inc;
                    error += dx;
                }
            }
            return value;
        }

        private double GetValueFromCell(int x, int y) {
            var currentCell = y * _numberOfGridCellsX + x;
            double val;
            return _grid.TryGetValue(currentCell, out val) ? val : 0.0;
        }

        /// Translates a cell into its corrsponding geo coordinates
        private GeoCoordinate GetGpsForCell(int cell)
        {
            var cellX = cell % _numberOfGridCellsX;
            var cellY = cell / _numberOfGridCellsX;
            return new GeoCoordinate
            (_topLat - (cellY * _latDistancePerCell + _latDistancePerCell / 2),
                _leftLong + (cellX * _longDistancePerCell + _longDistancePerCell / 2));
        }

        /// Return all neighboring cells for currentCell
        private IEnumerable<int> GetNeighborCells(int currentCell)
        {
            var neighbors = new HashSet<int>();
            var upperMostRow = currentCell < _numberOfGridCellsX;
            var bottomMostRow = currentCell > _numberOfGridCellsX * (_numberOfGridCellsY - 1);
            var leftColumn = (currentCell == 0) || currentCell % _numberOfGridCellsX == 0;
            var rightColumn = (currentCell != 0) && currentCell % _numberOfGridCellsX == _numberOfGridCellsX - 1;

            if (!upperMostRow)
            {
                neighbors.Add(currentCell - _numberOfGridCellsX);
                if (!leftColumn) neighbors.Add(currentCell - _numberOfGridCellsX - 1);
                if (!rightColumn) neighbors.Add(currentCell - _numberOfGridCellsX + 1);
            }
            if (!leftColumn)
            {
                neighbors.Add(currentCell - 1);
                if (!bottomMostRow) neighbors.Add(currentCell + _numberOfGridCellsX - 1);
            }
            if (!rightColumn)
            {
                neighbors.Add(currentCell + 1);
                if (!bottomMostRow) neighbors.Add(currentCell + _numberOfGridCellsX + 1);
            }
            if (!bottomMostRow)
            {
                neighbors.Add(currentCell + _numberOfGridCellsX);
            }
            return neighbors;
        }


        #endregion


    }
}
