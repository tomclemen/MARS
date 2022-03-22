namespace AgentCsvGenerator.Config
{
    public class AreaDefinition
    {
        public const int RasterLengthInMeter = 100; //raster in 1 ha = 100m x 100m
        
        public readonly double West;
        public readonly double East;
        public readonly double North;
        public readonly double South;

        public readonly int WidthInMeter;
        public readonly int LengthInMeter;


        public double OneMeterLat => (South - North) / LengthInMeter;

        public double OneMeterLon => (East - West) / WidthInMeter;

        public AreaDefinition(double west, double east, double north, double south, int widthInMeter,
            int lengthInMeter)
        {
            West = west;
            East = east;
            North = north;
            South = south;
            WidthInMeter = widthInMeter;
            LengthInMeter = lengthInMeter;
        }
    }
}