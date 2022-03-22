using System;
using System.Collections.Generic;
using System.IO;
using Accord.Statistics.Kernels;
using KNPElephantLayer;
using KNPElephantLayer.Agents;
using KNPElephantLayer.Layer;
using KNPGISRasterFenceLayer;
using KNPGISRasterShadeLayer;
using KNPGISRasterTempLayer;
using KNPGISRasterVegetationLayer;
using KNPGISVectorPrecipitationLayer;
using KNPGISVectorWaterLayer;
using KNPMarulaTreeLayer.Agents;
using Mars.Common.Logging;
using Mars.Common.Logging.Enums;
using Mars.Core.ModelContainer.Entities;
using Mars.Core.SimulationStarter;
using MarulaLayer.Layers;

namespace Starter
{
    class Program
    {
        static void Main(string[] args)
        {
//            if (!File.Exists(Path.Combine("bin", "Debug", "netcoreapp2.0", "input_data",
//                "waterpoints_working_v3.zip")))
//                throw new ArgumentOutOfRangeException();

            var modelDescription = new ModelDescription();

            //raster layers
            modelDescription.AddLayer<KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer>(new List<Type>(){typeof(IKNPGISRasterVegetationLayer)});
            modelDescription.AddLayer<KNPGISRasterTempLayer.KNPGISRasterTempLayer>(new List<Type>(){typeof(IKNPGISRasterTempLayer)});
            modelDescription.AddLayer<KNPGISRasterFenceLayer.KNPGISRasterFenceLayer>(new List<Type>(){typeof(IKNPGISRasterFenceLayer)});
            modelDescription.AddLayer<KNPGISRasterShadeLayer.KNPGISRasterShadeLayer>(new List<Type>(){typeof(IKNPGISRasterShadeLayer)});
            modelDescription.AddLayer<KNPGISRasterPrecipitationLayer.KNPGISRasterPrecipitationLayer>(new List<Type>());
            
            //agent layers
            modelDescription.AddLayer<ElephantLayer>(new List<Type>(){typeof(IKnpElephantLayer)});
            modelDescription.AddLayer<KNPMarulaTreeLayer.Layers.MarulaLayer>(new List<Type>(){typeof(IKNPMarulaLayer)});
            
            //vector layers
//            modelDescription.AddLayer<KNPGISVectorPrecipitationLayer.KNPGISVectorPrecipitationLayer>();
//            modelDescription.AddLayer<KNPGISVectorTempLayer.KNPGISVectorTempLayer>();
            modelDescription.AddLayer<KNPGISVectorWaterLayer.KNPGISVectorWaterLayer>(new List<Type>(){typeof(IKNPGISVectorWaterLayer)});

            //agents
            modelDescription.AddAgent<Elephant,ElephantLayer>();
            modelDescription.AddAgent<MarulaTree, KNPMarulaTreeLayer.Layers.MarulaLayer>();

            var file = File.ReadAllText(Path.Combine("sim_configs", "5kEl_5kTrees.json"));
            var simConfig = SimulationConfig.Deserialize(file);

            var starter = SimulationStarter.Start(modelDescription, simConfig);
            starter.Run();
        }
    }
}