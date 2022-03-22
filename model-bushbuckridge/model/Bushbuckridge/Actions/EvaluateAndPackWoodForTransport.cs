using Bushbuckridge.Agents.Collector;
using Bushbuckridge.States;
using Mars.Components.Services.Planning.Implementation;

namespace Bushbuckridge.Actions
{
    public class EvaluateAndPackWoodForTransport : GoapAction
    {
        private readonly FirewoodCollector _agent;

        public EvaluateAndPackWoodForTransport(FirewoodCollector agent) : base(agent.AgentStates, 10)
        {
            _agent = agent;

            AddOrUpdatePrecondition(FirewoodState.WoodstockRaised, true);
            AddOrUpdatePrecondition(FirewoodState.Evaluated, false);

            AddOrUpdateEffect(FirewoodState.Evaluated, true);
            AddOrUpdateEffect(FirewoodState.WoodstockRaised, false);
        }

        protected override bool ExecuteAction()
        {
            return _agent.Evaluate();
        }
    }
}