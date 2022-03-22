import math


def calculate_decimal_degrees_by_meters(
        distance_in_meters, distance_of_one_arc_second=30.87):
    """
    Gets the decimal degrees from meters.
    The default distance of one arc second is set to 30.9 meters,
    which is good for the equator. Set accordingly for other degree of latitude.
    Default value for distance of one arc second:
        http://www.esri.com/news/arcuser/0400/wdside.html

    :param distance_in_meters: Distance in meters.
    :type distance_in_meters: float
    :param distance_of_one_arc_second: Distance of one arc second.
    :type distance_of_one_arc_second: float
    :return: The decimal degrees by meters.
    :rtype: float
    """
    arc_seconds = distance_in_meters / float(distance_of_one_arc_second)
    return arc_seconds / 60.0 / 60.0


def calculate_decimal_degrees_by_meters_for_longitude(
        distance_in_meters, decimal_latitude):
    """
    Gets the decimal degrees from meters.
    The default distance of one arc second is set to 30.9 meters,
    which is good for the equator. Set accordingly for other degree of latitude.
    Default value for distance of one arc secound:
        http://www.esri.com/news/arcuser/0400/wdside.html

    :param distance_in_meters: Distance in meters.
    :type distance_in_meters: float
    :param decimal_latitude: Lat value of the coordinate
    :type decimal_latitude: float
    :return: The decimal degrees by meters.
    :rtype: float
    """
    arc_seconds = distance_in_meters / 30.87 * math.cos(
        decimal_latitude * math.pi / 180.0)
    return arc_seconds / 60.0 / 60.0


def deg2rad(degrees):
    return degrees * (math.pi / 180.0)


def calculate_distance_from_lat_lon_as_km(
        latitude1, longitude1, latitude2, longitude2):
    """
    Calculates the distance between two positions in km.
    Copying and Pasting from stack overflow for dummies:
    http://stackoverflow.com/questions/27928/calculate-distance-between-two
        -latitude-longitude-points-haversine-formula

    :param latitude1: Lat value from position one
    :type latitude1: float
    :param longitude1: Lon value from position one
    :type longitude2: float
    :param latitude2: Lat value from position two
    :type latitude2: float
    :param longitude2: Lon value from position two
    :type longitude2: float
    :return: The distance from lat lon in km.
    :rtype: float
    """
    earth_radius = 6371 #kilometers
    distance_latitude = deg2rad(latitude2 - latitude1)
    distance_longitude = deg2rad(longitude2 - longitude1)
    a = (math.sin(distance_latitude / 2.0) ** 2
         + math.cos(deg2rad(latitude1)) * math.cos(deg2rad(latitude2))
         * math.sin(distance_longitude / 2.0) ** 2)
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
    return earth_radius * c
