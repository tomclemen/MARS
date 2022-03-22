namespace KNPMarulaTreeLayer.Interaction
{
    public class FruitDropNormalDistribution {
        private static FruitDropNormalDistribution _singleton;

        private readonly double minimum = -3;
        private readonly double maximum = 3;

        private FruitDropNormalDistribution() {
        }

        public static FruitDropNormalDistribution GetInstance() {
            return _singleton ?? (_singleton = new FruitDropNormalDistribution());
        }

        public double CalculatePercentageFruitDropForDay(int totalDays, int currentDay) {
            double partLength = (maximum - minimum)/totalDays;
            double lowerValue = minimum + (currentDay - 1)*partLength;
            double higherValue = minimum + currentDay * partLength;
            return StandardNormalDistribution.GetInstance().CalculatePercentageForRange(lowerValue, higherValue);
        }
    }
}
