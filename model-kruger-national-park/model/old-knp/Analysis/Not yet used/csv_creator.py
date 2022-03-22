from pymongo import MongoClient
import pymongo as pymongo
from joblib import Parallel, delayed
from math import radians, cos, sin, asin, sqrt
import multiprocessing
import csv

# get number of cores available on this machine
num_cores = multiprocessing.cpu_count()

# configure MongoDB Connection
mongoDBAddress = "mongodb://dock-three.mars.haw-hamburg.de:27017"
dbName = "SimResults"

# SimulationID is used as the name of the collection
simulationId = '419c0b57-b7d7-4c00-9858-1d15ddac13ce'

# connect to database
client = MongoClient(mongoDBAddress)
db = client[dbName]
collection = db[simulationId]

# Projection for fields to output
projection = {"_id": 0, "AgentId": 1, "AgentType": 1, "AgentData.Herd": 1, "Position": 1, "Tick": 1}

agentTypeToQuery = "Elephant"

# query for all fieldnames and flatten AgentData
exampleDoc = collection.find_one({"AgentType": agentTypeToQuery, "Tick": 1}, projection)
fieldNames = []
for key in exampleDoc:
    if key == "Position":
        fieldNames.append("long")
        fieldNames.append("lat")
    if key == "AgentData":
        for k in exampleDoc[key]:
            fieldNames.append(k)
    else:
        fieldNames.append(key)

# uncomment to create index, if you plan to sort the result by a certain field and the output is very large

# collection.create_index([("AgentId", pymongo.ASCENDING)])

###############################
#####  DEFINE QUERY HERE! #####
###############################
cursor = collection \
    .find({"AgentType": agentTypeToQuery, "AgentData.Herd": 800}
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


def createCsvRowFromDocument(doc):
    # makes sure agentData fields are flattened
    docToInsert = {}
    for key in doc:
        if key == "Position":
            docToInsert["long"] = doc[key][2]
            docToInsert["lat"] = doc[key][0]
        elif key == "AgentData":
            for k in doc[key]:
                docToInsert[k] = doc[key][k]
        else:
            docToInsert[key] = doc[key]
    return docToInsert


# create csvRows in parallel
csvRows = []
csvRows += Parallel(n_jobs=num_cores)(delayed(createCsvRowFromDocument)(doc) for doc in cursor)

# output results to CSV file
with open('simOutput' + simulationId + '.csv', 'w') as simOutputCsv:
    # Create writer and write header to file
    writer = csv.DictWriter(simOutputCsv, fieldnames=fieldNames, delimiter=';')
    writer.writeheader()
    writer.writerows(csvRows)
