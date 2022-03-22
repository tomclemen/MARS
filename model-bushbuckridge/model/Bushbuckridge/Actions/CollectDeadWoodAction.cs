using Bushbuckridge.Agents.Collector;
using Bushbuckridge.States;
using Mars.Components.Services.Planning.Implementation;
using Mars.Mathematics;
using SavannaTrees;

namespace Bushbuckridge.Actions
{
    public class CollectDeadWoodAction : GoapAction
    {
        private const int originalCost = 10;
        private const double deadMassWorthExploiting = 1;

        private readonly FirewoodCollector _agent;
        private Tree _tree;

        public CollectDeadWoodAction(FirewoodCollector agent) : base(agent.AgentStates, originalCost)
        {
            _agent = agent;

            AddOrUpdatePrecondition(FirewoodState.IsNearDeadwoodTree, true);
            AddOrUpdatePrecondition(FirewoodState.HasAxe, true);

            AddOrUpdatePrecondition(FirewoodState.HasEnoughFirewood, false);
            AddOrUpdatePrecondition(FirewoodState.WoodstockRaised, false);

            AddOrUpdateEffect(FirewoodState.WoodstockRaised, true);
            AddOrUpdateEffect(FirewoodState.Home, false);
            AddOrUpdateEffect(FirewoodState.Evaluated, false);
        }

        protected override bool ExecuteAction()
        {
            return _agent.CollectDeadWood(_tree);
        }

        public override void UpdateCost()
        {
            _tree = _agent.FindTree(tree => tree.DeadWoodMass > deadMassWorthExploiting);
            var treeFound = _tree != null;
            AgentStates.AddOrUpdateState(FirewoodState.IsNearDeadwoodTree, treeFound);

            if (treeFound)
            {
                var distance = (float) Distance.Euclidean(_agent.CollectingPosition[0], _agent.CollectingPosition[1],
                    _tree[0],
                    _tree[1]);
                Cost = originalCost * distance;
            }
        }
    }
}