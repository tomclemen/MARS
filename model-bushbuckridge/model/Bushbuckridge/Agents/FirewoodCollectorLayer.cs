using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bushbuckridge.Config;
using Mars.Components.Agents;
using Mars.Components.Environments;
using Mars.Components.Services;
using Mars.Interfaces.Layer;
using Mars.Interfaces.Layer.Initialization;
using SavannaTrees;

namespace Bushbuckridge.Agents.Collector
{
    public class FirewoodCollectorLayer : ISteppedActiveLayer
    {
        public readonly Random Random;
        private readonly SavannaLayer _savannaLayer;
        private readonly GeoGridEnvironment<GeoAgent<FirewoodCollector>> _environment;

        private ConcurrentDictionary<Guid, FirewoodCollector> _agents;
        private long CurrentTick { get; set; }

        public FirewoodCollectorLayer(SavannaLayer savannaLayer)
        {
            _savannaLayer = savannaLayer;
            _environment =
                new GeoGridEnvironment<GeoAgent<FirewoodCollector>>(Territory.TOP_LAT, Territory.BOTTOM_LAT,
                    Territory.LEFT_LONG, Territory.RIGHT_LONG, 1000);
            Random = new Random();
        }

        public bool InitLayer(TInitData layerInitData, RegisterAgent registerAgentHandle,
            UnregisterAgent unregisterAgentHandle)
        {
            var agentInitConfig = layerInitData.AgentInitConfigs.FirstOrDefault();
            _agents = AgentManager.GetAgentsByAgentInitConfig<FirewoodCollector>(agentInitConfig, registerAgentHandle,
                unregisterAgentHandle,
                new List<ILayer>
                {
                    _savannaLayer,
                    this
                }, _environment);

            Console.WriteLine("[FirewoodCollectorLayer]: Created Agents: " + _agents.Count);
            return true;
        }

        public long GetCurrentTick()
        {
            return CurrentTick;
        }

        public void SetCurrentTick(long currentStep)
        {
            CurrentTick = currentStep;
//            Console.WriteLine("-------------- " + currentStep + " --------------");
        }

        public void Tick()
        {
        }

        public void PreTick()
        {
        }

        public void PostTick()
        {
        }
    }
}