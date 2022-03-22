import csv
import sys
from shapely import geometry

try:
    from osgeo import ogr, osr, gdal
except:
    sys.exit('ERROR: cannot find GDAL/OGR modules')

elephant_points = []

with open('census2011.csv', 'rb') as elephant_source:
    reader = csv.DictReader(elephant_source, delimiter=';')

    with open('knp_elephants_extracted.csv', 'w') as elephant_target:
        fieldnames = ['id', 'herd_id', 'type', 'lat', 'long', 'region', 'section', 'leading']

        # Create writer and write header to file
        writer = csv.DictWriter(elephant_target, fieldnames=fieldnames, delimiter=';')
        writer.writeheader()

        # limit valid entries to o=elephant_cow, ob=bulls and ok=calfs
        valid_entries = ['Elephant Bulls', 'Elephant Herd']
        type_mapping = {'Elephant Herd': 'ELEPHANT_COW', 'Elephant Bulls': 'ELEPHANT_BULL'}

        id = 0
        herd_id = 0
        for row in reader:
            species = row['SPECIES']
            # skip if entry is not valid for us
            if species not in valid_entries:
                continue

            leaderSet = 'false'
            calves = int(row['CALVES'])
            for x in range(0, int(row['TOTAL'])):

                agentType = type_mapping[species]
                leading = 'false'

                # check that there are not more calves than total animals
                if not species == 'Elephant Bulls':
                    if not calves >= int(row['TOTAL']) and calves > 0:
                        agentType = 'ELEPHANT_CALF'
                        calves -= 1
                    else:
                        # all calves have been created, make sure a leading elephant is created
                        if leaderSet == 'false' and species == 'Elephant Herd':
                            leading = leaderSet = 'true'

                writer.writerow(
                        {'id': id,
                         'herd_id': herd_id,
                         'type': agentType,
                         'lat': row['LATITUDE'],
                         'long': row['LONGITUDE'],
                         'region': row['KREGION'],
                         'section': row['KSECTION'],
                         'leading': leading})
                id += 1
                elephant_points.append(geometry.Point(float(row['LONGITUDE']), float(row['LATITUDE'])))
            # end for x
            herd_id += 1
            # end for row

# write points to shapefile
driver = ogr.GetDriverByName('ESRI Shapefile')
output_file = driver.CreateDataSource("elephants_extracted.shp")
output_layer = output_file.CreateLayer("point_out", None, ogr.wkbPoint)
for point in elephant_points:
    feat = ogr.Feature(output_layer.GetLayerDefn())
    pt = ogr.Geometry(ogr.wkbPoint)
    pt.SetPoint_2D(0, point.x, point.y)
    feat.SetGeometry(pt)
    output_layer.CreateFeature(feat)

output_layer = None
print "\n--> finished"
