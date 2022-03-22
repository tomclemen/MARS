using Mars.Components.Layers;

namespace KNPGISVectorTempLayer
{
    public class KNPGISVectorTempLayer : GISVectorLayer, IKNPGISVectorTempLayer
    {
        public double? GetTemperatureForCurrentSimulationTime()
        {
            var res = GetTimeseriesDataForCurrentTick()?.ToString();

            var success = double.TryParse(res, out var result);

            if (success)
            {
                return result;
            }

            return null;
        }
    }
}
