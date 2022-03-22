using System;
using System.Diagnostics;
using Mars.Core.SimulationManager.Entities;
using Mars.Interfaces.Layer;
using Mars.Interfaces.Layer.Initialization;

namespace TimeKeepingLayer
{
    public class KNPTimeKeeperLayer : ISteppedActiveLayer
    {
        private long _currentTick;
        private readonly Stopwatch _stopWatch;

        public KNPTimeKeeperLayer()
        {
            _stopWatch = new Stopwatch();
        }

        public bool InitLayer(TInitData layerInitData, RegisterAgent registerAgentHandle, UnregisterAgent unregisterAgentHandle)
        {
            
            _stopWatch.Restart();
            
            return true;
        }

        public long GetCurrentTick()
        {
            return _currentTick;
        }

        public void SetCurrentTick(long currentStep)
        {
            _currentTick = currentStep;
        }

        public void Tick()
        {
            //Console.WriteLine("[TimeKeeperLayer] Tick " + _currentTick + " finished");
            
            if (SimulationClock.CurrentTimePoint.Value.DayOfWeek == DayOfWeek.Monday && SimulationClock.CurrentTimePoint.Value.Hour == 1)
            {
                Console.Write(".");
            }

            if (SimulationClock.CurrentTimePoint.Value.Day == 1 && SimulationClock.CurrentTimePoint.Value.Hour == 1)
            {
                Console.Write(" ");
            }

            if (!IsNextYearTick()) return;
            Console.Write(SimulationClock.CurrentTimePoint.Value.Year + " in " + _stopWatch.Elapsed.Days + "d " +
                          _stopWatch.Elapsed.Hours + "h " + _stopWatch.Elapsed.Minutes + "m " +
                          _stopWatch.Elapsed.Seconds + "s ");
            Console.WriteLine();
            _stopWatch.Restart();
        }

        public void PreTick()
        {
        }

        public void PostTick()
        {
        }
        
        private static bool IsNextYearTick()
        {
            return SimulationClock.CurrentTimePoint.Value.Day == SimulationClock.StartTimePoint.Value.Day &&
                   SimulationClock.CurrentTimePoint.Value.Hour == 1 &&
                   SimulationClock.CurrentTimePoint.Value.Month == SimulationClock.StartTimePoint.Value.Month &&
                   SimulationClock.CurrentTimePoint.Value.Year > SimulationClock.StartTimePoint.Value.Year;
        }
    }
}