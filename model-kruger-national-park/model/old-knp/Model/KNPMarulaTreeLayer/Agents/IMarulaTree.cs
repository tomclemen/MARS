using Mars.Interfaces.Agent;
using Mars.Interfaces.Environment.GeoCommon;

namespace KNPMarulaTreeLayer.Agents
{
    /// <summary>
    ///     Interface for Marula tree functionality.
    /// </summary>
    public interface IMarulaTree : IAgent
    {
        /// <summary>
        /// A marula tree can be eaten by an animal only if the tree is a seedling
        /// </summary>
        /// <returns>the biomassAmountToTake of biomass aquired</returns>
        double EatMarulaSeedling(double biomassAmountToTake);

        /// <summary>
        /// Returns the number of eatable/available fruits
        /// </summary>
        /// <returns>the number of eatable/available fruits</returns>
        int EatableFruits { get; }

        AgeType TreeAgeType { get; }

        /// <summary>
        /// Eats a number of available fruits and returns the number of fruits that were really eaten.
        /// </summary>
        /// <param name="numberOfFruits"></param>
        /// <returns></returns>
        int EatFruit(int numberOfFruits, double biomassAmountToTake);

        /// <summary>
        /// Eats the leaves of a tree
        /// </summary>
        /// <returns>The biomassAmountToTake of biomass aquired</returns>
        double EatLeaves(double biomassAmountToTake);

        /// <summary>
        /// Pushes the tree into oblivion. Assumes the elphant consumes all 
        /// leaves afterwards.
        /// </summary>
        /// <param name="force">The force to push the tree with.</param>
        /// <returns>The biomass aquired from eating leaves.</returns>
        double PushTree(double force);

        /// <summary>
        /// Returns lat/lon position of tree
        /// </summary>
        /// <returns>Returns lat/lon position of tree</returns>
        IGeoCoordinate GetPosition();
    }
}