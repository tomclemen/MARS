using System;
using System.Collections.Generic;
using KNPGISRasterPrecipitationLayer;
using Mars.Interfaces.Environment.GeoCommon;
using MarulaLayer.Layers;

namespace KNPMarulaTreeLayer.Interaction
{
    /// <summary>
    /// Handles fruit seasons depending on the precipitation.
    /// Marula trees can query if the circumstances are good for fruit production
    /// </summary>
    public class FruitSeasonManager
    {
        public class FruitProcutionConditions
        {
            public static FruitProcutionConditions Good = new FruitProcutionConditions(1);
            public static FruitProcutionConditions Limited = new FruitProcutionConditions(0.1);
            public static FruitProcutionConditions Bad = new FruitProcutionConditions(0);

            public double _fruitProductionMultiplier { get; set; }

            private FruitProcutionConditions(double fruitProductionMultiplier)
            {
                _fruitProductionMultiplier = fruitProductionMultiplier;
            }
        }

        private KNPGISRasterPrecipitationLayer.KNPGISRasterPrecipitationLayer _precipitationLayer;

        private IKNPMarulaLayer _marulaLayer;

        //keys represented by months - 1 = january, 2 = february...
        private Dictionary<int, double?> precipitationValuesByMonth = new Dictionary<int, double?>();
        private readonly IGeoCoordinate _position;

        public FruitSeasonManager(IKNPMarulaLayer marulaLayer, KNPGISRasterPrecipitationLayer.KNPGISRasterPrecipitationLayer precipitationLayer,
            IGeoCoordinate position, DateTime currentSimulationTime)
        {
            _precipitationLayer = precipitationLayer;
            _marulaLayer = marulaLayer;
            _position = position;
            precipitationValuesByMonth[currentSimulationTime.Month] = _precipitationLayer.GetValue(_position);
        }

//		public void SetCurrentPrecipitation(DateTime currentSimulationTime, IGeoCoordinate pos)
//		{
//			var precipitationOfCurrentMonth = _precipitationLayer.GetValue(pos);
//			precipitationValuesByMonth[currentSimulationTime.Month] = precipitationOfCurrentMonth;
//		}

        public FruitProcutionConditions GetFruitProductionCondition()
        {
            var currentMonth = _marulaLayer.GetCurrentSimulationTime().Month;
            if (currentMonth > 3)
            {
                throw new ArgumentException(
                    "FruitProductionCondition should only be queried in fruit season (Jan, Feb and March)");
            }

            if (currentMonth == 1)
            {
                return GetFruitProductionConditionsForMonthsToCheck(new int[] {10, 11, 12, 1});
            }
            else if (currentMonth == 2 || currentMonth == 3)
            {
                return GetFruitProductionConditionsForMonthsToCheck(new int[] {10, 11, 12, 1, 2});
            }
            else
            {
                throw new InvalidOperationException("Current month should not be 0 or negative");
            }
        }

        private FruitProcutionConditions GetFruitProductionConditionsForMonthsToCheck(int[] monthsToCheck)
        {
            var valuesNotSet = false;
            var valuesMissing = false;
            var numberOfMonthWithLowPrecipitation = 0;
            foreach (int month in monthsToCheck)
            {
                if (!precipitationValuesByMonth.ContainsKey(month))
                {
                    valuesNotSet = true;
                }
                else if (precipitationValuesByMonth[month] < 0)
                {
                    valuesMissing = true;
                }
                else if (precipitationValuesByMonth[month] < 15)
                {
                    numberOfMonthWithLowPrecipitation++;
                }
            }

            //if values are missing or the simulations is just starting, we expect the conditions to be good - because we don't know what is true
            if (valuesNotSet || valuesMissing)
            {
                return FruitProcutionConditions.Good;
            }


            if (numberOfMonthWithLowPrecipitation == 2)
            {
                return FruitProcutionConditions.Bad;
            }
            else if (numberOfMonthWithLowPrecipitation == 1)
            {
                return FruitProcutionConditions.Limited;
            }
            else
            {
                return FruitProcutionConditions.Good;
            }
        }
    }
}