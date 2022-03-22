using Bushbuckridge.Agents.Collector;
using Bushbuckridge.States;
using Mars.Components.Services.Planning.Implementation;

namespace Bushbuckridge.Goals
{
    public class ReturnHomeGoal : GoapGoal
    {
        public ReturnHomeGoal(FirewoodCollector agent) : base(agent.AgentStates)
        {
            AddOrUpdateDesiredState(FirewoodState.Home, true);
        }
    }
}