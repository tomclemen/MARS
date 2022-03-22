using System;
using System.Collections.Generic;
using System.Linq;
using Bushbuckridge.Actions;
using Bushbuckridge.Goals;
using Bushbuckridge.States;
using Mars.Components.Agents;
using Mars.Components.Environments;
using Mars.Components.Services.Planning.Implementation;
using Mars.Components.Services.Planning.Implementation.ActionCommons;
using Mars.Components.Services.Planning.Interfaces;
using Mars.Interfaces.Environment.GeoCommon;
using Mars.Interfaces.Layer;
using Mars.Interfaces.LIFECapabilities;
using SavannaTrees;

namespace Bushbuckridge.Agents.Collector
{
    public class FirewoodCollector : GeoAgent<FirewoodCollector>
    {
        public override FirewoodCollector AgentReference => this;
        public IGoapAgentStates AgentStates { get; }
        private readonly GoapPlanner _goapPlanner;

        private readonly SavannaLayer _treeLayer;

        public double[] StartPosition;
        public double[] CollectingPosition;

        private const int desiredWoodAmountForEachTick = 25;
        private const int woodConsumptionPerDay = 10;

        private double woodAmountInStock;
        private double woodAmountCollectedThisTick;
        public double woodAmountCollectedThisYear { get; private set; }
        public int countOfAbortAndGoHome { get; private set; }
        public int countCutShoots { get; private set; }
        public int countCutBranches { get; private set; }
        public int countGatherDeadWood { get; private set; }

        [PublishForMappingInMars]
        public FirewoodCollector(FirewoodCollectorLayer layer, RegisterAgent register, UnregisterAgent unregister,
            GeoGridEnvironment<GeoAgent<FirewoodCollector>> env, SavannaLayer treeLayer, Guid id,
            double lat, double lon) :
            base(layer, register, unregister, env, new GeoCoordinate(lat, lon), id.ToByteArray())
        {
            _treeLayer = treeLayer;
            AgentStates = new GoapAgentStates();
            AgentStates.AddOrUpdateState(FirewoodState.HasAxe, true);
            AgentStates.AddOrUpdateState(FirewoodState.HasEnoughFirewood, false);

            _goapPlanner = new GoapPlanner(AgentStates);
            var searchAndGatherWoodGoal = new RaiseWoodStockGoal(this);
            var evaluateSituationGoal = new EvaluateSituationGoal(this);
            var goHomeGoal = new ReturnHomeGoal(this);

            _goapPlanner.AddGoal(searchAndGatherWoodGoal);
            _goapPlanner.AddGoal(evaluateSituationGoal);
            _goapPlanner.AddGoal(goHomeGoal);

            searchAndGatherWoodGoal.AddAction(new CollectDeadWoodAction(this));
            searchAndGatherWoodGoal.AddAction(new CutShootsAction(this));
            searchAndGatherWoodGoal.AddAction(new CutBranchesSbAction(this));
            searchAndGatherWoodGoal.AddAction(new CutBranchesCaAnTtAction(this));

            evaluateSituationGoal.AddAction(new EvaluateAndPackWoodForTransport(this));

            goHomeGoal.AddAction(new CarryWoodHome(this));
            goHomeGoal.AddAction(new AbortAndGoHome(this));

            woodAmountInStock = layer.Random.Next(10, 30);

            AgentStates.AddOrUpdateState(FirewoodState.Home, true);
            AgentStates.AddOrUpdateState(FirewoodState.HasEnoughFirewood, false);
            AgentStates.AddOrUpdateState(FirewoodState.WoodstockRaised, false);
            AgentStates.AddOrUpdateState(FirewoodState.Evaluated, false);
        }

        /// <summary>
        /// Set position directly north of any tree
        /// </summary>
        private void initAgentPosition()
        {
            var nearestTree = _treeLayer._TreeEnvironment.GetNearest(this);
            var furthestTree = _treeLayer._TreeEnvironment.Explore(this).Last();
            StartPosition = new[] {Latitude, (nearestTree.Longitude + furthestTree.Longitude) / 2};
            CollectingPosition = StartPosition;
        }


        protected override void Reason()
        {
            if (Layer.GetCurrentTick() == 1)
            {
                initAgentPosition();
            }

            if (NextYearHasStarted())
            {
                ResetObservePropertiesForThisYear();
            }

            ConsumeWood();
            if (RequiresWoodStockRefill())
            {
                Act();
            }
        }

        private void Act()
        {
            IList<IGoapAction> actions = new List<IGoapAction>();
            do
            {
                actions = _goapPlanner.Plan();
                var goal = _goapPlanner.SelectedGoal;

                foreach (var action in actions)
                {
                    if (!action.Execute())
                    {
                        break;
                    }
                }

                if (goal is ReturnHomeGoal finishingGoal && finishingGoal.IsSatisfied())
                {
                    break;
                }
            } while (actions.Any() && (!actions.First().Equals(NoGoalReachableAction.Instance) &&
                                       !actions.First().Equals(AllGoalsSatisfiedAction.Instance)));
        }

        private void ConsumeWood()
        {
            woodAmountInStock -= woodConsumptionPerDay;
        }

        private bool RequiresWoodStockRefill()
        {
            return woodAmountInStock <= woodConsumptionPerDay * 2;
        }

        private void ResetObservePropertiesForThisYear()
        {
            woodAmountCollectedThisYear = 0;
            countOfAbortAndGoHome = 0;
            countCutShoots = 0;
            countCutBranches = 0;
            countGatherDeadWood = 0;
        }

        private bool NextYearHasStarted()
        {
            return _treeLayer.GetCurrentTick() % 366 == 0;
        }

        public bool CollectDeadWood(Tree tree)
        {
            if (tree == null) return true;
            var desired = desiredWoodAmountForEachTick - woodAmountCollectedThisTick;
            AddWoodToStock(tree.TakeDeadWoodMass(desired));
            countGatherDeadWood++;
            CollectingPosition = tree.Position;

            return true;
        }

        private void AddWoodToStock(double woodMass)
        {
            woodAmountInStock += woodMass;
            woodAmountCollectedThisTick += woodMass;
        }

        public bool CutBranch(Tree tree)
        {
            if (tree == null) return true;
            var desired = Math.Abs(desiredWoodAmountForEachTick - woodAmountCollectedThisTick);
            var amount = tree.TakeLivingWoodMass(desired);
            AddWoodToStock(amount);
            countCutBranches++;
            CollectingPosition = tree.Position;

            return true;
        }

        public bool CutShoots(Tree tree)
        {
            if (tree == null) return true;
            AddWoodToStock(tree.TakeLivingWoodMass(tree.LivingWoodMass));
            countCutShoots++;
            CollectingPosition = tree.Position;

            return true;
        }

        private bool HasEnoughFirewood()
        {
            return woodAmountCollectedThisTick >= desiredWoodAmountForEachTick;
        }

        public bool CarryWoodHome()
        {
            CollectingPosition = StartPosition;

            woodAmountCollectedThisYear += woodAmountCollectedThisTick;
            woodAmountCollectedThisTick = 0;
            return true;
        }

        public bool AbortAndGoHome()
        {
            countOfAbortAndGoHome++;
            return CarryWoodHome();
        }

        public Tree FindTree(Func<Tree, bool> func)
        {
            return _treeLayer._TreeEnvironment.GetNearest(CollectingPosition[0], CollectingPosition[1], -1, func);
        }

        public bool Evaluate()
        {
            AgentStates.AddOrUpdateState(FirewoodState.HasEnoughFirewood, HasEnoughFirewood());
            //TODO or no time anymore
            return true;
        }
    }
}