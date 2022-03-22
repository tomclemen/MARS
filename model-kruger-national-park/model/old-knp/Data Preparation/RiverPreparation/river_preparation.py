import sys

from math import radians, cos, sin, asin, sqrt

try:
    from osgeo import ogr, osr, gdal
except:
    sys.exit('ERROR: cannot find GDAL/OGR modules')

def haversine(lon1, lat1, lon2, lat2):
    """
    Calculate the great circle distance between two points
    on the earth (specified in decimal degrees)
    """
    # convert decimal degrees to radians
    lon1, lat1, lon2, lat2 = map(radians, [lon1, lat1, lon2, lat2])
    # haversine formula
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = sin(dlat/2)**2 + cos(lat1) * cos(lat2) * sin(dlon/2)**2
    c = 2 * asin(sqrt(a))
    km = 6367 * c
    return km

driver = ogr.GetDriverByName('ESRI Shapefile')

# import shapefiles
rivers_shape = driver.Open("only large rivers.shp")
rivers_layer = rivers_shape.GetLayer()
number_of_river_parts = rivers_layer.GetFeatureCount()


#init new shape file
print("\n--> writing SHP file")
output_file = driver.CreateDataSource("myrivers500.shp")
output_layer = output_file.CreateLayer("rivers", None, ogr.wkbLineString)
output_layer.CreateField(ogr.FieldDefn('FROM_NODE', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('TO_NODE', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('Str_order', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('Shape_Leng', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('GridID', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('HydroID', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('NextDownID', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('ET_FNode', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('ET_TNode', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('LineSegs', ogr.OFTInteger))
output_layer.CreateField(ogr.FieldDefn('Sinuousity', ogr.OFTInteger))


# iterate over all features and their points
feat = rivers_layer.GetNextFeature()
while feat is not None:
    linestring = ogr.Geometry(ogr.wkbLineString)
    previous_point = None

    for point in feat.GetGeometryRef().GetPoints():
        very_last_point = point
        #first item
        if previous_point is None:
            #first point needs to be added
            linestring.AddPoint(point[0], point[1])
            previous_point = point
        #other items
        else:
            distance = haversine(previous_point[0], previous_point[1], point[0], point[1])
            #distance should be greater then 500 meters
            if distance > 0.5:
                previous_point = point
                linestring.AddPoint(point[0], point[1])
    #last point also has to be added
    linestring.AddPoint(very_last_point[0], very_last_point[1])

    new_feat = ogr.Feature(output_layer.GetLayerDefn())
    new_feat.SetGeometry(linestring)
    new_feat.SetField('FROM_NODE', feat.GetFieldAsInteger('FROM_NODE'))
    new_feat.SetField('TO_NODE', feat.GetFieldAsInteger('TO_NODE'))
    new_feat.SetField('Str_order', feat.GetFieldAsInteger('Str_order'))
    new_feat.SetField('Shape_Leng', feat.GetFieldAsInteger('Shape_Leng'))
    new_feat.SetField('GridID', feat.GetFieldAsInteger('GridID'))
    new_feat.SetField('HydroID', feat.GetFieldAsInteger('HydroID'))
    new_feat.SetField('NextDownID', feat.GetFieldAsInteger('NextDownID'))
    new_feat.SetField('ET_FNode', feat.GetFieldAsInteger('ET_FNode'))
    new_feat.SetField('ET_TNode', feat.GetFieldAsInteger('ET_TNode'))
    new_feat.SetField('LineSegs', feat.GetFieldAsInteger('LineSegs'))
    new_feat.SetField('Sinuousity', feat.GetFieldAsInteger('Sinuousity'))
    output_layer.CreateFeature(new_feat)

    feat = rivers_layer.GetNextFeature()

