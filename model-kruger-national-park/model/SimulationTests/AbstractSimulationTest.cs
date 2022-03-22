using System;
using Mars.Core.SimulationManager.Entities;

namespace SimulationTests
{
    public class AbstractSimulationTest : IDisposable
    {
        public void Dispose()
        {
            SimulationClock.Dispose();
        }
    }
}