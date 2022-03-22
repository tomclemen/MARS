import pymongo as pymongo
import random
import folium
from folium import plugins

r = lambda: random.randint(0,255)

mongoDBAddress = "mongodb://dock-three.mars.haw-hamburg.de:27017"
dbName = "SimResults"

#current simulation with mitjas potential field layer
simulationId = '9bfc6e32-cfb8-4b86-a548-d583f35c5bac'

#old simulation with waterpoints
waterpointId = 'c3c50bf4-1213-4977-9623-efc203651737'

# connect to database
client = pymongo.MongoClient(mongoDBAddress)
db = client[dbName]
collection = db[simulationId]

#collection waterpoint
wpCollection = db[waterpointId]

projection = {"_id": 0, "AgentId": 1, "AgentType": 1, "Position": 1, "AgentData": 1, "Tick":1}
agentTypeToQuery = "Elephant"
tick = 24
maxAgents = 20000

#cursor = collection.find({"AgentData.Herd": 542, "AgentType": agentTypeToQuery, "Tick": {"$lt": 120}}, projection).sort('AgentId', pymongo.ASCENDING)
cursor = collection.find({"AgentType": agentTypeToQuery, "Tick": {"$lt": 24}}, projection)#.sort('AgentId', pymongo.ASCENDING)
#"AgentData.Herd": 300,
wp_cursor = wpCollection.find({"AgentType": "WaterPoint", "Tick": 1}, {"Position": 1})

locations = []
currentId = cursor[0]["AgentId"]
cursor.rewind()

map = folium.Map([-23.7, 31.5], zoom_start=8)

for elephant in cursor:
    pos = [elephant["Position"]['_v']['coordinates'][1], elephant["Position"]['_v']['coordinates'][0]]
    locations.append(pos)

map.add_children(plugins.HeatMap(locations))
map.save("heatmap-" + agentTypeToQuery + ".html")
