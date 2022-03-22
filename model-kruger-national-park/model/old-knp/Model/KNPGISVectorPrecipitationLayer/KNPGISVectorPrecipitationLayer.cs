using Mars.Components.Layers;

namespace KNPGISVectorPrecipitationLayer
{
    public class KNPGISVectorPrecipitationLayer : GISVectorLayer, IKNPGISVectorPrecipitationLayer
    {
        public double? GetMonthlyPrecipitation()
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
