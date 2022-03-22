import os
from joblib import Parallel, delayed
import multiprocessing
from shapely import wkb, geometry, speedups
import random
import sys
import csv
import time

# starting time to calculate process duration
start = time.time()


# calculate the diameter of the created Marulas
# based on their age
def calculateDiameterInMForYears(years):
    currentDiameterInCm = 0.0
    for i in range(0, years):
        currentDiameterInCm = currentDiameterInCm + (-1 * 0.068 * currentDiameterInCm) + 4.54
    return currentDiameterInCm / 100


# create rows to write in the csv file with the created marulas
# the point states the geo coordinates where it will be
# the min max values are used to create a randomized height
# between those two values
def createCsvRowFromPointAndHeight(point, min, max):
    age = random.randint(10, 150)
    sexRand = random.uniform(0, 100)
    sex = 0 if sexRand < 55 else 1
    height = random.uniform(min,max)

    # horizontal extension is described by longitude, therefore x is lon and y is lat
    return {'Lat': point.y,
            'Lon': point.x,
            'Height': height,
            'Diameter': calculateDiameterInMForYears(age),
            'Age': age,
            'Sex': sex}

try:
    from osgeo import ogr, osr, gdal
except:
    sys.exit('ERROR: cannot find GDAL/OGR modules')



######################################################################################
################### Params ###########################################################
######################################################################################

# output csv?
output_csv = True
# output shp?
output_shp = True
# calc small marula positions?
createLowerMarulas = True
# calc tall marula positions?
createTallMarulas = False

######################################################################################
######################################################################################
######################################################################################




# delete old files
try:
    if output_shp:
        os.remove('marulasVenter.dbf')
        os.remove('marulasVenter.shp')
        os.remove('marulasVenter.shx')
    if output_csv:
        os.remove('marulasVenter.csv')
except:
    print("No files to remove, continuing")



print("Starting")



# get number of cores available on this machine
num_cores = multiprocessing.cpu_count()
# setup shapefile driver
driver = ogr.GetDriverByName('ESRI Shapefile')
# import shapefiles
knp_landtype_shapefile = driver.Open("landtype_wEstMarulaDens.shp")
# get layers
knp_landtypes = knp_landtype_shapefile.GetLayer()

number_of_landtypes = knp_landtypes.GetFeatureCount()
# print ("Number of Lantypes: ", number_of_landtypes)

csvRows = []
createdMarulas = []
counterForCalculatedAreas = 0
# array to hold found point
found_points_for_tall_marulas = []
found_points_for_lower_marulas=[]

