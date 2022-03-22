using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KNPGISRasterVegetationLayer;
using KNPGISRasterPrecipitationLayer;
using KNPMarulaTreeLayer.Interaction;
using Mars.Components.Agents;
using Mars.Components.Environments;
using Mars.Interfaces.Agent;
using Mars.Interfaces.Environment.GeoCommon;
using Mars.Interfaces.Layer;
using Mars.Interfaces.LIFECapabilities;
using MarulaLayer.Dto;
using MarulaLayer.Layers;

[assembly: InternalsVisibleTo("MarulaTest")]

namespace KNPMarulaTreeLayer.Agents
{
    /// <summary>
    ///     Marula tree implementation.
    /// </summary>
    public class MarulaTree : GeoAgent<MarulaTree>, IMarulaTree
    {
        private double StemBiomass { get; set; }
        private double BranchBiomass { get; set; }
        private double LeafBiomass { get; set; }

        private double TotalBiomass
        {
            get => StemBiomass + BranchBiomass + LeafBiomass;
            set
            {
                if (!Equals(value, 0.0))
                {
                    return;
                }

                StemBiomass = 0;
                BranchBiomass = 0;
                LeafBiomass = 0;
            }
        }

        private readonly IKNPMarulaLayer _marulaLayer;
        private readonly object _lock;

        internal readonly Sex TreeSex;
        internal readonly double MaxHeightInM;

        /*LeafArea = _random.Next(70, 92); 
        LeafArea += _random.NextDouble();*/

        internal readonly int FruitProductionPerYear = 3500;
        internal readonly int MaxTreeAgeInYears;
        internal static readonly int TickTreeEveryNthTick = 732;

        public enum HealthClass
        {
            FINE,
            ONLY_GROWTH_AND_REG,
            ONLY_REGENERATION
        };

        internal readonly Dictionary<HealthClass, int> HealthClassWithLowerBoundaries = new Dictionary<HealthClass, int>
        {
            {HealthClass.FINE, 66},
            {HealthClass.ONLY_GROWTH_AND_REG, 33},
            {HealthClass.ONLY_REGENERATION, 1}
        };

        public HealthClass Health = HealthClass.FINE;

        private readonly DateTime _dateOfGermination;
        private readonly Random _random = new Random();

        public double HeightInM;
        public double StemDiameterInM;
        internal double HealthPoints = 100;

        internal int _eatableFruits;
        internal AgeType AgeType;

        //layer
        private readonly KNPGISRasterPrecipitationLayer.KNPGISRasterPrecipitationLayer _precipitationLayer;
        private readonly IKNPGISRasterVegetationLayer _vegetationLayerDgvm;

        public int EatableFruits
        {
            get { return _eatableFruits; }
            internal set
            {
                if (_eatableFruits != value)
                {
                    _eatableFruits = value;
                    OnPropertyChanged("EatableFruits");
                }
            }
        }

        public AgeType TreeAgeType
        {
            get { return AgeType; }
            private set
            {
                if (AgeType != value)
                {
                    AgeType = value;
                    OnPropertyChanged("TreeAgeType");
                }
            }
        }

