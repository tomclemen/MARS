using Mars.Interfaces.Layer;

namespace KNPGISVectorPrecipitationLayer
{
    public interface IKNPGISVectorPrecipitationLayer : IGISVectorLayer
    {
        double? GetMonthlyPrecipitation();
    }
}
