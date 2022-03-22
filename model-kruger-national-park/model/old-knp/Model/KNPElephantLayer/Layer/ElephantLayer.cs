using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bushbuckridge.Agents.Collector;
using KNPElephantLayer.Agents;
using KNPGISRasterFenceLayer;
using KNPGISRasterShadeLayer;
using KNPGISVectorWaterLayer;
using KNPTreeQuickFindLayer;
using Mars.Common.Logging;
using Mars.Components.Environments;
using Mars.Components.Services;
using Mars.Interfaces.Layer;
using Mars.Interfaces.Layer.Initialization;
using SavannaTrees;

namespace KNPElephantLayer.Layer
{
    public class ElephantLayer : ISteppedActiveLayer
    {
        private GeoHashEnvironment<Elephant> Environment { get; }
        private readonly IKNPGISVectorWaterLayer _waterPotentialLayer;
        private readonly SavannaLayer _savannaLayer;
        private readonly DroughtLayer _droughtLayer;
        private readonly KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer _vegetationLayerDgvm;
        private long _currentTick;
        public ConcurrentDictionary<Guid, Elephant> ElephantMap;
        private readonly IDictionary<int, ElephantHerd> _herdMap;
        public ILogger Logger = LoggerFactory.GetLogger(typeof(ElephantLayer));

        private readonly Temperature _temperatureLayer;
        private readonly GISRasterFenceLayer _gisRasterFenceLayer;
        private readonly IKNPGISRasterShadeLayer _shadeLayer;
        private readonly TreeQuickFindLayer _treeQuickFindLayer;

        private readonly NormalDistributionGenerator _normalDistributionGenerator;


        private RegisterAgent _registerAgent;
        private UnregisterAgent _unregisterAgent;

        public ElephantLayer
        (
            KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer vegetationLayerDgvm,
            IKNPGISVectorWaterLayer waterPotentialLayer,
            Temperature temperatureLayer,
            GISRasterFenceLayer gisRasterFenceLayer,
            SavannaLayer savannaLayer,
            DroughtLayer droughtLayer,
            TreeQuickFindLayer treeQuickFindLayer,
            IKNPGISRasterShadeLayer shadeLayer)
        {
            _waterPotentialLayer = waterPotentialLayer;
            _savannaLayer = savannaLayer;
            _droughtLayer = droughtLayer;
            _vegetationLayerDgvm = vegetationLayerDgvm;
            _treeQuickFindLayer = treeQuickFindLayer;
            ElephantMap = new ConcurrentDictionary<Guid, Elephant>();
            _herdMap = new ConcurrentDictionary<int, ElephantHerd>();

            Environment = new GeoHashEnvironment<Elephant>(false, 1000);
            _temperatureLayer = temperatureLayer;
            _gisRasterFenceLayer = gisRasterFenceLayer;
            _shadeLayer = shadeLayer;
            _normalDistributionGenerator = new NormalDistributionGenerator(35, 30);
        }

        #region IKnpElephantLayer Members

        bool ILayer.InitLayer
            (TInitData layerInitData, RegisterAgent registerAgentHandle, UnregisterAgent unregisterAgentHandle)
        {
            //params needed for calf spawn
            _registerAgent = registerAgentHandle;
            _unregisterAgent = unregisterAgentHandle;

            var agentInitConfig = layerInitData.AgentInitConfigs.FirstOrDefault();

            // initialize from SHUTTLE data
            if (agentInitConfig != null && agentInitConfig.AgentInitParameters == null)
            {
                return false;
            }

            // create all agents
            ElephantMap = AgentManager.GetAgentsByAgentInitConfig<Elephant>
            (agentInitConfig, registerAgentHandle, unregisterAgentHandle,
                new List<ILayer>
                {
                    _waterPotentialLayer,
                    this,
                    _savannaLayer,
                    _temperatureLayer,
                    _droughtLayer,
                    _shadeLayer,
                    _vegetationLayerDgvm,
                    _treeQuickFindLayer,
                    _gisRasterFenceLayer
                },
                Environment);

            Console.WriteLine("[ElephantLayer]: Created " + ElephantMap.Count + " Agents");

            // create herd objects
            var listOfHerds =
                ElephantMap.Values.AsParallel().GroupBy(elephant => elephant.HerdId).Select(grp => grp.ToList())
                    .ToList();

            Console.WriteLine("[ElephantLayer]: Created " + listOfHerds.Count + " Herds");
            Parallel.ForEach
            (listOfHerds,
                h =>
                {
                    var leader = h.FirstOrDefault(e => e.Leading);
                    if (leader == null)
                    {
                        leader = h.FirstOrDefault();
                        if (leader == null)
                        {
                            throw new Exception("There is a herd without elephants, which is impossible!");
                        }

                        leader.Leading = true;
                    }

                    var other = h.Where(e => !e.Leading).ToList();
                    _herdMap.Add(leader.HerdId, new ElephantHerd(leader.HerdId, leader, other));
                });

            Console.WriteLine("[ElephantLayer]: Filled Herds");

            return true;
        }

