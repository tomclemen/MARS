using Mars.Interfaces.Layer;

namespace KNPGISVectorTempLayer
{
    public interface IKNPGISVectorTempLayer : IGISVectorLayer
    {
        double? GetTemperatureForCurrentSimulationTime();
    }
}
