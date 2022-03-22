namespace KNPElephantLayer.Interactions
{
    /*public class EatMarulaFruitsAction : IInteraction
    {
        private readonly IMarulaTree _marulaTree;
        private readonly Elephant _elephant;

        public EatMarulaFruitsAction(Elephant elephant, MarulaTree marula)
        {
            _elephant = elephant;
            _marulaTree = marula;
        }

        public void Execute()
        {
            double fruitsEaten = _marulaTree.EatFruit((int) (_marulaTree.EatableFruits * 0.1 + 1),
                _elephant.SatietyIntakeHourly[_elephant.ElephantLifePeriod]);
            _elephant.HasEatenFruits = true;
            _elephant.BiomassConsumed = fruitsEaten;
            _elephant.Hydration += fruitsEaten / 10;
            _elephant.Satiety += fruitsEaten / 4;
        }
    }*/
}