        public Elephant GetLeadingElephantByHerd(int herdId)
        {
            _herdMap.TryGetValue(herdId, out var herd);
            return herd?.LeadingElephant;
        }

        public long GetCurrentTick()
        {
            return _currentTick;
        }

        public void SpawnCalf(ElephantLayer elephantLayer, double latitude, double longitude, int herdId,
            double biomassCellDifference = 1.0, double satietyMultiplier = 1.0, int tickSearchForFood = 1,
            int biomassNeighbourSearchLvl = 1,
            double minDehydration = 100)
        {
            var newElephant = new Elephant
            (elephantLayer, _registerAgent, _unregisterAgent, Environment, _waterPotentialLayer
                , _vegetationLayerDgvm,_gisRasterFenceLayer, _savannaLayer, _droughtLayer, _temperatureLayer, _shadeLayer, _treeQuickFindLayer, Guid.NewGuid(),
                latitude,
                longitude, herdId
                , "ELEPHANT_NEWBORN", false, biomassCellDifference, satietyMultiplier, tickSearchForFood,
                biomassNeighbourSearchLvl, minDehydration);

            ElephantMap.TryAdd(newElephant.ID, newElephant);
        }

        public void SetCurrentTick(long currentTick)
        {
            _currentTick = currentTick;
        }

        public void Tick()
        {
        }

        public void PreTick()
        {
        }

        private void KillElephantHerd()
        {
            var herdId = _herdMap.Keys.FirstOrDefault();
            _herdMap.TryGetValue(herdId, out var myHerd);
            if (myHerd != null)
            {
                var leadingCow = myHerd.LeadingElephant;
                var otherElephants = myHerd.OtherElephants;
                leadingCow.Die(MattersOfDeath.Culling);
                ElephantMap.TryRemove(leadingCow.ID, out _);
                foreach (var el in otherElephants)
                {
                    el.Die(MattersOfDeath.Culling);
                    ElephantMap.TryRemove(el.ID, out _);
                }

                _herdMap.Remove(herdId);
            }
            else
            {
                Console.WriteLine("[ElephantLayer] error killing a herd");
            }
        }

        public void PostTick()
        {
            // CHECK: this must be harmonized to real elephant numbers

//            // culling elephants (goes on to including 1994)
//            // a year is calculated with 8766 hours to include leap years
//            // the culling quotas used for this come from the book
//            // "Elephant Management" - Scholes 2009
//            // every 3 days a herd is killed (if neccessary)
//            if (_currentTick % 72 != 0) return;
//            // 1989
//            if (_currentTick < 8766 && ElephantMap.Count > 7468)
//            {
//                KillElephantHerd();
//            }
//            // 1990
//            else if (_currentTick < 17532 && ElephantMap.Count > 7287)
//            {
//                KillElephantHerd();
//            }
//            // 1991
//            else if (_currentTick < 26298 && ElephantMap.Count > 7470)
//            {
//                KillElephantHerd();
//            }
//            // 1992
//            else if (_currentTick < 35064 && ElephantMap.Count > 7632)
//            {
//                KillElephantHerd();
//            }
//            // 1993
//            else if (_currentTick < 43830 && ElephantMap.Count > 7834)
//            {
//                KillElephantHerd();
//            }
//            // 1994
//            else if (_currentTick < 52596 && ElephantMap.Count > 7806)
//            {
//                KillElephantHerd();
//            }
        }

        public int GetNextNormalDistribution()
        {
            return (int) _normalDistributionGenerator.GetNext();
        }

        #endregion
    }

    public class NormalDistributionGenerator
    {
        private readonly double _meanValue;
        private readonly double _standardDeviation;
        private readonly double _maximumDeviation;
        private readonly Random _rand = new Random();

        public NormalDistributionGenerator(double meanValue, double maximumDeviation)
        {
            _meanValue = meanValue;
            _maximumDeviation = maximumDeviation;
            //Since the normal distribution is theoretically infinite, you can't have a hard cap on your range.
            //So we cut every number that is not in the 99.73% (three standard deviations). Therefore the maximum deviation is devided by 3 to get the standard deviation.
            //Hopefully this description is formally correct ;-) Otherwise look a the example in the class documentation.
            _standardDeviation = maximumDeviation / 3;
        }

        public double GetNext()
        {
            //code partly from http://stackoverflow.com/questions/218060/random-gaussian-variables
            var u1 = _rand.NextDouble();
            var u2 = _rand.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2);
            var random = _meanValue + _standardDeviation * randStdNormal;

            if (random < (_meanValue - _maximumDeviation))
            {
                return _meanValue - _maximumDeviation;
            }

            if (random > (_meanValue + _maximumDeviation))
            {
                return _meanValue + _maximumDeviation;
            }

            return random;
        }
    }
}