using System;
using KNPMarulaTreeLayer.Agents;

namespace MarulaLayer.Dto
{
    [Serializable]
    public class MarulaDto {
        public Guid Id { get; private set; }
        public int CurrentlyEatableFruits { get; private set; }
        public AgeType AgeType { get; private set; }
        public double Latidude { get; private set; }
        public double Longitude { get; private set; }


        public MarulaDto
            (Guid id, int currentlyEatableFruits, AgeType ageType, double latidude, double longitude) {
            Id = id;
            CurrentlyEatableFruits = currentlyEatableFruits;
            AgeType = ageType;
            Latidude = latidude;
            Longitude = longitude;
        }
        /*
        public MarulaDto(Guid id) { 
            var ass = new AgentShadowingServiceComponent<IMarulaTree, MarulaTree>();
            var _marula = ass.ResolveAgent(id);
            Id = _marula.ID;
            CurrentlyEatableFruits = _marula.GetEatableFruitCount();
            AgeType = _marula.;
            HeightInCm = heightInCm;
            DiameterInCm = diameterInCm;
        }
        */

    }

}