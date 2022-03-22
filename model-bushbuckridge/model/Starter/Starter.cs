using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Bushbuckridge.Agents.Collector;
using Mars.Common.Logging;
using Mars.Common.Logging.Enums;
using Mars.Core.ModelContainer.Entities;
using Mars.Core.SimulationStarter;
using SavannaTrees;
using TimeKeepingLayer;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args != null && Enumerable.Any(args, s => s.Equals("-l")))
        {
            LoggerFactory.SetLogLevel(LogLevel.Error);
            LoggerFactory.ActivateConsoleLogging();
        }

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var description = new ModelDescription();
        description.AddLayer<Precipitation>();
        description.AddLayer<Temperature>();
        description.AddLayer<TreeRaster>();

        description.AddLayer<SavannaLayer>();
        description.AddLayer<TimeKeeperLayer>();

        description.AddLayer<DroughtLayer>();
        description.AddLayer<HerbivorePressureLayer>();
        description.AddLayer<FirewoodCollectorLayer>();

        description.AddAgent<Rafiki, SavannaLayer>();
        description.AddAgent<Tree, SavannaLayer>();
        description.AddAgent<FirewoodCollector, FirewoodCollectorLayer>();
        var stopwatch = Stopwatch.StartNew();

        var task = SimulationStarter.Start(description, args);
        var loopResults = task.Run();
        Console.WriteLine($"Simulation execution finished after {loopResults.Iterations} steps in " +
                          stopwatch.Elapsed.Days + "d " + stopwatch.Elapsed.Hours + "h " + stopwatch.Elapsed.Minutes +
                          "m " + stopwatch.Elapsed.Seconds + "s");
    }
}