using System;
using System.Collections.Generic;
using MarulaLayer.Dto;
using KNPMarulaTreeLayer.Agents;
using KNPMarulaTreeLayer.Interaction;
using Mars.Interfaces.Environment.GeoCommon;
using Mars.Interfaces.Layer;

namespace MarulaLayer.Layers
{
    public interface IKNPMarulaLayer : ISteppedActiveLayer
    {
        DateTime GetCurrentSimulationTime();
        TimeSpan GetTickTimeSpan();
        FruitSeasonManager GetFruitSeasonManager(IGeoCoordinate pos);
        List<MarulaTree> Explore(double lat, double lon, double radius, int maxResults = -1);
        MarulaDto PlantTree(double lat, double lon);
        double? GetPrecipitation(IGeoCoordinate pos);

        /// <summary>
        /// Removes a tree from the layers' list of trees.
        /// </summary>
        /// <param name="treeId">The ID of the tree to remove.</param>
        void RemoveTree(Guid treeId);
    }
}