using System;

namespace WaterPotentialFieldLayerGenerator
{
    internal class MainClass
    {
        // TODO: detect boundings
//        private const double LatitudeTop = -22.35937;
//        private const double LongitudeLeft = 30.9237;
//        private const double LatitudeBottom = -25.5295;
//        private const double LongitudeRight = 32.032578;

        private const double LatitudeTop = 3.012;
        private const double LongitudeLeft = -77.003;
        private const double LatitudeBottom = 2.041;
        private const double LongitudeRight = -76.038;

        private const int GridLengthInMeter = 1000;
        private const int PotentialDepthInCells = 7;

        private const string ShadePoints = @"../../InputFiles/shadePoints.csv";
        private const string OutputFile = @"./potential field shade.csv";
        private const int ShadeLonIndex = 0;
        private const int ShadeLatIndex = 1;

        // Insert your input and output file as well as the position of LON and LAT
        private static readonly object[][] PotentialFieldInputData =
        {
//            new object[]
//            {
//                @"../../InputFiles/waterpoints future open.csv", @"./potential field water future.csv", 6, 7
//            },
//            new object[]
//            {
//                @"../../InputFiles/waterpoints current open.csv", @"./potential field water current.csv", 6, 7
//            },
//            new object[]
//            {
//                null, @"./potential field water no WP.csv", 6, 7
//            },
            new object[]
            {
                @"../../InputFiles/Waterways.csv", @"./waterways.csv", 0, 1
            }
        };


        public static void Main(string[] args)
        {
            // the shadePoints.csv is large (56MB), therefore its not in git, but in the owncloud
            // ownCloud/MARS/Models & Data/MARS KNP/Model Comparison/Input Data/Bucini Vegetation/Conversion steps/step 4 - extract points
            GenerateShadePotentialField();
            GenerateWaterPotentialFields();
        }

        public static void GenerateShadePotentialField()
        {
            var futurePotentialFieldGenerator = new WaterPotentialFieldGenerator(LatitudeTop, LongitudeLeft,
                LatitudeBottom, LongitudeRight, GridLengthInMeter, PotentialDepthInCells);

            futurePotentialFieldGenerator.InitLayerFromFile(ShadePoints, ShadeLonIndex, ShadeLatIndex, ',');
            futurePotentialFieldGenerator.PrintPotentialField();

            futurePotentialFieldGenerator.WriteLayerToFile(OutputFile);
        }

        private static void GenerateWaterPotentialFields()
        {
            foreach (var input in PotentialFieldInputData)
            {
                GeneratePotentialField(input[0].ToString(), input[1].ToString(), (int) input[2], (int) input[3]);
            }
        }

        private static void GeneratePotentialField(string waterpointFile, string outputFile, int lonIndex, int latIndex)
        {
            var futurePotentialFieldGenerator = new WaterPotentialFieldGenerator(LatitudeTop, LongitudeLeft,
                LatitudeBottom, LongitudeRight, GridLengthInMeter, PotentialDepthInCells);

            Console.WriteLine("Using " + waterpointFile + " as input");

            futurePotentialFieldGenerator.InitLayerFromFile(waterpointFile, lonIndex, latIndex, ';');

            futurePotentialFieldGenerator.PrintPotentialField();
            futurePotentialFieldGenerator.WriteLayerToFile(outputFile);
        }
    }
}