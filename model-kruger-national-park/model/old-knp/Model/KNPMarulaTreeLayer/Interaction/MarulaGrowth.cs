using System;
using System.Runtime.CompilerServices;
using KNPMarulaTreeLayer.Agents;
using Mars.Interfaces.Agent;
using Mars.Interfaces.Environment.GeoCommon;
using MarulaLayer.Layers;

[assembly: InternalsVisibleTo("MarulaTest")]

namespace KNPMarulaTreeLayer.Interaction
{
    public class MarulaGrowth : IInteraction
    {
        private readonly double _minStemDiameterInMForFruitProduction = 0.09D;

        private readonly MarulaTree _marula;
        private readonly IKNPMarulaLayer _marulaLayer;
        private readonly double _soilWaterInPercent;
        private readonly DateTime _fruitGrowingStartDate;
        private readonly DateTime _fruitGrowingEndDate;
        private readonly DateTime _currentSimulationTime;
        private readonly IGeoCoordinate _postion;

        public MarulaGrowth(MarulaTree marula, IKNPMarulaLayer marulaLayer, Double soilWaterInPercent,
            IGeoCoordinate pos)
        {
            _marula = marula;
            _marulaLayer = marulaLayer;
            _soilWaterInPercent = soilWaterInPercent;
            _currentSimulationTime = _marulaLayer.GetCurrentSimulationTime();
            _fruitGrowingStartDate = new DateTime(_currentSimulationTime.Year, 1, 15);
            _fruitGrowingEndDate = new DateTime(_currentSimulationTime.Year, 3, 15);
            _postion = pos;
        }

        #region IInteraction Members

        public void Execute()
        {
            if (_marula.GetTreeAgeInYears() > _marula.MaxTreeAgeInYears)
            {
                _marula.KillTree();
            }

            if (_soilWaterInPercent < 13)
            {
                //Marula tree needs 13 % soil water content to grow ("Are savannas patch-dynamics systems? A landscape model")
                return;
            }

            // regeneration now depends on health class 
            var currentHealthClass = _marula.GetHealthClass();

            if (currentHealthClass == MarulaTree.HealthClass.FINE)
            {
                _marula.HealthPoints += 0.05;
            }
            else if (currentHealthClass == MarulaTree.HealthClass.ONLY_GROWTH_AND_REG)
            {
                _marula.HealthPoints += 0.03;
            }
            else
            {
                _marula.HealthPoints += 0.01;
            }

            if (_marula.HealthPoints > 100)
            {
                _marula.HealthPoints = 100;
            }

            _marula.CalculateHealthClass();

            // todo calculate once in constructor
            TimeSpan timespanSinceLastGotTicked =
                new TimeSpan(_marulaLayer.GetTickTimeSpan().Ticks * MarulaTree.TickTreeEveryNthTick);

            //let all fruits rot in april
            if (_currentSimulationTime.Month == 4)
            {
                _marula.EatableFruits = 0;
            }

            //Grow and produce fruits
            if (timespanSinceLastGotTicked >= TimeSpan.FromDays(1))
            {
                int daysPassed = timespanSinceLastGotTicked.Days;
                GrowForDays(daysPassed);
                GrowFruitsForDays(daysPassed);
            }
            else if (_marulaLayer.GetCurrentSimulationTime().Subtract(timespanSinceLastGotTicked).Day
                     != _marulaLayer.GetCurrentSimulationTime().Day)
            {
                // tickspan is smaller than 1 day - if the last tick changed the day, let the tree grow
                GrowForDays(1);
                GrowFruitsForDays(1);
            }

            // Regeneration of tree.
            // todo what is 182 ???
            _marula.HealthPoints += (100 - _marula.HealthPoints) / 182;
            if (_marula.HealthPoints > 100)
            {
                _marula.HealthPoints = 100;
            }

            _marula.CalculateBiomass();
        }

        #endregion

        /// <summary>
        ///     Linear tree growing function.
        /// </summary>
        private void GrowForDays(int days)
        {
            // check if it is growing season (November - March) 
            if (_currentSimulationTime.Month < 11 && _currentSimulationTime.Month > 3)
            {
                return;
            }

            double[] stemGrowthRates = {0.003, 0.133, 0.667}; // Stem diameter increase: 0-3mm, 0.3-13.6cm, max 80cm.
            double[] heightGrowthFactors = {0.1, 0.8, 1.0}; // Height factors: 10%, 80%, rest.
            int[] ageRanges = {1, 17, 382}; // AgeType steps: 0-1, 1-18, 18-400.
            const int threeQuarterYear = 271; // Growing period.     

            //var precipitationForMonth = _marulaLayer.GetPrecipitation();
            var precipitationForMonth = _marulaLayer.GetPrecipitation(_postion);

            // PHOTOSYNTHESIS 
            // todo: check values 
            if (precipitationForMonth >= 15 && _marula.getLeafBiomass() > 20)
            {
                _marula.HealthPoints += 0.05;
            }

            _marula.StemDiameterInM += (stemGrowthRates[(int) _marula.TreeAgeType]
                                        / (ageRanges[(int) _marula.TreeAgeType] * threeQuarterYear)) * days;
            if (_marula.HeightInM < _marula.MaxHeightInM)
            {
                _marula.HeightInM += ((heightGrowthFactors[(int) _marula.TreeAgeType] * _marula.MaxHeightInM)
                                      / (ageRanges[(int) _marula.TreeAgeType] * threeQuarterYear)) * days;
            }
        }

        private void GrowFruitsForDays(int days)
        {
            if (_marula.TreeSex == Sex.Female
                && _marula.TreeAgeType == AgeType.Adult
                && _marula.StemDiameterInM > _minStemDiameterInMForFruitProduction
                && _fruitGrowingStartDate.CompareTo(_currentSimulationTime) < 1
                && _currentSimulationTime.CompareTo(_fruitGrowingEndDate) < 1)
            {
                var currentDay = _marulaLayer.GetCurrentSimulationTime().Day;
                long fruitGrowingPeriodInTicks = _fruitGrowingEndDate.Subtract(_fruitGrowingStartDate).Ticks;
                var fruitGrowingPeriodInDays = new TimeSpan(fruitGrowingPeriodInTicks).Days;
                var fruitDropCalculator = FruitDropNormalDistribution.GetInstance();
                var percentageOfTotalFruits =
                    fruitDropCalculator.CalculatePercentageFruitDropForDay(fruitGrowingPeriodInDays, currentDay);

                var fruitProductionMultiplier = _marulaLayer.GetFruitSeasonManager(_postion)
                    .GetFruitProductionCondition()._fruitProductionMultiplier;

                _marula.EatableFruits +=
                    (int) (percentageOfTotalFruits * _marula.FruitProductionPerYear * fruitProductionMultiplier);
            }
        }
    }
}