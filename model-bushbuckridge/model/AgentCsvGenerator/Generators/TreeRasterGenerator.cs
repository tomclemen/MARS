using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using AgentCsvGenerator.Config;

namespace AgentCsvGenerator.Generators
{
    public class TreeRasterGenerator
    {
        public Func<int, int, bool> IsEmptyRaster { get; }
        private readonly AreaDefinition _area;

        public int rasterCountLon => _area.WidthInMeter / AreaDefinition.RasterLengthInMeter;
        public int rasterCountLat => _area.LengthInMeter / AreaDefinition.RasterLengthInMeter;
        public double CellSize { get; }

        public double West => _area.West;
        public double South => _area.South;
        
        public TreeRasterGenerator(AreaDefinition area , Func<int, int, bool> isEmptyRaster = null)
        {
            IsEmptyRaster = isEmptyRaster;
            _area = area;

            var cellSizeX = Math.Abs(_area.East - _area.West) / rasterCountLat;
            var cellSizeY = Math.Abs(_area.North - _area.South) / rasterCountLon;

            CellSize = (cellSizeX + cellSizeY) / 2;
        }

        public string Generate()
        {
            var result = new StringBuilder();

            result.AppendLine("ncols " + rasterCountLon);
            result.AppendLine("nrows " + rasterCountLat);
            result.AppendLine("xllcorner " + West);
            result.AppendLine("yllcorner " + South);
            result.AppendLine("cellsize " + CellSize);
            result.AppendLine("nodata_value -9999");

            for (var rasterLatIndex = 0; rasterLatIndex < rasterCountLat; rasterLatIndex++)
            {
                var rows = new List<string>();
                for (var rasterLonIndex = 0; rasterLonIndex < rasterCountLon; rasterLonIndex++)
                {
                    if (IsEmptyRaster != null && IsEmptyRaster.Invoke(rasterLatIndex, rasterLonIndex))
                    {
                        rows.Add("-" + GenRasterId(rasterLatIndex, rasterLonIndex));
                    }
                    else
                    {
                        rows.Add(GenRasterId(rasterLatIndex, rasterLonIndex));
                    }
                }

                result.AppendLine(string.Join(" ", rows));
            }

            return result.ToString();
        }

        public static string GenRasterId(int rasterLatIndex, int rasterLonIndex)
        {
            return 1 + rasterLonIndex.ToString("D2") + rasterLatIndex.ToString("D2");
        }
    }
}