using System;

namespace KNPMarulaTreeLayer.Interaction
{

    /// <summary>
    /// The standard normal distribution is a special case of the normal distribution with a mean value of 0 and a standard distribution of 1.
    /// This is a helper class to calculate the probability of a range. For example: The probability for the range -3 to 0 is nearly 0.5 or nearly 50%.
    /// </summary>
    public class StandardNormalDistribution {

        private static StandardNormalDistribution singleton;

        private StandardNormalDistribution() {
        }

        public static StandardNormalDistribution GetInstance() {
            return singleton ?? (singleton = new StandardNormalDistribution());
        }

        /// <summary>
        /// Returns percent of population Z Between two given values as a value between 0 and 1.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="higher"></param>
        /// <returns></returns>
        public double CalculatePercentageForRange(double lower, double higher) {
            //implementation is based on the java script code of this page:
            //http://davidmlane.com/hyperstat/z_table.html
            var zp = CalculatePercentageForZ(higher) - CalculatePercentageForZ(lower);
            return Math.Round(zp*10000)/10000;
        }

        private double CalculatePercentageForZ(double z)
        {
            //Some math foo from the above referenced page. Don't question it, just use it ;)
            if (z < -7) { return 0.0; }
            if (z > 7) { return 1.0; }

            var flag = z < 0.0;

            z = Math.Abs(z);
            var b = 0.0;
            var s = Math.Sqrt(2) / 3 * z;
            var hh = .5;

            for (var i = 0; i < 12; i++)
            {
                var a = Math.Exp(-hh * hh / 9) * Math.Sin(hh * s) / hh;
                b = b + a;
                hh = hh + 1.0;
            }
            var p = .5 - b / Math.PI;
            if (!flag) { p = 1.0 - p; }
            return p;
        }
    }
}
