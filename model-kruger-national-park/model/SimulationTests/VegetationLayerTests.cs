using System;
using System.Linq;
using Mars.Interfaces.Environment;
using Mars.Interfaces.Layer.Initialization;
using Xunit;

namespace SimulationTests
{
    public class VegetationLayerTests : AbstractSimulationTest
    {
//        [Fact]
//        public void CoordinateTest()
//        {
//            var layer = new KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer();
//            var context = new SimulationContext(TimeSpan.FromMinutes(1), DateTime.Now);
//            var initData = new TInitData(context);
//            initData.LayerInitConfig.File = "../../../../../model_input/gis_raster_biomass_ts.zip";
//
//            layer.InitLayer(initData, null, null);
//
//
//            var gridCoordinate = layer.ConverToGridCoordinate(Position.CreateGeoPosition(31.169, -22.757));
//
//            Assert.NotNull(gridCoordinate);
//            Assert.Equal(1, gridCoordinate.X);
//            Assert.Equal(5, gridCoordinate.Y);
//
//
//            gridCoordinate = layer.ConverToGridCoordinate(Position.CreateGeoPosition(32.188, -22.250));
//
//            Assert.NotNull(gridCoordinate);
//            Assert.Equal(3, gridCoordinate.X);
//            Assert.Equal(6, gridCoordinate.Y);
//
//            gridCoordinate = layer.ConverToGridCoordinate(Position.CreateGeoPosition(32.49999948, -25.49999950));
//
//            Assert.NotNull(gridCoordinate);
//            Assert.Equal(3, gridCoordinate.X);
//            Assert.Equal(0, gridCoordinate.Y);
//
//            gridCoordinate = layer.ConverToGridCoordinate(Position.CreateGeoPosition(30.499952, -21.499952));
//
//            Assert.Null(gridCoordinate);
//        }

        [Fact]
        public void FindBiomassTest()
        {
            var layer = new KNPGISRasterVegetationLayer.KNPGISRasterVegetationLayer();
            var context = new SimulationContext(TimeSpan.FromMinutes(1), new DateTime(2019,12,12));
            var initData = new TInitData(context);
            initData.LayerInitConfig.File = "../../../../../model_input/gis_raster_biomass_ts.zip";

            layer.InitLayer(initData, null, null);

            //30.754,-21.799 -> upper left
            //32.268,-21.829 -> upper right
            //30.751,-25.225 -> lower left
            //32.223,-25.238 -> lower right

            var upperLeft = layer.Explore(Position.CreateGeoPosition(30.754, -21.799), double.MaxValue, 1);
            Assert.Equal(0, upperLeft.First().Node.NodePosition.Longitude);
            Assert.Equal(7, upperLeft.First().Node.NodePosition.Latitude);
            
            var lowerRight = layer.Explore(Position.CreateGeoPosition(32.223,-25.238), double.MaxValue, 1);
            Assert.Equal(3, lowerRight.First().Node.NodePosition.Longitude);
            Assert.Equal(0, lowerRight.First().Node.NodePosition.Latitude);

            //find maxima
            var all = layer.Explore(Position.CreateGeoPosition(30.754, -21.799), double.MaxValue, 33);
            
            var lowest = all.OrderBy(a => a.Node.Value).First();
            Assert.Equal(1, lowest.Node.NodePosition.Longitude);
            Assert.Equal(2, lowest.Node.NodePosition.Latitude);
            
            var highest = all.OrderBy(a => a.Node.Value).Last();
            Assert.Equal(3, highest.Node.NodePosition.Longitude);
            Assert.Equal(7, highest.Node.NodePosition.Latitude);
        }
    }
}