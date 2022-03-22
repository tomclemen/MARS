import csv
import sys
from shapely import geometry
try:
    from osgeo import ogr, osr, gdal
except:
    sys.exit('ERROR: cannot find GDAL/OGR modules')


elephant_points = []

with open('knp_elephants_source.csv', 'rb') as elephant_source:
    reader = csv.DictReader(elephant_source, delimiter=';')

    with open('knp_elephants_final.csv', 'w') as elephant_target:
        fieldnames = ['id', 'herd_id', 'type', 'lat', 'long', 'region', 'leading']

        # Create writer and write header to file
        writer = csv.DictWriter(elephant_target, fieldnames=fieldnames, delimiter=';')
        writer.writeheader()

        # limit valid entries to o=elephant_cow, ob=bulls and ok=calfs
        valid_entries = ['o', 'ob', 'ok']
        type_mapping = {'o': 'ELEPHANT_COW', 'ob': 'ELEPHANT_BULL', 'ok': 'ELEPHANT_CALF'}

        id = 0
        herd_id = 0
        for row in reader:

            # skip if entry is not valid for us
            if not row['SP'] in valid_entries:
                continue

            for x in range(0, int(row['NO'])):
                if x == 0 and row['SP'] == 'o':
                    leading = 'true'
                else:
                    leading = 'false'

                writer.writerow(
                        {'id': id,
                         'herd_id': herd_id,
                         'type': type_mapping[row['SP']],
                         'lat': row['LAT'],
                         'long': row['LONG'],
                         'region': row['NAME'],
                         'leading': leading})
                id += 1
                elephant_points.append(geometry.Point(float(row['LONG']), float(row['LAT'])))
            # end for x
            herd_id += 1
        # end for row

# write points to shapefile
driver = ogr.GetDriverByName('ESRI Shapefile')
output_file = driver.CreateDataSource("elephants_extracted.shp")
output_layer = output_file.CreateLayer("point_out", None, ogr.wkbPoint )
for point in elephant_points:
    feat = ogr.Feature(output_layer.GetLayerDefn())
    pt = ogr.Geometry(ogr.wkbPoint)
    pt.SetPoint_2D(0, point.x, point.y)
    feat.SetGeometry(pt)
    output_layer.CreateFeature(feat)

output_layer = None
print "\n--> finished"

