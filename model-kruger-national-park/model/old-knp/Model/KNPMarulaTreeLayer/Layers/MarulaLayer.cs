using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using KNPGISRasterPrecipitationLayer;
using KNPGISRasterVegetationLayer;
using KNPMarulaTreeLayer.Agents;
using KNPMarulaTreeLayer.Interaction;
using Mars.Components.Agents;
using Mars.Components.Environments;
using Mars.Components.Services;
using Mars.Interfaces.Environment.GeoCommon;
using Mars.Interfaces.Layer;
using Mars.Interfaces.Layer.Initialization;
using MarulaLayer.Dto;
using MarulaLayer.Layers;

[assembly: InternalsVisibleTo("MarulaTest")]

namespace KNPMarulaTreeLayer.Layers
{

    /// <summary>
    ///     Season enumeration.
    /// </summary>
    public enum Season {
        GrowingSeason,
        NonGrowingSeason
    };

    /// <summary>
    ///     Layer for the Marula trees in Skukuza.
    /// </summary>
	public class MarulaLayer : IKNPMarulaLayer {
        internal Season Season { get; private set; }
        internal DateTime CurrentSimulationTime;
        private TimeSpan _tickTimeSpan;
        private readonly string _startTime;
        private GeoGridEnvironment<GeoAgent<MarulaTree>> _geoEnvironment;
        private long _tick;
        private readonly Random _random = new Random();

        //private readonly ITemperatureTimeSeriesLayer _temperatureTimeSeriesLayer;
        private IDictionary<Guid, MarulaTree> _agents;
        private UnregisterAgent _unregisterAgent;
        private RegisterAgent _registerAgent;
        private ConcurrentBag<Guid> _treeIdsToRemove;

        private readonly Random _rand;
        private readonly KNPGISRasterPrecipitationLayer.KNPGISRasterPrecipitationLayer _precipitationLayer;
        private readonly IKNPGISRasterVegetationLayer _vegetationLayerDgvm;


        /// <summary>
        ///     Create new layer for Marula trees.
        /// </summary>
		public MarulaLayer(
            KNPGISRasterPrecipitationLayer.KNPGISRasterPrecipitationLayer precipitationLayer,
            IKNPGISRasterVegetationLayer vegetationLayerDgvm) {
			_agents = new ConcurrentDictionary<Guid, MarulaTree>();
            _startTime = DateTime.Now.ToString("s");
            _treeIdsToRemove = new ConcurrentBag<Guid>();
            _rand = new Random();
            _precipitationLayer = precipitationLayer;
            _vegetationLayerDgvm = vegetationLayerDgvm;
            
//            Console.WriteLine("[MarulaLayer] " +_vegetationLayerDgvm.GetHashCode());
//            Console.WriteLine("[MarulaLayer]: Printing obstacle map for vegetation layer constructor");
//            Console.WriteLine("top left corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-21.74,30.75)));
//            Console.WriteLine("top right corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-21.74,32.25)));
//            Console.WriteLine("bottom left corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-25.25,30.75)));
//            Console.WriteLine("bottom right corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-25.25,32.25)));
//            Console.WriteLine("in the middle" + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-24.08,31.81)));
            
        }

        #region ISteppedActiveLayer Members

        /// <summary>
        ///     Initializes this layer.
        /// </summary>
        /// <param name="layerInitData">Generic layer init data object. Not used here!</param>
        /// <param name="regHndl">Delegate for agent registration function.</param>
        /// <param name="unregHndl">Delegate for agent unregistration function.</param>
        /// <returns>Initialization success flag.</returns>
        public bool InitLayer(TInitData layerInitData, RegisterAgent regHndl, UnregisterAgent unregHndl) {
            //var mcast = MulticastAddressGenerator.GetIPv4MulticastAddress
            //    (typeof(MarulaLayer).ToString() + layerInitData.SimulationId);
            //_environment = new DistributedESC();//(null, new MessagingService(mcast, 24567),1000);
            _geoEnvironment = new GeoGridEnvironment<GeoAgent<MarulaTree>>(-22.1593, -25.7295, 30.7237, 32.232578, 1000);
            CurrentSimulationTime = layerInitData.SimulationStartPointDateTime.Value;
            _tickTimeSpan = layerInitData.OneTickTimeSpan.Value;
            _registerAgent = regHndl;
            _unregisterAgent = unregHndl;
            CalculateSeason();

            var agentInitConfig = layerInitData.AgentInitConfigs.First();

            // try to initialize from SHUTTLE data
            if (agentInitConfig.AgentInitParameters == null) {
                throw new ArgumentException("Shuttle data for marula tree initialization is null");
            }

            //var am = new AgentManager<MarulaTree>();
            _agents = AgentManager.GetAgentsByAgentInitConfig<MarulaTree>(agentInitConfig, regHndl, unregHndl,
                new List<ILayer> { this, _precipitationLayer, _vegetationLayerDgvm }, _geoEnvironment,-1);

            // register agents with shadowingService
            //_marulaAgentShadowingService.RegisterRealAgents(_agents.Values.ToArray());

            return true;

        }

