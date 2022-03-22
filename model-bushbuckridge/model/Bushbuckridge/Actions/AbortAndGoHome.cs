using Bushbuckridge.Agents.Collector;
using Bushbuckridge.States;
using Mars.Components.Services.Planning.Implementation;

namespace Bushbuckridge.Actions
{
    public class AbortAndGoHome : GoapAction
    {
        private readonly FirewoodCollector _agent;

        public AbortAndGoHome(FirewoodCollector agent) : base(agent.AgentStates, 10)
        {
            _agent = agent;

            AddOrUpdatePrecondition(FirewoodState.Home, false);

            AddOrUpdateEffect(FirewoodState.HasEnoughFirewood, false);
            AddOrUpdateEffect(FirewoodState.WoodstockRaised, false);
            AddOrUpdateEffect(FirewoodState.Evaluated, false);
            AddOrUpdateEffect(FirewoodState.Home, true);
        }

        protected override bool ExecuteAction()
        {
            return _agent.AbortAndGoHome();
        }
    }
}