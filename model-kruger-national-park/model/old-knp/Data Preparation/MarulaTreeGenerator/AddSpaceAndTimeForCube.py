import csv
import sys
from shapely import geometry
from joblib import Parallel, delayed
import multiprocessing
try:
    from osgeo import ogr, osr, gdal
except:
    sys.exit('ERROR: cannot find GDAL/OGR modules')

elephant_points = []

with open('marula_trees_knp_generated_5million.csv', 'rb') as elephant_source:
    reader = csv.DictReader(elephant_source, delimiter=';')

    with open('marula_trees_knp_generated_5million_with_CubeRef.csv', 'w') as elephant_target:
        fieldnames = ['Lat','Lon','Height','Diameter','Age','Sex','d_space','d_time']
        # Create writer and write header to file
        writer = csv.DictWriter(elephant_target, fieldnames=fieldnames, delimiter=';')
        writer.writeheader()

        newRows = []
        i = 13923
        for row in reader:
            newRows.append({'Lat': row['Lat'],
                            'Lon': row['Lon'],
                            'Height': row['Height'],
                            'Diameter': row['Diameter'],
                            'Age': row['Age'],
                            'Sex': row['Sex'],
                            'd_space': i,
                            'd_time': i})
            i += 1
        writer.writerows(newRows)

