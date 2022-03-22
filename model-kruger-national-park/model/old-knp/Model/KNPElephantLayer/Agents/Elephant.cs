using System;
using System.Collections.Generic;
using System.Linq;
using Bushbuckridge.Agents.Collector;
using KNPElephantLayer.Layer;
using KNPGISRasterFenceLayer;
using KNPGISRasterShadeLayer;
using KNPGISVectorWaterLayer;
using KNPTreeQuickFindLayer;
using Mars.Components.Agents;
using Mars.Components.Environments;
using Mars.Core.SimulationManager.Entities;
using Mars.Interfaces.Environment;
using Mars.Interfaces.Layer;
using Mars.Interfaces.LIFECapabilities;
using SavannaTrees;

namespace KNPElephantLayer.Agents
{
    public class Elephant : Agent, IPositionable
    {
        #region state variables

        public Position Position { get; set; }

        public double Latitude => Position.Y;
        public double Longitude => Position.X;
        public double TargetLon { get; set; }
        public double TargetLat { get; set; }

        private int _chanceOfDeath;
        private int _hoursLived;
        private int _hoursWithoutWater;
        private int _hoursWithoutFood;
        private int _currentHourOfTheDay;
        private bool _pregnant;
        private int _pregnancyDuration;
        private ElephantLifePeriod _elephantLifePeriod;
        private readonly Random _random;
        private int AmountOfTreesToInteractWith { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public MattersOfDeath MatterOfDeath { get; private set; }
        private readonly ElephantLayer _elephantLayer;

        // ReSharper disable once MemberCanBePrivate.Global
        public int Age { get; set; }

        //Flock related
        public bool Leading { get; set; }

        private const int RandomWalkMaxDistanceInM = 7000;
        private const int RandomWalkMinDistanceInM = 20;

        // ReSharper disable once MemberCanBePrivate.Global
        public string TreeInteraction { get; set; } = "none";

        public int HerdId { get; }
        private Elephant _elephantLeader;

        //food related
        // ReSharper disable once MemberCanBePrivate.Global
        internal double Satiety { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public double BiomassCellDifference { get; internal set; }

        //water related
        // ReSharper disable once MemberCanBePrivate.Global
        public double Hydration { get; internal set; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int HoursWithoutWater { get; set; }
        private readonly WaterSources _waterSources;

        //reproduction
        //private readonly string _reproductionYearString;

        private readonly int[] _reproductionYears = {15, 40};

        //stage of life
        private readonly Dictionary<string, ElephantType> _elephantTypeMap = new Dictionary<string, ElephantType>
        {
            {"ELEPHANT_COW", ElephantType.ElephantCow},
            {"ELEPHANT_BULL", ElephantType.ElephantBull},
            {"ELEPHANT_CALF", ElephantType.ElephantCalf},
            {"ELEPHANT_NEWBORN", ElephantType.ElephantNewborn}
        };

        //water consumption
        private readonly Dictionary<ElephantLifePeriod, double> _hydrationMapDaily =
            new Dictionary<ElephantLifePeriod, double>
            {
                {ElephantLifePeriod.Calf, 140.0}, // 140 liters a day
                {ElephantLifePeriod.Adolescent, 190.0}, // 190 liters a day
                {ElephantLifePeriod.Adult, 200.0} // 200 liters a day
            };

        // elephants eat only 18 hours a day due to other tasks like sleeping
        // for this reason we upped the values for hourly food consumption to
        // amount of biomass used per day divided by the time they have to gain
        // that amount
        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<ElephantLifePeriod, double> SatietyIntakeHourly { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public double SatietyMultiplier { get; internal set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public int BiomassNeighbourSearchLvl { get; internal set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public int TickSearchForFood { get; internal set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public double MinDehydration { get; internal set; } = 100;


        /// <summary>
        ///     The age. The baby stage lasts from birth until the elephant has been weaned off its mother’s milk completely.
        ///     This can be anywhere between 5 and 10 years of age. Being weaned means that the calf no longer drinks milk
        ///     from its mother, but is able to live only on solid vegetation. For the first 3 to 5 years, most elephant
        ///     calves are totally dependant on their mothers for their nutrition, hygiene, migration and health.
        ///     The adolescent stage extends from the time that the elephant has been weaned (5 to 10 years of age)
        ///     until about 17 years old. It is during this stage that the elephants reach sexual maturity. This
        ///     generally occurs anywhere between 8 and 13 years of age. They do not usually begin to mate at this
        ///     adolescent stage. Adolescence is the time in which young elephants begin to break away from the main
        ///     herd. Young bulls, in particular, tend to form smaller pods of peers, known as ‘bachelor pods’. Females
        ///     are more likely to stick to the main matriarchal herd.
        ///     Adulthood starts at about 18 years of age, and the elephant has an average life expectancy of 70 years.
        ///     Although sexually mature in their early teens, elephants generally only start to mate at about 20 years
        ///     and stop bearing calves at about 50.
        /// </summary>
        ///

        //Layers
        private readonly SavannaLayer _savannaLayer;

        private readonly KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer _vegetationLayerDgvm;
        private readonly GISRasterFenceLayer _gisRasterFenceLayer;
        private readonly Temperature _temperatureLayer;
        private readonly IKNPGISRasterShadeLayer _shadeLayer;
        private readonly TreeQuickFindLayer _treeQuickFindLayer;
        private ElephantType _elephantType;
        private DroughtLayer _droughtLayer;
        private GeoHashEnvironment<Elephant> _elephantEnvironment;

        #endregion

        [PublishForMappingInMars]
        public Elephant
        (ElephantLayer layer,
            RegisterAgent registerAgent,
            UnregisterAgent unregisterAgent,
            GeoHashEnvironment<Elephant> environment,
            IKNPGISVectorWaterLayer waterPotentialLayer,
            KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer vegetationLayerDgvm,
            GISRasterFenceLayer gisRasterFenceLayer,
            SavannaLayer savannaLayer,
            DroughtLayer droughtLayer,
            Temperature temperatureTimeSeriesLayer,
            IKNPGISRasterShadeLayer shadeLayer,
            TreeQuickFindLayer treeQuickFindLayer,
            Guid id,
            double lat,
            double lon,
            int herdId,
            string elephantType,
            bool isLeading,
            double biomassCellDifference = 1.0,
            double satietyMultiplier = 1.0,
            int tickSearchForFood = 1,
            int biomassNeighbourSearchLvl = 1,
            double minDehydration = 1.0
        )
            :
            base(layer, registerAgent, unregisterAgent, id.ToByteArray())
        {
            _random = new Random(ID.GetHashCode());

            BiomassCellDifference = biomassCellDifference;
            TickSearchForFood = tickSearchForFood;
            BiomassNeighbourSearchLvl = biomassNeighbourSearchLvl;
            SatietyMultiplier = satietyMultiplier;
            MinDehydration = minDehydration;
            _elephantEnvironment = environment;

            SatietyIntakeHourly = new Dictionary<ElephantLifePeriod, double>
            {
                {
                    ElephantLifePeriod.Calf, SatietyMultiplier * 0.009444
                }, // 170 kg food a day, 7,08 kg per hour (18 hrs), biomass is provided in tons
                {ElephantLifePeriod.Adolescent, SatietyMultiplier * 0.01167}, // 210 kg food a day, 8,75 kg per hour
                {ElephantLifePeriod.Adult, SatietyMultiplier * 0.01167} // 210 kg food a day, 8,75 kg per hour
            };

            //flock
            Leading = isLeading;
            HerdId = herdId;
            _elephantType = _elephantTypeMap[elephantType];

            //Layers
            _elephantLayer = (ElephantLayer) layer;
            _savannaLayer = savannaLayer;
            _droughtLayer = droughtLayer;
            _vegetationLayerDgvm = vegetationLayerDgvm;
            _gisRasterFenceLayer = gisRasterFenceLayer;
            _temperatureLayer = temperatureTimeSeriesLayer;
            _shadeLayer = shadeLayer;
            _treeQuickFindLayer = treeQuickFindLayer;

            _hoursWithoutFood = 0;
            Satiety = _random.Next(50, 100);

            //Water
            Hydration = _random.Next(50, 150);
            _hoursWithoutWater = 0;
            _currentHourOfTheDay = 0;
            _hoursLived = _random.Next(0, 8759);


            // try to parse reproductionYears from provided string;
//            if (reproductionYears != string.Empty)
//            {
//                _reproductionYearString = reproductionYears;
//                var reproductionYearsCleared = reproductionYears.Replace("\r", "").Replace("[", "").Replace("]", "");
//                var years = reproductionYearsCleared.Split(';', ',');
//                _reproductionYears = years.Select(int.Parse).ToArray();
//            }


            //set elephants age, sex and possible pregnancy state
            switch (_elephantType)
            {
                case ElephantType.ElephantNewborn:
                    Age = 0;
                    _elephantLifePeriod = ElephantLifePeriod.Calf;
                    break;
                case ElephantType.ElephantCalf:
                    Age = _random.Next(1, 5);
                    _elephantLifePeriod = ElephantLifePeriod.Calf;
                    break;
                case ElephantType.ElephantBull:
                    Age = layer.GetNextNormalDistribution();
                    _elephantLifePeriod = GetElephantLifePeriodFromAge(Age);
                    break;
                case ElephantType.ElephantCow:
                    Age = layer.GetNextNormalDistribution();
                    _elephantLifePeriod = GetElephantLifePeriodFromAge(Age);

                    if (_reproductionYears.Contains(Age))
                    {
                        // Calculate probability of pregnancy (80%)
                        if (_random.NextDouble() <= 0.8)
                        {
                            _pregnant = true;
                            //CAUTION whatever is set here probably isn't what you expect, see other comment too
                            //random value = 5 => new calf after 17 months
                            //random value = 21 => new calf after one month
                            //random value = 22 => new in the first tick
                            _pregnancyDuration = _random.Next(0, 22);
                        }
                    }

                    // CHECK: What was the semantic reason for this else-if?
//                    else if (_reproductionYears.Contains(Age + 1))
//                    {
//                        _pregnant = true;
//                        _pregnancyDuration = 12;
//                    }

                    break;
                default:
                    Age = layer.GetNextNormalDistribution();
                    _elephantLifePeriod = GetElephantLifePeriodFromAge(Age);
                    break;
            }

            _waterSources = new WaterSources(waterPotentialLayer);

            // Leading elephant knows about single surrounding waterpoint
            if (Leading)
            {
                _waterSources.AddInitialWaterSource(lat, lon);
            }

            Position = Position.CreateGeoPosition(lon, lat);
        }

        #region reason

        protected override void Reason()
        {
            if (SimulationClock.CurrentTimePoint != null)
                _currentHourOfTheDay = SimulationClock.CurrentTimePoint.Value.Hour;
            else
                throw new NullReferenceException();

            var dehydration = CalculateDehydrationPerHourByTemperature(_hydrationMapDaily[_elephantLifePeriod]);
            Hydration -= dehydration;
            if (Hydration <= MinDehydration)
            {
                if (Hydration <= 0.0001) Hydration = 0;
                //is set to 0 when drink action is performed
                _hoursWithoutWater++;

                //The Elephant dies if it doesn't get water over a
                //period of 72 hrs
                if (_hoursWithoutWater >= 72)
                {
                    Die(MattersOfDeath.NoWater);
                    return;
                }
            }

            if (_hoursWithoutFood >= 24 && Satiety <= 0)
            {
                Die(MattersOfDeath.NoFood);
                return;
            }

            // still alive, increase day counter
            _hoursLived++;

            //monthly routine
            //check if pregnant and give birth as appropriate
            if (_hoursLived % 730 == 0 && _pregnant)
            {
                //CAUTION the currently set pregnancy duration has little effect, no calf will be born before the mother
                //was pregnant for 22 months/ her pregnancy duration counter hits 22
                if (_pregnancyDuration < 22)
                {
                    _pregnancyDuration++;
                }
                else
                {
                    _pregnancyDuration = 0;
                    _elephantLayer.SpawnCalf(_elephantLayer, Position.Latitude, Position.Longitude, HerdId,
                        BiomassCellDifference, SatietyMultiplier,
                        TickSearchForFood, BiomassNeighbourSearchLvl, MinDehydration);
                    //Console.WriteLine("Elephant calf was born at: " + Latitude + ", " + Longitude);
                }
            }

            //yearly routine
            //get older, die eventually
            if (_hoursLived == 8760)
            {
                var alive = YearlyRoutine();
                if (!alive)
                {
                    return;
                }
            }

            switch (_currentHourOfTheDay)
            {
                //3-4 hrs sleeping (Handbook): 3 am (2-3 hrs), 12 am (1-2 hrs)
                case 3:
                case 4:
                case 12:
                case 13:
                {
                    // an elephant loses fewer biomass while sleeping
                    BurnSatiety(SatietyIntakeHourly[_elephantLifePeriod] / 4.0);
                    break;
                }

                //seek shadow 
                // TODO: it would be better to make that dependable from temperature
                case 14:
                    // CHECK: Dieses Konstrukt verstehe ich nicht. Ich würde davon ausgehen, dass 
                    // shadePosition nach der Ausführung die Zellkoordinaten mit dem meisten
                    // Schatten beinhaltet. Aber ich verstehe auch Lennart's Implementierung nicht.
                    /* Janus:
                     In der Methode ExploreClosestFullPotentialField wird GetClosestCellWithValue ausgeführt, der "Value"
                     wird dort auch mit FullPotential = 100 definiert. Wieso hier statisch ein Wert (100) gewählt wurde,
                     kann ich nicht beurteilen.
                     Die von Dir vorgeschlagene Methode GetNeighbourCellWithMaxValue gibt nur einen direkten Nachbarn
                     wieder.
                     Julius: Der Wert 100 wird gewaehlt weil die Shade Datei nur binaer 0 oder 100 kennt. Durch den
                     Check auf 100 prueft Lennart ob sich an dieser Stell Schatten befindet oder nicht. Die 
                     GetNeighbourCellWithMaxValue Methode wuerde nur die 4/8 Nachbarzellen durchsuchen und diejenige
                     mit dem groesten Wert zurueck liefern. Welchen Bereich das im echten Leben abdeckt haengt davon 
                     ab wie gross so eine Zelle ist
                    */

                    var shadePosition =
                        _shadeLayer.ExploreClosestFullPotentialField(Position.Latitude, Position.Longitude, 100);

                    /* Janus:
                     Wenn kein Schatten gefunden wurde, soll doch nach wie vor Satitey verbrannt werden oder nicht?
                     Julius: Ja, dann sollte die volle Menge verbrannt werden
                    */
                    if (shadePosition == null)
                    {
                        BurnSatiety(SatietyIntakeHourly[_elephantLifePeriod]);
                        //Console.WriteLine("No shadow found around: "+ Latitude + ", " + Longitude);
                        break;
                    }

                    MoveTowardsPosition(shadePosition.Latitude, shadePosition.Longitude);


                    BurnSatiety(SatietyIntakeHourly[_elephantLifePeriod] * 0.5);
                    break;

                //drink or eat
                default:
                    // during the day the elephant consumes more biomass/ food
                    // than he burns in those hours. Goal is to give the elephant
                    // the chance to eat more biomass than he actually burns over
                    // the day

                    // TODO: elephants try to eat/drink every day, remove < 30 hydration clause? 
                    // needs rethinking:   TryToDrinkFromWaterhole -> should not consume 200
                    // notice:             TryToEatFromMarula -> also adds hydration

                    TryToEatFromVegetationLayer();

                    //if (Hydration < 30) {
                    TryToDrinkFromWaterhole();

                    // Not important for DGVM
                    //TryToEatFromMarula();
                    //}

                    BurnSatiety(SatietyIntakeHourly[_elephantLifePeriod] * 0.8);

                    //differentiate behavior for leaders and following family members
                    if (Leading)
                    {
                        LeadingElephantAction();
                    }
                    else
                    {
                        ElephantAction();
                    }

                    break;
            }


            //-------------------------------//
            //* Interact with Ulfia's trees *//
            //-------------------------------//

            var treeExploreDistance = 5000.0;
            AmountOfTreesToInteractWith = 5;
            TreeInteraction = "none";
            var currentMonth = SimulationClock.CurrentTimePoint.Value.Month;

            var treesNearby = _treeQuickFindLayer.Explore(Position.PositionArray, treeExploreDistance / 1000);
            if (!treesNearby.Any()) return;

            //change explore distance based on the current month
            switch (currentMonth)
            {
                case 3:
                    treeExploreDistance = 10000;
                    break;
                default:
                    treeExploreDistance = 5000;
                    break;
            }

            var treeAdultJuvenile = _savannaLayer._TreeEnvironment.Explore(Position.Latitude, Position.Longitude,
                treeExploreDistance,
                -1
                , tree => { return (tree.MyTreeAgeGroup == TreeAgeGroup.Adult || tree.MyTreeAgeGroup == TreeAgeGroup.Juvenile); });

            //behavior for months 7,8,9
            if ((currentMonth == 7 || currentMonth == 8 || currentMonth == 9) &&
                (_elephantType == ElephantType.ElephantBull || Leading) &&
                (_elephantLifePeriod == ElephantLifePeriod.Adult && treeAdultJuvenile.Any()))
            {
                //we have a drought
                if (_droughtLayer.HasDrought)
                {
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (_elephantType)
                    {
                        //female elephant
                        case ElephantType.ElephantCow:
                        {
                            var rand = _random.Next(2);
                            if (rand == 1)
                            {
                                var chosenTrees = SelectTrees(treeAdultJuvenile);

                                foreach (var tree in chosenTrees)
                                {
                                    TakeLivingWoodMass(tree, 20, 35, 19, 25);
                                }
                            }
                            else
                            {
                                var foundTrees = PickOutBiggestTrees(treeAdultJuvenile, 15, 30, 10000, 20);
                                if (foundTrees.Any())
                                {
                                    var chosenTrees = SelectTrees(foundTrees);

                                    foreach (var tree in chosenTrees)
                                    {
                                        RingbarkTree(tree, 20, 10, 0, 15);
                                    }
                                }
                            }

                            break;
                        }

                        //male elephant
                        case ElephantType.ElephantBull:
                        {
                            var rand = _random.Next(3);

                            switch (rand)
                            {
                                case 0:
                                    var chosenTrees = SelectTrees(treeAdultJuvenile);

                                    foreach (var tree in chosenTrees)
                                    {
                                        TakeLivingWoodMass(tree, 20, 35, 19, 25);
                                    }

                                    break;

                                case 1:
                                    var foundTrees = PickOutBiggestTrees(treeAdultJuvenile,
                                        15, 30, 10000, 20);
                                    if (foundTrees.Any())
                                    {
                                        chosenTrees = SelectTrees(foundTrees);

                                        foreach (var foundTree in chosenTrees)
                                        {
                                            RingbarkTree(foundTree, 20, 10, 0, 15);
                                        }
                                    }

                                    break;

                                case 2:
                                    var random = _random.Next(100);
                                    if (random < 5)
                                    {
                                        chosenTrees = SelectTrees(treeAdultJuvenile);
                                        var chosenTree = chosenTrees.First();

                                        switch (chosenTree.Species)
                                        {
                                            case "an":
                                                if (chosenTree.StemDiameter < 10)
                                                    PushTree(chosenTree);
                                                break;
                                            case "sb":
                                                if (chosenTree.StemDiameter < 40 &&
                                                    chosenTree.StemDiameter > 30)
                                                    PushTree(chosenTree);
                                                break;
                                            default:
                                                PushTree(chosenTree);
                                                break;
                                        }
                                    }

                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                //no drought
                else
                {
                    switch (currentMonth)
                    {
                        case 9:
                        {
                            var rand = _random.Next(10);
                            if (rand >= 2)
                            {
                                var chosenTrees = SelectTrees(treeAdultJuvenile);

                                foreach (var tree in chosenTrees)
                                {
                                    TakeLivingWoodMass(tree, 10, 20, 5, 12);
                                }
                            }
                            else
                            {
                                var foundTrees = PickOutBiggestTrees(treeAdultJuvenile,
                                    15, 30, 10000, 20);
                                if (foundTrees.Any())
                                {
                                    var chosenTrees = SelectTrees(foundTrees);

                                    foreach (var foundTree in chosenTrees)
                                    {
                                        RingbarkTree(foundTree, 10, 5, 0, 7);
                                    }
                                }
                            }

                            break;
                        }

                        default:
                        {
                            var chosenTrees = SelectTrees(treeAdultJuvenile);

                            foreach (var tree in chosenTrees)
                            {
                                if (tree.MyTreeAgeGroup == TreeAgeGroup.Juvenile)
                                {
                                    TakeLivingWoodMass(tree, 10, 20, 5, 12);
                                }
                            }

                            break;
                        }
                    }
                }
            }
            //behavior for rest of the year
            else if (_random.Next(2) > 0)
            {
                var chosenTrees = SelectTrees(treeAdultJuvenile);

                foreach (var tree in chosenTrees)
                {
                    if (tree.MyTreeAgeGroup == TreeAgeGroup.Juvenile)
                    {
                        TakeLivingWoodMass(tree, 10, 20, 5, 12);
                    }
                }
            }

            //-------------------------------//
            //*    Regular tick behavior    *//
            //-------------------------------//
        }


        double MetersToDecimalDegrees(double meters, double latitude)
        {
            return meters / (111.32 * 1000 * Math.Cos(latitude * (Math.PI / 180)));
        }

        private void LeadingElephantAction()
        {
            //todo May find water and biomass outside of the fences and tries to move towards the position

            //When thirsty and the cow has knowledge about water points

            /*if( //(HoursWithoutWater > 10) && 
                    ((Hydration < 200 && Satiety > 95) || 
                     (Hydration < 150 && Satiety > 75) ||
                     (Hydration < 100 && Satiety > 50) ||
                     (Hydration < 50  && Satiety > 20) ||
                     (Hydration < 20  && Satiety > 10) ||
                     (Hydration < 10  && Satiety >= -0.001)))
            { */
            var closestWaterSource = _waterSources.GetClosestWaterSource(Position.Latitude, Position.Longitude);

            // CHECK: HoursWithoutWater constraint added, otherwise the leading cow is looking
            // for water only

            if (closestWaterSource != null && _hoursWithoutWater > 10)
            {
                if (Position.Equals(closestWaterSource))
                {
                    //Console.WriteLine("water on same position:" + GetPosition() + " AgentId " + ID);
                }

                MoveTowardsPosition(closestWaterSource.Latitude, closestWaterSource.Longitude);
                return;
            }


            if (_elephantLayer.GetCurrentTick() % TickSearchForFood == 0)
            {
                if (_vegetationLayerDgvm.IsPointInside(Position))
                {
                    var all = _vegetationLayerDgvm.Explore(Position, double.MaxValue, 4);
                    var res = all.OrderBy(a => a.Node.Value).Last();

                    if (res.Node?.NodePosition != null)
                    {
                        var targetX = res.Node.NodePosition.X;
                        var targetY = res.Node.NodePosition.Y;

                        //Console.WriteLine($"({targetX}, {targetY})");

                        if (_gisRasterFenceLayer.IsPointInside(res.Node.NodePosition))
                        {
                            var targetLon = _vegetationLayerDgvm.LowerLeft.X +
                                            targetX * _vegetationLayerDgvm.CellWidth;
                            var targetLat = _vegetationLayerDgvm.LowerLeft.Y +
                                            targetY * _vegetationLayerDgvm.CellHeight;
                            MoveTowardsPosition(targetLat, targetLon);
                            return;
                        }
                    }
                }


                DoRandomWalk();
                return;

                // current food coordinate has max value
            }

            // everything is fine, just do random walk
            DoRandomWalk();
        }

        private void ElephantAction()
        {
            // finally move to the leading cow if distance is too large
            _elephantLeader = _elephantLayer.GetLeadingElephantByHerd(HerdId);

            //if leading cow exists, move towards it
            if (Leading || _elephantLeader == null)
            {
                DoRandomWalk();
                return;
            }

            //finally move towards leader, if path cost is reasonable
            var leaderPos = _elephantLeader.Position;
            //distance in meters
            var distanceToLeaderInMeters = Position.DistanceInKmTo(leaderPos) * 1000;

            //check if leading cow is more than 100m away
            if (distanceToLeaderInMeters > 100)
            {
                MoveTowardsPosition(leaderPos.Latitude, leaderPos.Longitude);
            }
            else
            {
                DoRandomWalk();
            }
        }

        #endregion

        #region private methods

        private void BurnSatiety(double rate)
        {
            Satiety -= rate;
        }

        private double CalculateDehydrationPerHourByTemperature(double basicWaterLossDaily)
        {
            var temperature = _temperatureLayer.GetValue(Position);
            //determine to which extend the water loss should apply
            var actualWaterLossPercentage = 100.0 / 200.0 * basicWaterLossDaily / 100;

            //calculate gap between the "normal" temperature and the current temperature
            var temperatureDiff = temperature - 22.5;

            var foundShade = _shadeLayer.HasFullPotential(Position.Latitude, Position.Longitude);
            if ((_currentHourOfTheDay == 13 || _currentHourOfTheDay == 14) && foundShade)
            {
                //shadow reduces the temperature by 10 degree at 13 and 14 o'clock
                temperatureDiff -= 10;
            }

            //manually set factor to represent the distribution minimal and maximal water loss (between 100L and 300L)
            const int litersPerDegree = 5;

            //calculate the weighted water loss as a function of temperature basic water loss
            if (Math.Abs(temperatureDiff) < 0.000001 && temperatureDiff > 0)
            {
                temperatureDiff = 1;
            }

            var weightedWaterLoss = temperatureDiff * litersPerDegree * actualWaterLossPercentage;
            if (weightedWaterLoss < 0)
            {
                weightedWaterLoss *= -1;
            }

            return (basicWaterLossDaily + weightedWaterLoss) / 24.0;
        }

        private void MoveTowardsPosition(double targetLatitude, double targetLongitude)
        {
            var distance = Position.DistanceInKmTo(Position.CreateGeoPosition(targetLongitude, targetLatitude)) * 1000;

            if (distance > RandomWalkMaxDistanceInM)
                distance = _random.Next(RandomWalkMinDistanceInM, RandomWalkMaxDistanceInM);

            var bearing = Position.GetBearing(Position.CreateGeoPosition(targetLongitude, targetLatitude));
            for (var i = 0; i < 3; i++)
            {
                if (distance > RandomWalkMaxDistanceInM)
                    distance = _random.Next(RandomWalkMinDistanceInM, RandomWalkMaxDistanceInM);

                var calculatedCoordinate = Position.CalculateRelativePosition(bearing, distance);

                if (_gisRasterFenceLayer.IsPointInside(calculatedCoordinate))
                {
                    TargetLat = targetLatitude;
                    TargetLon = targetLongitude;
                    Position = _elephantEnvironment.MoveToPosition(this, calculatedCoordinate.Latitude,
                        calculatedCoordinate.Longitude);

                    return;
                }
            }
        }

        private void DoRandomWalk()
        {
            var distance = _random.Next(RandomWalkMinDistanceInM, RandomWalkMaxDistanceInM);

            // CHECK: Avoid getting stuck near the fence. 
            //        Increase bearing each retry
            const int retries = 10;
            for (var i = 1; i <= retries; i++)
            {
                var bearing = _random.Next(0, 360);
                //var bearing = (_random.Next(-20 * i, 20 * i)) % 360;

                var newPos = Position.GetRelativePosition(bearing, distance);

                if (_gisRasterFenceLayer.IsPointInside(newPos))
                {
                    MoveTowardsPosition(newPos.Latitude, newPos.Longitude);
                }
            }
        }

        private void TryToEatFromVegetationLayer()
        {
            var biomassTaken = 0.0;
            if (_vegetationLayerDgvm.Extent.Contains(Position.ToCoordinate()))
            {
                if (_vegetationLayerDgvm.GetValue(Position) >= SatietyIntakeHourly[_elephantLifePeriod])
                {
                    _vegetationLayerDgvm.SubtractFromField(Position, SatietyIntakeHourly[_elephantLifePeriod]);
                    biomassTaken = SatietyIntakeHourly[_elephantLifePeriod];
                }
            }

            if (!(biomassTaken > 0))
            {
                _hoursWithoutFood++;
                return;
            }

            Satiety += biomassTaken; // in tons
            _hoursWithoutFood = 0;
        }

        private void TryToDrinkFromWaterhole()
        {
            var waterPoint = _waterSources.GetClosestWaterSourceInSight(Position.Latitude, Position.Longitude);

            //todo Elephants should be close to water when drinking
            if (waterPoint != null)
            {
                var distance = Position.DistanceInKmTo(waterPoint);
                if (distance <= 500)
                {
                    // TODO: take value from waterhole/river layer
                    Hydration = _hydrationMapDaily[_elephantLifePeriod];
                    HoursWithoutWater = 0;
                }
            }
        }


//        private void TryToEatFromMarula()
//        {
//            // look for marula trees
//            var marulas = _marulaLayer.Explore(Latitude, Longitude, 1000, 3);
//
//            // SensorArray.Get<MarulaSensor, List<MarulaDto>>();
//            if (marulas == null || !marulas.Any()) return;
//
//            // get tree with fruits if any
//            var fruityMarula = marulas.FirstOrDefault(m => m.AgentReference.EatableFruits > 0);
//            if (fruityMarula != null)
//            {
//                _actions.AddAction(new EatMarulaFruitsAction(this, fruityMarula));
//                return;
//            }
//
//            // try to push tree and eat leaves
//            var nonSeedling = marulas.FirstOrDefault(m => m.AgentReference.TreeAgeType != AgeType.Seedling);
//            if (nonSeedling != null)
//            {
//                _actions.AddAction(new EatLeavesAction(this, nonSeedling,
//                    SatietyIntakeHourly[ElephantLifePeriod]));
//                _actions.AddAction(new PushTreeAction(this, nonSeedling));
//                return;
//            }
//
//            // try to eat a seedling
//            var seedling = marulas.FirstOrDefault(m => m.AgentReference.TreeAgeType == AgeType.Seedling);
//            if (seedling != null)
//            {
//                _actions.AddAction(new EatSeedlingAction(this, seedling,
//                    SatietyIntakeHourly[ElephantLifePeriod]));
//            }
//        }

        //the elephant ages one year
        //female elephants get pregnant if they reach a
        //certain age
        //returns true if still alive
        //returns false if dead
        private bool YearlyRoutine()
        {
            _hoursLived = 0;
            Age++;

            //decide sex once the elephant reaches the adult stage
            var newLifePeriod = GetElephantLifePeriodFromAge(Age);
            if (newLifePeriod != _elephantLifePeriod)
            {
                if (newLifePeriod == ElephantLifePeriod.Adult)
                {
                    //50:50 chance of being male or female
                    if (_random.Next(2) == 0)
                        _elephantType = ElephantType.ElephantBull;
                    else
                        _elephantType = ElephantType.ElephantCow;
                }

                _elephantLifePeriod = newLifePeriod;
            }


            //died of age?
            if (Age > 55)
            {
                _chanceOfDeath = (Age - 55) * 10;
                if (_random.Next(0, 100) >= _chanceOfDeath) return true;

                Die(MattersOfDeath.Age);
                return false;
            }

            //check for possible reproduction
            if (!_reproductionYears.Contains(Age)) return true;

            if (!_elephantType.Equals(ElephantType.ElephantCow)) return true;

            _pregnant = true;

            return true;
        }

        private static ElephantLifePeriod GetElephantLifePeriodFromAge(int age)
        {
            if (age < 5)
            {
                return ElephantLifePeriod.Calf;
            }

            return age <= 18 ? ElephantLifePeriod.Adolescent : ElephantLifePeriod.Adult;
        }

        #endregion

        public void Die(MattersOfDeath mannerOfDeath)
        {
            //Console.WriteLine("Elephant: Agent died: " + mannerOfDeath + ". " + Latitude + ", " + Longitude);
            MatterOfDeath = mannerOfDeath;
            IsAlive = false;
            _elephantLayer.ElephantMap.TryRemove(ID, out _);
        }

        private void TakeLivingWoodMass(Tree tree, int anLimit, int sbLimit, int caLimit, int ttLimit)
        {
            var percent = GetPercentageForTree(tree.Species, anLimit, sbLimit, caLimit, ttLimit);
            var takenMass = (double) percent / 100 * tree.LivingWoodMass;
            tree.TakeLivingWoodMass(takenMass);
            TreeInteraction = "TakeWoodMass";
            _elephantLayer.Logger.LogWarning("Take wood mass: " + takenMass);
        }

        private void RingbarkTree(Tree tree, int anLimit, int sbLimit, int caLimit, int ttLimit)
        {
            var percent = GetPercentageForTree(tree.Species, anLimit, sbLimit, caLimit, ttLimit);

            tree.TakeBark(percent);
            TreeInteraction = "Ringbark";

            _elephantLayer.Logger.LogWarning("Ringbark tree: " + percent);
        }

        private void PushTree(Tree tree)
        {
            tree.TakePushOver();
            TreeInteraction = "PushOver";

            _elephantLayer.Logger.LogWarning("Push tree");
        }

        private int GetPercentageForTree(string species, int acLimit, int sbLimit, int caLimit, int ttLimit)
        {
            switch (species)
            {
                case "an":
                    return acLimit;
                case "sb":
                    return sbLimit;
                case "ca":
                    return caLimit;
                case "tt":
                    return ttLimit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(species));
            }
        }

//        private int GetPercentageForTree(string species, int acLimit, int sbLimit, int caLimit, int ttLimit)
//        {
//            var rand = -1;
//            switch (species)
//            {
//                case "an":
//                    rand = _random.Next(acLimit);
//                    break;
//                case "sb":
//                    rand = _random.Next(sbLimit);
//                    break;
//                case "ca":
//                    rand = _random.Next(caLimit);
//                    break;
//                case "tt":
//                    rand = _random.Next(ttLimit);
//                    break;
//                default:
//                    throw new ArgumentOutOfRangeException(nameof(species));
//            }
//
//            //_elephantLayer.Logger.LogWarning("Random number: " + rand);
//            //Console.WriteLine("Random number: " + rand);
//            return rand;
//        }

        private List<Tree> SelectTrees(IEnumerable<Tree> allTrees)
        {
            var treeCount = allTrees.Count();
            var foundTrees = new List<Tree>();

            if (treeCount == 0) return foundTrees;

            for (var i = 0; i < AmountOfTreesToInteractWith; i++)
            {
                foundTrees.Add(allTrees.ElementAt(_random.Next(treeCount)));
            }

            return foundTrees;
        }


        private List<Tree> PickOutBiggestTrees(IEnumerable<Tree> allTrees, int anMinDiameter, int sbMinDiameter,
            int caMinDiameter, int ttMinDiameter)
        {
            var selectedTrees = new List<Tree>();
            foreach (var tree in allTrees)
            {
                switch (tree.Species)
                {
                    case "an":
                        if (tree.StemDiameter > anMinDiameter)
                            selectedTrees.Add(tree);
                        break;
                    case "sb":
                        if (tree.StemDiameter > sbMinDiameter)
                            selectedTrees.Add(tree);
                        break;
                    case "ca":
                        //elephants don't like CA
                        break;
                    case "tt":
                        if (tree.StemDiameter > ttMinDiameter)
                            selectedTrees.Add(tree);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tree.Species));
                }
            }

            return selectedTrees;
        }
    }
}