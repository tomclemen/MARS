using Bushbuckridge.Agents.Collector;
using Bushbuckridge.States;
using Mars.Components.Services.Planning.Implementation;

namespace Bushbuckridge.Goals
{
    /// <summary>
    /// This goal desires the collector to be on the firewood site and to successfully raise the wood stock
    /// </summary>
    public class RaiseWoodStockGoal : GoapGoal
    {
        public RaiseWoodStockGoal(FirewoodCollector agent) : base(agent.AgentStates, 0.15f)
        {
            AddOrUpdateDesiredState(FirewoodState.Home, false);
            AddOrUpdateDesiredState(FirewoodState.WoodstockRaised, true);
        }
    }
}