import os

from joblib import Parallel, delayed
import multiprocessing

from shapely import wkb, geometry, speedups
import random
import sys
import csv

try:
    from osgeo import ogr, osr, gdal
except:
    sys.exit('ERROR: cannot find GDAL/OGR modules')


def calculateHeightInMForYears(years):
    currentHeightInM = 0.0
    currentAgeType = 0
    maxHeightInM = random.uniform(10, 18)
    maxHeightInM += random.random()
    heightGrowthFactors = [0.1, 0.05, 0.01]
    ageRanges = [1, 17, 382]
    for i in range(0, years):
        if i == 1:
            currentAgeType = 1
        else:
            if i > 18:
                currentAgeType = 2

        if currentHeightInM < maxHeightInM:
            currentHeightInM += heightGrowthFactors[currentAgeType] * maxHeightInM

    return currentHeightInM


def calculateDiameterInMForYears(years):
    currentDiameterInCm = 0.0
    for i in range(0, years):
        currentDiameterInCm = currentDiameterInCm + (-1 * 0.068 * currentDiameterInCm) + 4.54
    return currentDiameterInCm / 100


def createCsvRowFromPoint(point):
    age = random.randint(10, 150)
    sexRand = random.uniform(0, 100)
    sex = 0 if sexRand < 55 else 1

    #horizontal extension is described by longitude, therefore x is lon and y is lat
    return {'Lat': point.y,
            'Lon': point.x,
            'Height': calculateHeightInMForYears(age),
            'Diameter': calculateDiameterInMForYears(age),
            'Age': age,
            'Sex': sex}

# output csv?
output_csv = True
# output shp?
output_shp = False
# delete files
try:
    if output_shp:
        os.remove('random_points.dbf')
        os.remove('random_points.shp')
        os.remove('random_points.shx')
    if output_csv:
        os.remove('marula_trees_knp_generated.csv')
except:
    print("No files to remove, continuing")

print("Starting")

# get number of cores available on this machine
num_cores = multiprocessing.cpu_count()

# setup shapefile driver
driver = ogr.GetDriverByName('ESRI Shapefile')

# input SpatialReference
inSpatialRef = osr.SpatialReference()
inSpatialRef.ImportFromEPSG(32736)  # UTM 36S

# output SpatialReference
outSpatialRef = osr.SpatialReference()
outSpatialRef.ImportFromEPSG(4326)  # WGS 84

# create the CoordinateTransformation
coordTrans = osr.CoordinateTransformation(inSpatialRef, outSpatialRef)

# import shapefiles
knp_landtype_shapefile = driver.Open("landscapes_gertenbach1983.shp")

# get layers
knp_landtypes = knp_landtype_shapefile.GetLayer()

# sample_farmplots = sample_farmplots_shapefile.GetLayer()
number_of_landtypes = knp_landtypes.GetFeatureCount()

# Dictionary of ok area IDs
valid_areas = [1, 2, 3, 4, 5, 13, 14, 17, 18, 19, 21, 29, 31]

# set default tree count value
target_tree_count = 1100
# create random points for each province, points have to be inside farm plots
# array to hold all result rows for csv output
csvRows = []
# array to hold found
found_points = []
print("\n--> creating new tree positions")
for i in xrange(number_of_landtypes):

    # convert to shapely
    landtype_feature = knp_landtypes.GetFeature(i)
    area_id = int(landtype_feature.GetField("LSCAP_ID"))
    if not area_id in valid_areas:
        continue

    landtype_geom = landtype_feature.GetGeometryRef()

    # check whether area can be calculated
    if landtype_geom.GetGeometryName() == "POLYGON":
        # get area in square kilometres. Unit is m since we use UTM here
        areaInSquareKM = landtype_geom.GetArea() / 1000000
        areaInHectare = areaInSquareKM * 100
        # only take mature marulas into account
        target_tree_count = int(areaInHectare * 3)

    # transform reference to WGS 84 / GPS coordinates
    landtype_geom.Transform(coordTrans)

    # load feature as WKB
    landtype = wkb.loads(landtype_geom.ExportToWkb())

    # reset random point count
    random_point_count = 0


    def createPoint(counter):
        done = False
        while not done:
            # create random point from polygon boundary
            maxx, maxy, minx, miny = landtype.bounds
            random_point = geometry.Point(minx + (random.random() * (maxx - minx)),
                                          (miny + (random.random() * (maxy - miny))))

            # check whether it is inside the polygon
            if not landtype.contains(random_point):
                done = False
            else:
                done = True
        return random_point

    # fill random points in parallel
    found_points += Parallel(n_jobs=num_cores)(delayed(createPoint)(i) for i in range(target_tree_count))

print("\n--> generating additional attributes for trees")
# create csvRows in parallel
csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPoint)(point) for point in found_points)


# output CSV file
if output_csv:
    print("\n--> writing CSV file")
    with open('marula_trees_knp_generated.csv', 'w') as marula_csv_target:
        fieldnames = ['Lat', 'Lon', 'Height', 'Diameter', 'Age', 'Sex']

        # Create writer and write header to file
        writer = csv.DictWriter(marula_csv_target, fieldnames=fieldnames, delimiter=';')
        writer.writeheader()
        writer.writerows(csvRows)

# output shapefile with points
if output_shp:
    print("\n--> writing SHP file")
    output_file = driver.CreateDataSource("random_points.shp")
    output_layer = output_file.CreateLayer("point_out", None, ogr.wkbPoint)
    output_layer.CreateField(ogr.FieldDefn('Height', ogr.OFTReal))
    output_layer.CreateField(ogr.FieldDefn('Diameter', ogr.OFTReal))
    output_layer.CreateField(ogr.FieldDefn('Age', ogr.OFTInteger))
    output_layer.CreateField(ogr.FieldDefn('Sex', ogr.OFTInteger))

    for row in csvRows:
        feat = ogr.Feature(output_layer.GetLayerDefn())
        pt = ogr.Geometry(ogr.wkbPoint)
        #Like the comment in #createCsvRowFromPoint said: x is defined by longitude
        pt.SetPoint_2D(0, row['Lon'], row['Lat'])
        feat.SetField('Height', row['Height'])
        feat.SetField('Diameter', row['Diameter'])
        feat.SetField('Age', row['Age'])
        feat.SetField('Sex', row['Sex'])
        feat.SetGeometry(pt)

        output_layer.CreateFeature(feat)
        feat = pt = None


print("\n--> finished")