        /// <summary>
        ///     Create a new Marula tree agent.
        /// </summary>
        /// <param name="marulaLayer">Marula marulaLayer reference.</param>
        /// <param name="registerAgent">Agent registration handle.</param>
        /// <param name="unregisterAgent">Unregistration delegate.</param>
        /// <param name="env">Environment reference.</param>
        /// <param name="precipitationLayer">Precipitation layer</param>
        /// <param name="vegetationLayerDgvm"></param>
        /// <param name="id">Globally unique agent ID.</param>
        /// <param name="lat">Latitude value.</param>
        /// <param name="lon">Longitude value.</param>
        /// <param name="heightInM">Tree height in meter</param>
        /// <param name="ageInDays">Tree age in days</param>
        /// <param name="stemDiameterInM">Tree stem diameter in m</param>
        /// <param name="sex">Sex of tree</param>
        [PublishForMappingInMars]
        public MarulaTree
        (
            IKNPMarulaLayer marulaLayer,
            RegisterAgent registerAgent,
            UnregisterAgent unregisterAgent,
            GeoGridEnvironment<GeoAgent<MarulaTree>> env,
            KNPGISRasterPrecipitationLayer.KNPGISRasterPrecipitationLayer precipitationLayer,
            IKNPGISRasterVegetationLayer vegetationLayerDgvm,
            Guid id,
            double lat,
            double lon,
            double heightInM,
            int ageInDays,
            double stemDiameterInM,
            int sex)
            : base(marulaLayer,
                registerAgent,
                unregisterAgent,
                env,
                new GeoCoordinate(lat, lon),
                id.ToByteArray(),
                TickTreeEveryNthTick)
        {
            _lock = new object();
            HeightInM = heightInM;
            StemDiameterInM = stemDiameterInM;
            TreeSex = (Sex) sex;
            _marulaLayer = marulaLayer;

            _precipitationLayer = precipitationLayer;
            _vegetationLayerDgvm = vegetationLayerDgvm;

            // age is provided in years, not days, but variable mapping is fixed now. So correct error
            ageInDays = ageInDays * 365;
            _dateOfGermination = _marulaLayer.GetCurrentSimulationTime().Subtract(TimeSpan.FromDays(ageInDays));
            TreeAgeType = CalculateAgeType();

            // Get maximum height.
            // TODO ersetzen durch daten aus generierten marulas
            MaxHeightInM = _random.Next(10, 18);
            MaxHeightInM += _random.NextDouble();

            var maxYearVariation = _random.Next(-20, 20);
            MaxTreeAgeInYears = 250 + maxYearVariation;

            // calculate initial biomass
            CalculateBiomass();
        }


        #region IMarulaTree Members

        public IGeoCoordinate GetPosition()
        {
            return new GeoCoordinate(Latitude, Longitude);
        }


        public double EatMarulaSeedling(double biomassAmountToTake)
        {
            if (TreeAgeType != AgeType.Seedling || IsAlive == false)
            {
                return 0;
            }

            // needs locking
            lock (_lock)
            {
                KillTree();
                var retVal = TotalBiomass;
                TotalBiomass = 0;
//                _vegetationLayerDgvm.TryToReduceCellRating(new GeoCoordinate(Latitude, Longitude), 
//                    biomassAmountToTake );
                return retVal;
            }
        }

        public int EatFruit(int numberOfFruits, double biomassAmountToTake)
        {
            lock (_lock)
            {
                if (numberOfFruits <= EatableFruits)
                {
                    EatableFruits -= numberOfFruits;
//                    _vegetationLayerDgvm.TryToReduceCellRating(new GeoCoordinate(Latitude,Longitude),
//                        biomassAmountToTake);
                    return numberOfFruits;
                }

                var eaten = EatableFruits;
                EatableFruits = 0;
                return eaten;
            }
        }

        //TODO discuss with Ulfia, find appropriate biomassAmountToTake
        public double EatLeaves(double biomassAmountToTake)
        {
            if (LeafBiomass > 0)
            {
//                var biomassTaken = _vegetationLayerDgvm.TryToReduceCellRating(new GeoCoordinate(Latitude, Longitude),
//                    biomassAmountToTake);
//                LeafBiomass -= biomassTaken;
//                if (LeafBiomass < 0)
//                    LeafBiomass = 0;
//                
//                SetDamage(_random.NextDouble() + 1);
//                return biomassTaken;

                //WIEDER LOESCHEN
                return 5;
                //WIEDER LOESCHEN
            }

            return 0;
        }


        // TODO check correctness
        // Baum stirbt in jedem Fall
        public double PushTree(double force)
        {
            //only if less then 50 percent fitness pushing over causes 25 percent less fitness
            if (HealthPoints < 50)
            {
                SetDamage(25);
            }

            if (IsAlive == false)
            {
                return 0;
            }

            lock (_lock)
            {
                KillTree();
                // trees continue living after being pushed over
                var retVal = LeafBiomass;
                LeafBiomass = 0;
                return retVal;
            }
        }

        #endregion

        internal void SetDamage(double damage)
        {
            HealthPoints -= damage;
            if (HealthPoints < HealthClassWithLowerBoundaries[HealthClass.ONLY_REGENERATION])
            {
                KillTree();
            }

            CalculateHealthClass();
        }

