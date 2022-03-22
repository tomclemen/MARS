using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Bushbuckridge.Agents.Collector;
using KNPElephantLayer.Agents;
using KNPElephantLayer.Layer;
using KNPGISRasterFenceLayer;
using KNPGISRasterShadeLayer;
using KNPGISVectorWaterLayer;
using KNPTreeQuickFindLayer;
using Mars.Common.Collections;
using Mars.Common.Logging;
using Mars.Common.Logging.Enums;
using Mars.Core.ModelContainer.Entities;
using Mars.Core.SimulationStarter;
using SavannaTrees;

namespace Starter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var watch = new Stopwatch();
            watch.Start();
            
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var description = new ModelDescription();
            
            LoggerFactory.SetLogLevel(LogLevel.Off);

            description.AddLayer<Precipitation>();
            description.AddLayer<Temperature>();
            description.AddLayer<SavannaLayer>();
            description.AddLayer<DroughtLayer>();
            description.AddLayer<TimeKeepingLayer.KNPTimeKeeperLayer>();
            description.AddLayer<HerbivorePressureLayer>();
            description.AddLayer<TreeRaster>();
            description.AddLayer<TreeQuickFindLayer>();
            description.AddLayer<GISRasterFenceLayer>();

            description.AddLayer<KNPGISRasterShadeLayer.KNPGISRasterShadeLayer>(new List<Type>()
            {
                typeof(IKNPGISRasterShadeLayer)
            });

            description.AddLayer<KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer>();
        
            description.AddLayer<KNPGISVectorWaterLayer.KNPGISVectorWaterLayer>(new List<Type>()
            {
                typeof(IKNPGISVectorWaterLayer)
            });
        
            description.AddLayer<ElephantLayer>();

            description.AddAgent<Tree, SavannaLayer>();
            description.AddAgent<Rafiki, SavannaLayer>();
            description.AddAgent<Elephant, ElephantLayer>();

            if (args != null)
            {
                if (args.Any(s => s.Equals("-l")))
                {
                    LoggerFactory.SetLogLevel(LogLevel.Info);
                    LoggerFactory.ActivateConsoleLogging();
                }

                if (args.Any(s => s.Equals("-sm")))
                {
                    var index = args.IndexOf(s => s == "-sm");
                    var file = File.ReadAllText(args[index + 1]);
                    var simConfig = SimulationConfig.Deserialize(file);

                    var starter = SimulationStarter.Start(description, simConfig);
                    starter.Run();
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            
            watch.Stop();
            Console.WriteLine("Simulation took " + watch.Elapsed);
        }
    }
}