        public DateTime GetCurrentSimulationTime() {
            return CurrentSimulationTime;
        }

        public Season GetSeason() {
            return Season;
        }

        public TimeSpan GetTickTimeSpan() {
            return _tickTimeSpan;
        }

        public List<MarulaTree> Explore(double lat, double lon, double radius, int maxResults = -1)
		{
			var marulas = new List<MarulaTree>();
			var agents = _geoEnvironment.Explore(lat, lon, radius, maxResults);
			foreach (var agent in agents) marulas.Add(agent.AgentReference);
			return marulas;
		}


        public MarulaDto PlantTree(double lat, double lon) {
            if(_rand.Next(0,100) <= 3.5){
                // Get random sex of tree (male: 55 %).
                var rnd = _random.Next(100);
                var treeSex = (rnd < 55) ? Sex.Male : Sex.Female;

                var newMarula =  new MarulaTree(this, _registerAgent, _unregisterAgent, _geoEnvironment, 
                    _precipitationLayer,_vegetationLayerDgvm,
                    Guid.NewGuid(), lat, lon, 0.001, 1, 0.01, (int) treeSex);
                // add to internal dictionary
                _agents.Add(newMarula.ID, newMarula);
                //register agent with shadowingService
                //_marulaAgentShadowingService.RegisterRealAgent(newMarula);
                return newMarula.ToDto();
            }
            return null;
        }


        /// <summary>
        ///     Returns the current tick.
        /// </summary>
        /// <returns>Current tick value.</returns>
        public long GetCurrentTick() {
            return _tick;
        }

        public double? GetPrecipitation(IGeoCoordinate pos)
        {
            return _precipitationLayer.GetValue(pos);
        }

        /// <summary>
        ///     Sets the current tick. This function is called by the RTE manager in each tick.
        /// </summary>
        /// <param name="currentTick">current tick value.</param>
        public void SetCurrentTick(long currentTick) {
            _tick = currentTick;
            CurrentSimulationTime = CurrentSimulationTime.Add(_tickTimeSpan);
			//FruitSeasonManager.SetCurrentPrecipitation(CurrentSimulationTime, pos);
        }


        /// <summary>
        ///     Post tick execution.
        /// </summary>
        public void PostTick() {
            CalculateSeason();
            // cleanup
            foreach (var guid in _treeIdsToRemove) {
                _agents.Remove(guid);
            }
            _treeIdsToRemove = new ConcurrentBag<Guid>();
        }


        public void Tick() {}

        public void PreTick()
        {
//            if (_tick == 1)
//            {
//                Console.WriteLine("[MarulaLayer]: Printing obstacle map for vegetation layer (Tick 1)");
//                Console.WriteLine("top left corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-21.74,30.75)));
//                Console.WriteLine("top right corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-21.74,32.25)));
//                Console.WriteLine("bottom left corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-25.25,30.75)));
//                Console.WriteLine("bottom right corner: " + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-25.25,32.25)));
//                Console.WriteLine("in the middle" + _vegetationLayerDgvm.GetCellRating(new GeoCoordinate(-24.08,31.81)));
//            }
        }

        public void RemoveTree(Guid treeId) {
            _treeIdsToRemove.Add(treeId);
        }

        #endregion


        /// <summary>
        ///     Calculate the current season based on the tick counter.
        /// </summary>
        internal void CalculateSeason() {
            //growing season from 1st November to 1st March
            if (CurrentSimulationTime.Month < 3 || CurrentSimulationTime.Month > 10) {
                Season = Season.GrowingSeason;
            }
            else {
                Season = Season.NonGrowingSeason;
            }
        }

		public FruitSeasonManager GetFruitSeasonManager(IGeoCoordinate pos)
		{
		    return new FruitSeasonManager(this, _precipitationLayer, pos, CurrentSimulationTime);
		}
	}

}