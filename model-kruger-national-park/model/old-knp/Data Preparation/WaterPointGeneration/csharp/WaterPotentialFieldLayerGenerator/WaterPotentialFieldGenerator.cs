using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WaterPotentialFieldLayerGenerator
{
    /// <summary>
    /// Generates a potential field for a given csv file
    /// </summary>
    public class WaterPotentialFieldGenerator
    {
        private readonly double _latitudeTop;
        private readonly double _longitudeLeft;
        private readonly double _latitudeBottom;
        private readonly double _longitudeRight;

        private readonly int _gridLengthInMeter;
        private readonly int _potentialDepthInCells;

        private readonly double _latDistance;
        private readonly double _longDistance;
        private readonly int _numberOfGridCellsX;
        private readonly int _numberOfGridCellsY;

        private readonly double[] _potentialField;

        public WaterPotentialFieldGenerator(double latitudeTop, double longitudeLeft, double latitudeBottom,
            double longitudeRight, int gridLengthInMeter, int potentialDepthInCells)
        {
            _latitudeTop = latitudeTop;
            _longitudeLeft = longitudeLeft;
            _latitudeBottom = latitudeBottom;
            _longitudeRight = longitudeRight;

            _gridLengthInMeter = gridLengthInMeter;
            _potentialDepthInCells = potentialDepthInCells;

            var tempDistanceTop =
                DistanceCalculator.GetDistanceFromLatLonInKm(_latitudeTop, _longitudeLeft, _latitudeTop,
                    _longitudeRight);
            var missingHorizontalDistance =
                DistanceCalculator.GetDecimalDegreesByMetersForLong(
                    _gridLengthInMeter - (int) (tempDistanceTop * 1000 % _gridLengthInMeter), _latitudeTop);

            var tempDistanceLeft = DistanceCalculator.GetDistanceFromLatLonInKm(
                _latitudeTop, _longitudeLeft, _latitudeBottom, _longitudeLeft);
            var missingVerticalDistance = DistanceCalculator.GetDecimalDegreesByMetersForLat(
                _gridLengthInMeter - (int) (tempDistanceLeft * 1000 % _gridLengthInMeter));

            _longitudeRight = _longitudeRight + missingHorizontalDistance;
            _latitudeBottom = _latitudeBottom - missingVerticalDistance;

            _latDistance = Math.Abs(_latitudeBottom - _latitudeTop);
            _longDistance = Math.Abs(_longitudeRight - _longitudeLeft);

            var distanceTop = DistanceCalculator
                .GetDistanceFromLatLonInKm(_latitudeTop, _longitudeLeft, _latitudeTop, _longitudeRight);
            var distanceLeft = DistanceCalculator
                .GetDistanceFromLatLonInKm(_latitudeTop, _longitudeLeft, _latitudeBottom,
                    _longitudeLeft);
            //add 1 to avoid exception based on rounding
            _numberOfGridCellsX = (int) Math.Round(distanceTop * 1000 / _gridLengthInMeter, 0);
            _numberOfGridCellsY = (int) Math.Round(distanceLeft * 1000 / _gridLengthInMeter, 0);

            Console.WriteLine("Number of cells in x direction: " + _numberOfGridCellsX);
            Console.WriteLine("Number of cells in y direction: " + _numberOfGridCellsY);

            _potentialField = new double[_numberOfGridCellsX * _numberOfGridCellsY];
        }


        public void PrintPotentialField()
        {
            for (var i = 0; i < _numberOfGridCellsX * _numberOfGridCellsY; i++)
            {
                if (i > 0 && i % _numberOfGridCellsX == 0)
                {
                    Console.WriteLine();
                }
                var toWrite = "..";
                if (_potentialField[i] > 0)
                {
                    toWrite = _potentialField[i] == 100 ? "99" : ((int) _potentialField[i]).ToString();
                }
                Console.Write(toWrite + " ");
            }
        }

        public void InitLayerFromFile(string inputFile, int longIndex, int latIndex, char delimiter)
        {
            if (!File.Exists(inputFile)) return;
            
            var reader = new StreamReader(File.OpenRead(inputFile));

            //skip first line - the first line is normally the header
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                var values = line.Split(delimiter);
                var lon = double.Parse(values[longIndex], CultureInfo.InvariantCulture);
                var lat = double.Parse(values[latIndex], CultureInfo.InvariantCulture);
                RegisterPotential(lon, lat);
            }
        }

        private void RegisterPotential(double lon, double lat)
        {
            var lonInGrid = Math.Abs(lon - _longitudeLeft);
            var xPosition = (int) (lonInGrid / (_longDistance / _numberOfGridCellsX));

            var latInGrid = Math.Abs(lat - _latitudeTop);
            var yPosition = (int) (latInGrid / (_latDistance / _numberOfGridCellsY));

            var currentCell = yPosition * _numberOfGridCellsX + xPosition;

            if (currentCell >= _potentialField.Length) return;
            _potentialField[currentCell] = 100;
            PropagatePotential(currentCell, currentCell, _potentialDepthInCells);
        }

        private void PropagatePotential(int originalCell, int currentCell, double depthRemaining)
        {
            var neighbors = GetNeighborCells(currentCell);
            foreach (var neighbor in neighbors)
            {
                var cellX = originalCell % _numberOfGridCellsX;
                var cellY = originalCell / _numberOfGridCellsX;
                var xDistance = Math.Abs(cellX - (neighbor % _numberOfGridCellsX));
                var yDistance = Math.Abs(cellY - (neighbor / _numberOfGridCellsX));
                var neighborCellDistance = Math.Sqrt(xDistance * xDistance + yDistance * yDistance);
                var currentPotential = (_potentialDepthInCells - neighborCellDistance) / _potentialDepthInCells *
                                       100;

                if (neighbor >= _potentialField.Length || !(_potentialField[neighbor] < currentPotential) ||
                    (int) currentPotential <= 0) continue;
                _potentialField[neighbor] = currentPotential;
                PropagatePotential(originalCell, neighbor, currentPotential - 1.0);
            }
        }

        private IEnumerable<int> GetNeighborCells(int currentCell)
        {
            IList<int> neighbors = new List<int>();
            var upperMostRow = currentCell < _numberOfGridCellsX;
            var bottomMostRow = currentCell > _numberOfGridCellsX * (_numberOfGridCellsY - 1);
            var leftColumn = currentCell == 0 || currentCell % _numberOfGridCellsX == 0;

            var rightColumn = currentCell != 0 && currentCell % _numberOfGridCellsX == _numberOfGridCellsX - 1;
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

        public void WriteLayerToFile(string outputFileName)
        {
            using (var file = new StreamWriter(outputFileName))
            {
                file.WriteLine("LatitudeTop=" + _latitudeTop);
                file.WriteLine("LongitudeLeft=" + _longitudeLeft);
                file.WriteLine("LatitudeBottom=" + _latitudeBottom);
                file.WriteLine("LongitudeRight=" + _longitudeRight);
                file.WriteLine("CellSizeInM=" + _gridLengthInMeter);

                for (var i = 0; i < _numberOfGridCellsX * _numberOfGridCellsY; i++)
                {
                    if (i > 0 && i % _numberOfGridCellsX == 0)
                    {
                        file.WriteLine();
                    }

                    var currentValue = (int) _potentialField[i];
                    //don't write a semicolon for the last value of a line
                    if (i % _numberOfGridCellsX == _numberOfGridCellsX - 1)
                    {
                        file.Write(currentValue);
                    }
                    else
                    {
                        file.Write(currentValue + ";");
                    }
                }
            }
        }
    }
}