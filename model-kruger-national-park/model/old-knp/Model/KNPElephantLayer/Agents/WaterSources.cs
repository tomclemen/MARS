using System;
using System.Collections.Generic;
using System.Linq;
using KNPGISVectorWaterLayer;
using Mars.Interfaces.Environment;

namespace KNPElephantLayer.Agents
{
    public class WaterSources
    {
        private readonly IKNPGISVectorWaterLayer _gisWaterLayer;
        private readonly IList<Position> _waterSources;

        public WaterSources(IKNPGISVectorWaterLayer gisWaterLayer)
        {
            _waterSources = new List<Position>();
            _gisWaterLayer = gisWaterLayer;
        }

        internal void AddInitialWaterSource(double lat, double lon)
        {
            const double maxMaxDistance = double.MaxValue;
            var closestSource = _gisWaterLayer.ExploreClosestFullPotentialField(lat, lon, maxMaxDistance);
            if (closestSource != null)
            {
                _waterSources.Add(closestSource);
            }
        }

        internal Position GetClosestWaterSource(double lat, double lon)
        {
            var closestInSight = GetClosestWaterSourceInSight(lat, lon);
            if (closestInSight != null)
            {
                return closestInSight;
            }

            return _waterSources.Any()
                ? _waterSources.OrderBy(source => source.DistanceInKmTo(Position.CreateGeoPosition(lon, lat)))
                    .FirstOrDefault()
                : null;
        }

        internal Position GetClosestWaterSourceInSight(double lat, double lon)
        {
            // CHECK: Increased sense to water in order to avoid dead ends in the model

            const double agentMaxSightInKm = 25;
            var closestInSight = _gisWaterLayer.ExploreClosestFullPotentialField(lat, lon, agentMaxSightInKm);
            if (closestInSight == null)
            {
                //Console.WriteLine("No water available at: " + lat + ", " + lon);
                return null;
            }

            _waterSources.Add(closestInSight);
            return closestInSight;
        }
    }
}