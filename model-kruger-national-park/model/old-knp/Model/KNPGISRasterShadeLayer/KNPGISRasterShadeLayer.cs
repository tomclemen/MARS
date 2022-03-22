using System;
using System.Linq;
using Mars.Components.Layers;
using Mars.Interfaces.Environment;

namespace KNPGISRasterShadeLayer
{
    public class KNPGISRasterShadeLayer : RasterLayer, IKNPGISRasterShadeLayer
    {
        private const int FullPotential = 100;

        public bool IsPointInside(Position coord)
        {
            return base.Extent.Contains(coord.X, coord.Y) && Math.Abs(base.GetValue(coord) - 1) < 0.001;
        }

        public Position ExploreClosestFullPotentialField(double lat, double lon, int maxDistance)
        {
            if (IsPointInside(Position.CreateGeoPosition(lon, lat)))
            {
                var res = Explore(Position.CreateGeoPosition(lon, lat), maxDistance).FirstOrDefault();

                if (res.Node?.NodePosition != null)
                {
                    var targetLon = LowerLeft.X + res.Node.NodePosition.X * base.CellWidth;
                    var targetLat = LowerLeft.Y + res.Node.NodePosition.Y * base.CellHeight;

                    return Position.CreateGeoPosition(targetLon, targetLat);
                }
            }

            return null;
        }

        public bool HasFullPotential(double lat, double lon)
        {
            if (Extent.Contains(lon, lat))
            {
                var value = GetValue(Position.CreateGeoPosition(lon, lat));
                return Math.Abs(FullPotential - value) < 0.00001;
            }

            return false;
        }
    }
}