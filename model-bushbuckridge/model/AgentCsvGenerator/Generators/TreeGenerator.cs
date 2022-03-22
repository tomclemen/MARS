using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using AgentCsvGenerator.Config;

namespace AgentCsvGenerator.Generators
{
    public class Species
    {
        public string Name { get; }
        public float SeedlingsPerHa { get; }
        public float JuvenilesPerHa { get; }
        public float AdultPerHa { get; }
        public int MinAdultDiameter { get; }

        public Species(string name, float seedlingsPerHa, float juvenilesPerHa, float adultPerHa, int minAdultDiameter)
        {
            Name = name;
            SeedlingsPerHa = seedlingsPerHa;
            JuvenilesPerHa = juvenilesPerHa;
            AdultPerHa = adultPerHa;
            MinAdultDiameter = minAdultDiameter;
        }
    }

    public class TreeGenerator
    {
        private readonly TreeRasterGenerator _raster;
        private static readonly Random Random = new Random();

        public TreeGenerator(TreeRasterGenerator raster)
        {
            _raster = raster;
        }

        public string Generate(IEnumerable<Species> species)
        {
            var result = new StringBuilder();
            result.AppendLine(string.Join(AgentCsvGenerator.Delimiter, "lat", "lon", "species", "diameter", "raster"));

            for (var rasterLonIndex = 0; rasterLonIndex < _raster.rasterCountLon; rasterLonIndex++)
            {
                var offsetLon = _raster.West + rasterLonIndex * _raster.CellSize;
                for (var rasterLatIndex = 0; rasterLatIndex <  _raster.rasterCountLat; rasterLatIndex++)
                {
                    if (_raster.IsEmptyRaster != null && _raster.IsEmptyRaster.Invoke(rasterLatIndex, rasterLonIndex)) continue;
                    
                    var offsetLat = _raster.South +  _raster.CellSize * (_raster.rasterCountLat - 1)  - rasterLatIndex * _raster.CellSize;
                    foreach (var aSpecies in species)
                    {
                        for (var i = 0; i < aSpecies.JuvenilesPerHa; i++)
                        {
                            result.AppendLine(GenerateTree(aSpecies, rasterLatIndex, rasterLonIndex, offsetLat,
                                offsetLon, GenerateRandomDiameter(1, 6)));
                        }

                        for (var i = 0; i < aSpecies.AdultPerHa; i++)
                        {
                            result.AppendLine(GenerateTree(aSpecies, rasterLatIndex, rasterLonIndex, offsetLat,
                                offsetLon,
                                GenerateRandomDiameter(aSpecies.MinAdultDiameter, aSpecies.MinAdultDiameter + 6)));
                        }
                    }
                }
            }

            return result.ToString();
        }

        private string GenerateTree(Species type, int rasterLatIndex, int rasterLonIndex, double offsetLat,
            double offsetLon, float diameter)
        {
            var posLon = offsetLon + _raster.CellSize * Random.NextDouble();
            var posLat = offsetLat + _raster.CellSize * Random.NextDouble();
            var raster = TreeRasterGenerator.GenRasterId(rasterLatIndex, rasterLonIndex);

            return string.Join(AgentCsvGenerator.Delimiter, posLat, posLon, type.Name, diameter, raster);
        }

        private static float GenerateRandomDiameter(int min, int max)
        {
            return (float) (Random.Next(min, max) + Random.NextDouble());
        }
    }
}