for i in xrange(number_of_landtypes):

    # empty all lists
    createdMarulas = []

    # read features from gis file
    landtype_feature = knp_landtypes.GetFeature(i)
    landtype_geom = landtype_feature.GetGeometryRef()

    # get size of the area
    areaInHectares = landtype_feature.GetField("AREA")

    # tall marulas
    tallMarulas = float(landtype_feature.GetField("ESTTALLMRL"))
    numberOfTallMarulasToCreate = int(areaInHectares * tallMarulas)

    # lower marulas
    lowerMarulas = int(landtype_feature.GetField("ESTLWRMRLD"))
    numberOfLowerMarulasToCreate = int(areaInHectares * lowerMarulas)

    # Land Type of the area
    ltype = landtype_feature.GetField("LTYPE")
    # primary key in landtype_wEstMarulaDens.shp
    autoId = landtype_feature.GetField("AUTO_ID")
    # load feature as WKB
    landtype = wkb.loads(landtype_geom.ExportToWkb())


    def createPoint():
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


    ######################################################################################
    ################### Tall Marulas #####################################################
    ######################################################################################

    if createTallMarulas == True:
        if tallMarulas == 2.0:
            counterForCalculatedAreas += 1
            print ("AutoID: ", autoId, "LTYPE: ", ltype, "Tall Marula Column Value: ", tallMarulas, " Desired Count of Trees: ",
                   numberOfTallMarulasToCreate)
            # Marulas 2-5m
            countOfMarulasInDesiredHeigthClass = int (numberOfTallMarulasToCreate * 0.08)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point,2,5) for point in createdMarulas)

            # Marulas 5-8m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.1)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 5, 8) for point in createdMarulas)

            # Marulas 8-11m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.44)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 8, 11) for point in createdMarulas)

            # Marulas 11-14m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.3)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 11, 14) for point in createdMarulas)

            # Marulas 14-16
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.08)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 14, 16) for point in createdMarulas)

        if tallMarulas == 0.5:
            counterForCalculatedAreas += 1
            print (
            "AutoID: ", autoId, "LTYPE: ", ltype, "Tall Marula Column Value: ", tallMarulas, " Desired Count of Trees: ",
            numberOfTallMarulasToCreate)
            # Marulas 2-5m
            countOfMarulasInDesiredHeigthClass = int (numberOfTallMarulasToCreate * 0.01)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point,2,5) for point in createdMarulas)

            # Marulas 5-8m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.01)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 5, 8) for point in createdMarulas)

            # Marulas 8-11m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.35)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 8, 11) for point in createdMarulas)

            # Marulas 11-14m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.5)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 11, 14) for point in createdMarulas)

            # Marulas 14-16
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.13)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 14, 16) for point in createdMarulas)

        if tallMarulas == 0.05:
            counterForCalculatedAreas += 1
            print (
            "AutoID: ", autoId, "LTYPE: ", ltype, "Tall Marula Column Value: ", tallMarulas, " Desired Count of Trees: ",
            numberOfTallMarulasToCreate)
            # Marulas 2-5m
            countOfMarulasInDesiredHeigthClass = int (numberOfTallMarulasToCreate * 0.01)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point,2,5) for point in createdMarulas)

            # Marulas 5-8m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.01)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 5, 8) for point in createdMarulas)

            # Marulas 8-11m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.35)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 8, 11) for point in createdMarulas)

            # Marulas 11-14m
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.5)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 11, 14) for point in createdMarulas)

            # Marulas 14-16
            countOfMarulasInDesiredHeigthClass = int(numberOfTallMarulasToCreate * 0.13)
            createdMarulas = Parallel(n_jobs=num_cores)(delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromPointAndHeight)(point, 14, 16) for point in createdMarulas)




    ######################################################################################
    ################### Lower Marulas ####################################################
    ######################################################################################

    if createLowerMarulas == True:
        if lowerMarulas == 28:
            counterForCalculatedAreas += 1
            print ("AutoID: ", autoId, "LTYPE: ", ltype, "Lower Marula Column Value: ", lowerMarulas,
                   " Desired Count of Trees: ",
                   numberOfLowerMarulasToCreate)
            # Marulas 0-0.25m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.15)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 0, 0.25) for point in createdMarulas)

            # Marulas 0.25-1m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.7)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 0.25, 1) for point in createdMarulas)

            # Marulas 1-2m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.15)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 1, 2) for point in createdMarulas)

        if lowerMarulas == 14:
            counterForCalculatedAreas += 1
            print (
                "AutoID: ", autoId, "LTYPE: ", ltype, "Lower Marula Column Value: ", lowerMarulas,
                " Desired Count of Trees: ",
                numberOfLowerMarulasToCreate)
            # Marulas 0-0.25m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.2)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 0, 0.25) for point in createdMarulas)

            # Marulas 0.25-1m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.65)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 0.25, 1) for point in createdMarulas)

            # Marulas 1-2m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.15)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 1, 2) for point in createdMarulas)

        if lowerMarulas == 1.4:
            counterForCalculatedAreas += 1
            print (
                "AutoID: ", autoId, "LTYPE: ", ltype, "Lower Marula Column Value: ", lowerMarulas,
                " Desired Count of Trees: ",
                numberOfLowerMarulasToCreate)
            # Marulas 0-0.25m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.2)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 0, 0.25) for point in createdMarulas)

            # Marulas 0.25-1m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.65)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 0.25, 1) for point in createdMarulas)

            # Marulas 1-2m
            countOfMarulasInDesiredHeigthClass = int(numberOfLowerMarulasToCreate * 0.15)
            createdMarulas = Parallel(n_jobs=num_cores)(
                delayed(createPoint)() for i in range(countOfMarulasInDesiredHeigthClass))
            csvRows += Parallel(n_jobs=num_cores)(
                delayed(createCsvRowFromPointAndHeight)(point, 1, 2) for point in createdMarulas)



print ("Calculated Marulas for ", counterForCalculatedAreas, " Areas")
endOfGenerating = time.time()
print(endOfGenerating - start, " seconds from start to end of generation")

if output_csv:
    print("\n--> writing CSV file")
    with open('marulasVenter.csv', 'w') as marula_csv_target:
        fieldnames = ['Lat', 'Lon', 'Height', 'Diameter', 'Age', 'Sex']

        # Create writer and write header to file
        writer = csv.DictWriter(marula_csv_target, fieldnames=fieldnames, delimiter=';')
        writer.writeheader()
        writer.writerows(csvRows)

# output shapefile with points
if output_shp:
    print("\n--> writing SHP file")
    output_file = driver.CreateDataSource("marulasVenter.shp")
    output_layer = output_file.CreateLayer("point_out", None, ogr.wkbPoint)
    output_layer.CreateField(ogr.FieldDefn('Height', ogr.OFqgisTReal))
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


end = time.time()
print(end - start, " seconds from start to end")

