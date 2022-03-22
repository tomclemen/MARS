using System;
using Mars.Components.Layers;
using Mars.Interfaces.Environment;

namespace KNPGISRasterFenceLayer
{
    public class GISRasterFenceLayer : GISRasterModelLayer
    {
        public bool IsPointInside(Position coord)
        {
            return base.Extent.Contains(coord.X, coord.Y) && Math.Abs(base.GetValue(coord) - 1) < 0.001;
        }
    }
}