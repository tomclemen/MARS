using Bushbuckridge.Agents.Collector;
using Bushbuckridge.States;
using Mars.Components.Services.Planning.Implementation;

namespace Bushbuckridge.Goals
{
    public class EvaluateSituationGoal : GoapGoal
    {
        public EvaluateSituationGoal(FirewoodCollector agent) : base(agent.AgentStates, 0.1f)
        {
            AddOrUpdateDesiredState(FirewoodState.Evaluated, true);
        }
    }
}