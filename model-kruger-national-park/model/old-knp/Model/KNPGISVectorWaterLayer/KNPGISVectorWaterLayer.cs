using Mars.Components.Layers;
using Mars.Interfaces.Environment;

namespace KNPGISVectorWaterLayer
{
    public class KNPGISVectorWaterLayer : VectorLayer, IKNPGISVectorWaterLayer 
    {
        public Position ExploreClosestFullPotentialField(double lat, double lon, double maxDistance)
        {
            return GetClosestPoint(Position.CreateGeoPosition(lon,lat), maxDistance);
        }
    }
}