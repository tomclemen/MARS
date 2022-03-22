#!/bin/sh

INPUT_FILE_DIRECTORY="../csharp/WaterPotentialFieldLayerGenerator/InputFiles"
AREA_SETTINGS="--latitude-top 3.012 --longitude-left -77.003 --latitude-bottom 2.041 --longitude-right -76.038"

mkdir -p output
python cli.py $AREA_SETTINGS -o output/waterways.csv $INPUT_FILE_DIRECTORY/Waterways.csv
