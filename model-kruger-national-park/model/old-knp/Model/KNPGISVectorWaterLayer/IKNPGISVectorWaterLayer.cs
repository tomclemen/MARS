using Mars.Interfaces.Environment;
using Mars.Interfaces.Layer;

namespace KNPGISVectorWaterLayer
{
    public interface IKNPGISVectorWaterLayer : IGISVectorLayer
    {
        /// Searches for nearest full potential field. Returns coordinates of field or null if
        /// not found or current field has 0 value.
        Position ExploreClosestFullPotentialField(double lat, double lon, double maxDistance);
    }
}