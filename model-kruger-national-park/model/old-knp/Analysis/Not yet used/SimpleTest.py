from pymongo import MongoClient
from bson.code import Code
from pprint import pprint


mongoDBAddress = "mongodb://dock-three.mars.haw-hamburg.de:27017"
dbName = "SimResults"
simulationId = 'c6364c54-02f8-40f2-ab89-761d56a5637a'


client = MongoClient(mongoDBAddress)
db = client[dbName]

collection = db[simulationId]
tick = 1
cursor = collection.find({"AgentType": "Elephant", "AgentData.Herd": 800, "Tick": tick}, {"_id": 0, "AgentData": 1, "Position": 1})
for e in cursor:
    pprint(e)

pprint(collection.find({"AgentType": "Elephant", "AgentData.Herd": 800, "Tick": tick}).count())
