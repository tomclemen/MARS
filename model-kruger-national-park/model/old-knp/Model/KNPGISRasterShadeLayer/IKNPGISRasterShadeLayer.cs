

using Mars.Interfaces.Environment;
using Mars.Interfaces.Layer;

namespace KNPGISRasterShadeLayer
{
    public interface IKNPGISRasterShadeLayer : IGISRasterLayer
    {
        /// Searches for nearest full potential field. Returns coordinates of field or null if
        /// not found or current field has 0 value.
        Position ExploreClosestFullPotentialField(double lat, double lon, int maxDistance);

        /// Returns if there is max potential on the requested cell
        bool HasFullPotential(double lat, double lon);
    }
}