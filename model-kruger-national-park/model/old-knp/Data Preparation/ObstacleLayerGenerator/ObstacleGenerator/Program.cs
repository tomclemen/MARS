using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using LIFE.API.Environment.GeoCommon;

namespace ObstacleGenerator
{
    class Program
    {
        public static string Filename = "veg_knp.csv";
        public static string InputFileFolder = "./input/";
        public static string OutputFileFolder = "./output/";
        public static int ColumnCount = 11;

        public static string FileHeader = "long;lat;year;c4_biomass;c3_biomass;dead_grass_biomass;savanna_tree_cover;" +
                                          "forest_tree_cover;tree_leaf_biomass;tree_stem_biomass;trees_per_ha";

        public static int StartYear = 1979;
        public static int EndYear = 2099;

        public static double LongitudeLeft = 30.751;
        public static double LongitudeRight = 32.251;
        public static double LatitudeTop = -21.74;
        public static double LatitudeBottom = -25.251;
        public static int CellSizeInMeter = 50000;

        public static int ConversionFactorHaTo50Km2 = 250000;

        static void Main(string[] args)
        {
            Console.WriteLine("[ObstacleGenerator] starting up");

            if (!Directory.Exists(InputFileFolder))
            {
                Console.WriteLine("Input folder created, put input files there");
                Directory.CreateDirectory(InputFileFolder);
                return;
            }

            if (!Directory.Exists(OutputFileFolder))
                Directory.CreateDirectory(OutputFileFolder);
            else
            {
                Directory.Delete(OutputFileFolder,true);
                Directory.CreateDirectory(OutputFileFolder);
            }

            foreach (var file in Directory.EnumerateFiles(InputFileFolder, "*.csv"))
            {
                //open input file
                using (var fileReader = new StreamReader(File.OpenRead(file)))
                {
                    //check if header is in right format
                    if (fileReader.ReadLine() != FileHeader)
                        throw new Exception("Header not in right format");

                    var currentYear = StartYear;
                    var obstacleMap = new ObstacleMapCopy(
                        topLat: LatitudeTop,
                        bottomLat: LatitudeBottom,
                        leftLong: LongitudeLeft,
                        rightLong: LongitudeRight,
                        cellSizeInM: CellSizeInMeter);

                    while (!fileReader.EndOfStream)
                    {
                        var line = fileReader.ReadLine().Split(';');
                        if (line.Length != ColumnCount)
                            throw new Exception("Not in enough columns in row");

                        //add values to current year list
                        if (Convert.ToInt32(line[2]) == currentYear)
                        {
                            var lon = double.Parse(line[0]);
                            var lat = double.Parse(line[1]);
                            
                            var c4Biomass = double.Parse(line[4]);
                            var c3Biomass = double.Parse(line[5]);
                            var deadGrassBiomass = double.Parse(line[6]);
                            var treeLeafBiomass = double.Parse(line[9]);
                            var treeStemBiomass = double.Parse(line[10]);

                            var biomassSum =  ConversionFactorHaTo50Km2 * (c4Biomass + c3Biomass + deadGrassBiomass +
                                                                          treeLeafBiomass + treeStemBiomass);

                            //Mitja's import can't handle doubles as input
                            var biomassLong = Convert.ToInt64(biomassSum);
                            
                            obstacleMap.AddCellRating(new GeoCoordinate(lat, lon), biomassLong);
                        }
                        //year is
                        else if (Convert.ToInt32(line[2]) == currentYear + 1)
                        {
                            WriteObstcleMapToFile(OutputFileFolder, Path.GetFileNameWithoutExtension(file), currentYear.ToString(), obstacleMap);
                            obstacleMap = new ObstacleMapCopy(
                                topLat: LatitudeTop,
                                bottomLat: LatitudeBottom,
                                leftLong: LongitudeLeft,
                                rightLong: LongitudeRight,
                                cellSizeInM: CellSizeInMeter);
                            currentYear++;
                        }
                    }
                    //todo last iteration once the input file has no lines anymore
                    WriteObstcleMapToFile(OutputFileFolder, Path.GetFileNameWithoutExtension(file), currentYear.ToString(), obstacleMap);
                }
            }

            Console.WriteLine("[ObstacleGenerator] shutting down");
        }

        public static void WriteObstcleMapToFile(string folder, string subFolder, string fileName, ObstacleMapCopy obstacleMap)
        {
            if (!Directory.Exists(Path.Combine(folder, subFolder)))
                Directory.CreateDirectory(Path.Combine(folder + subFolder));
            
            var outputFilePath = Path.Combine(folder, subFolder, fileName + ".csv");
            
            using (var file = new StreamWriter(new FileStream(outputFilePath, FileMode.OpenOrCreate)))
            {
                file.WriteLine("LatitudeTop=" + LatitudeTop);
                file.WriteLine("LongitudeLeft=" + LongitudeLeft);
                file.WriteLine("LatitudeBottom=" + LatitudeBottom);
                file.WriteLine("LongitudeRight=" + LongitudeRight);
                file.WriteLine("CellSizeInM=" + CellSizeInMeter);

                obstacleMap.WriteObstacleMapToFile(file);
                
                Console.WriteLine("Created obstacle file \"" + subFolder + fileName + ".csv\"");
            }
        }
    }
}