from pymongo import MongoClient
import pymongo as pymongo
from joblib import Parallel, delayed
from math import radians, cos, sin, asin, sqrt
from itertools import tee, izip
import multiprocessing
import csv

# get number of cores available on this machine
num_cores = multiprocessing.cpu_count()

# configure MongoDB Connection
mongoDBAddress = "mongodb://dock-three.mars.haw-hamburg.de:27017"
dbName = "SimResults"

# SimulationID is used as the name of the collection
# c54c486d-fcef-4f39-9a1f-fd4bab8a4ee5 -> most recent run
simulationId = 'e306d3d5-ab3f-431c-9f2c-75f6c5a1ab14'#'85e3e8c3-6034-4cdd-89e2-a2a608cafa5a'#'03c16d37-2cd0-4d0c-bfe3-21bcc89fe0e6'

# connect to database
client = MongoClient(mongoDBAddress)
db = client[dbName]
collection = db[simulationId]

# Projection for fields to output
projection = {"_id": 0, "Position": 1, "Tick": 1, "AgentData.Leading": 1}

agentTypeToQuery = "Elephant"


cursor = collection \
    .find({"AgentType": agentTypeToQuery, "AgentData.Herd": 542, "AgentData.Leading": True}
          , projection) \
    .sort([
        ('Tick', pymongo.ASCENDING),
        ('AgentId', pymongo.ASCENDING)])


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
    a = sin(dlat / 2) ** 2 + cos(lat1) * cos(lat2) * sin(dlon / 2) ** 2
    c = 2 * asin(sqrt(a))
    r = 6371  # Radius of earth in kilometers. Use 3956 for miles
    return c * r

# create csvRows in parallel
csvRows = []

timePerTick = 1
#csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromDocument)(doc) for doc in cursor)
elephantList = []
for elephant in cursor:
    elephantList.append(elephant)


def pairwise(iterable):
    "s -> (s0,s1), (s1,s2), (s2, s3), ..."
    a, b = tee(iterable)
    next(b, None)
    return izip(a, b)

for elephant, next_elephant in pairwise(elephantList):
    docToInsert = {}
    docToInsert['tick'] = elephant['Tick']

    pos1 = [elephant["Position"]['_v']['coordinates'][0], elephant["Position"]['_v']['coordinates'][1], ]
    pos2 = [next_elephant["Position"]['_v']['coordinates'][0], next_elephant["Position"]['_v']['coordinates'][1]]
    stepSize = haversine(pos1[0], pos1[1], pos2[0], pos2[1])
    docToInsert['step'] = stepSize
    docToInsert['speed'] = stepSize / timePerTick

    csvRows.append(docToInsert)




# output results to CSV file
with open('stepLength' + simulationId + '.csv', 'w') as simOutputCsv:
    # Create writer and write header to file
    writer = csv.DictWriter(simOutputCsv, fieldnames=['tick','step','speed'], delimiter=';')
    writer.writeheader()
    writer.writerows(csvRows)
