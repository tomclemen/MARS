using System;

namespace KNPMarulaTreeLayer.Interaction {

    /// <summary>
    ///     This class helps generating normal distributed random numbers.
    ///     For example: 1000 rounded return values for "new NormalDistributionGenerator(5, 5).GetNext()"
    ///     0 - times: 2
    ///     1 - times: 14
    ///     2 - times: 56
    ///     3 - times: 109
    ///     4 - times: 209
    ///     5 - times: 233 - the meanValue
    ///     6 - times: 211
    ///     7 - times: 107
    ///     8 - times: 38
    ///     9 - times: 15
    ///     10 - times: 6
    /// </summary>
    public class NormalDistributionGenerator {
        private readonly double _meanValue;
        private readonly double _standardDeviation;
        private readonly double _maximumDeviation;
        private readonly Random _rand = new Random();

        public NormalDistributionGenerator(double meanValue, double maximumDeviation) {
            _meanValue = meanValue;
            _maximumDeviation = maximumDeviation;
            //Since the normal distribution is theoretically infinite, you can't have a hard cap on your range.
            //So we cut every number that is not in the 99.73% (three standard deviations). Therefore the maximum deviation is devided by 3 to get the standard deviation.
            //Hopefully this description is formally correct ;-) Otherwise look a the example in the class documentation.
            _standardDeviation = maximumDeviation/3;
        }

        public double GetNext() {
            //code partly from http://stackoverflow.com/questions/218060/random-gaussian-variables
            double u1 = _rand.NextDouble();
            double u2 = _rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0*Math.Log(u1))*
                                   Math.Sin(2.0*Math.PI*u2);
            var random = _meanValue + _standardDeviation*randStdNormal;

            if (random < (_meanValue - _maximumDeviation)) {
                return _meanValue - _maximumDeviation;
            }
            if (random > (_meanValue + _maximumDeviation)) {
                return _meanValue + _maximumDeviation;
            }

            return random;
        }

        public double test(int days, int day) {
            var o = days/6;
            var u = days/2;
            return (1/(Math.Sqrt(2*Math.PI)* o)) * Math.Exp(-1 * (Math.Pow(day - u, 2) / (2*Math.Pow(o, 2))));
        }
    }

}