        internal void CalculateHealthClass()
        {
            if (HealthPoints > HealthClassWithLowerBoundaries[HealthClass.FINE])
            {
                Health = HealthClass.FINE;
            }
            else if (HealthPoints > HealthClassWithLowerBoundaries[HealthClass.ONLY_GROWTH_AND_REG])
            {
                Health = HealthClass.ONLY_GROWTH_AND_REG;
            }
            else if (HealthPoints > HealthClassWithLowerBoundaries[HealthClass.ONLY_REGENERATION])
            {
                Health = HealthClass.ONLY_REGENERATION;
            }
        }

        /// <summary>
        ///     The reaction logic of the Marula tree.
        /// </summary>
        /// <returns>The action to execute in this tick.</returns>
        protected override IInteraction Reason()
        {
            TreeAgeType = CalculateAgeType();

            // TODO Was war die urspruengliche Idee hierfuer?
            var soilWater = 20D;

            return new MarulaGrowth(this, _marulaLayer, soilWater, GetPosition());
        }

        public int GetTreeAgeInYears()
        {
            var currentSimulationTime = _marulaLayer.GetCurrentSimulationTime();
            var ageInYears = currentSimulationTime.Year - _dateOfGermination.Year;
            // TODO what does this do?
            if (currentSimulationTime < _dateOfGermination.AddYears(ageInYears))
            {
                ageInYears--;
            }

            return ageInYears;
        }

        #region Transformation methods

        /// <summary>
        ///     Output the agent properties.
        /// </summary>
        /// <returns>Formatted string.</returns>
        public override string ToString()
        {
            return String.Format
            ("[MarulaTree] ID: {0}, HP: {1,3}, Height: {2,6:0.000}m " +
             "(max: {3,6:0.000}m), Diameter: {4,6:0.000}m, Fruits: {5,2}",
                ID,
                (int) HealthPoints,
                HeightInM,
                MaxHeightInM,
                StemDiameterInM,
                (int) EatableFruits);
        }


        public MarulaDto ToDto()
        {
            return new MarulaDto(ID, EatableFruits, TreeAgeType, Latitude, Longitude);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Calculates the agetype of a tree
        /// </summary>
        /// <returns>Enum indicating the age type of current tree</returns>
        private AgeType CalculateAgeType()
        {
            var treeAgeInYears = GetTreeAgeInYears();

            if (treeAgeInYears < 1)
            {
                return AgeType.Seedling;
            }

            if (treeAgeInYears < 18)
            {
                return AgeType.Juvenile;
            }

            return AgeType.Adult;
        }

        public double getLeafBiomass()
        {
            return LeafBiomass;
        }

        /// <summary>
        ///     The agent has died!
        /// </summary>
        internal void KillTree()
        {
            IsAlive = false;
            //_marulaLayer.RemoveTree(ID);
        }

        internal bool GetIsAlive()
        {
            return IsAlive;
        }

        internal double GetHeight()
        {
            return HeightInM;
        }

        public HealthClass GetHealthClass()
        {
            return this.Health;
        }

        internal void CalculateBiomass()
        {
            StemBiomass = 0.9396 * (Math.Pow(StemDiameterInM, 2) * HeightInM) * Math.Pow(Math.E, 0.9326);
            BranchBiomass = 0.003487 * (Math.Pow(StemDiameterInM, 2) * HeightInM) * Math.Pow(Math.E, 1.027);
            var month = _marulaLayer.GetCurrentSimulationTime().Month;
            if (month < 11 && month > 3)
            {
                LeafBiomass = 1 / ((28 / (StemBiomass + BranchBiomass)) + 0.025);
            }
            else
            {
                LeafBiomass = 0;
            }
        }

        #endregion

        #region PropertyChanged Handling

        // TODO Wofuer genau brauchen wir das?
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        ///   Return the concrete agent reference. This is required by the environment
        ///   to get back from the 'GeoAgent' wrapper to 'MarulaTree' itself.
        /// </summary>
        public override MarulaTree AgentReference => this;
    }
}