using KNPElephantLayer.Agents;
using Mars.Interfaces.Agent;

namespace KNPElephantLayer.Interactions
{
    /*class EatSeedlingAction : IInteraction
    {
        private readonly IMarulaTree _marulaTree;
        private readonly Elephant _elephant;
        private double _biomassToTake;

        public EatSeedlingAction(Elephant elephant, IMarulaTree marula, double biomassToTake)
        {
            _elephant = elephant;
            _marulaTree = marula;
            _biomassToTake = biomassToTake;
        }

        public void Execute()
        {
            double biomass = _marulaTree.EatMarulaSeedling(_biomassToTake);
            _elephant.BiomassConsumed = biomass;
            _elephant.Hydration += biomass / 2.0;
            _elephant.Satiety += biomass;
        }
    }*/
}