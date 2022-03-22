using System.Collections.Generic;
using KNPElephantLayer.Agents;

namespace KNPElephantLayer.Layer
{
    /// <summary>
    ///     Represents an elephant herd
    ///     Is not an agent, but just an object to
    ///     store information about elephants in a herd
    /// </summary>
    public class ElephantHerd
    {
        private int Id { get; set; }
        public Elephant LeadingElephant { get; }

        public readonly List<Elephant> OtherElephants;

        public ElephantHerd(int herdId, Elephant leader, List<Elephant> other)
        {
            OtherElephants = other;
            Id = herdId;
            LeadingElephant = leader;
        }
    }
}