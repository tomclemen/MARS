
import pymongo as pymongo
import random
import folium
r = lambda: random.randint(0,255)


mongoDBAddress = "mongodb://localhost:27017"
dbName = "SimResults"

#current simulation with mitjas potential field layer
simulationId = '92b0cbba-ca69-4be4-ac10-9fbe31fe86af'
agentTypeToQuery = "Elephant"
agentId = '28410233-7f00-4dce-bd8d-22e01d831a29'

maxAgents = 3
startQueryTick = 1
endQueryTick = 100

# connect to database
client = pymongo.MongoClient(mongoDBAddress)
db = client[dbName]
collection = db[simulationId]

map = folium.Map([-24.0, 31.5], zoom_start=9)

projection = {"_id": 0, "AgentId": 1, "AgentType": 1, "Position": 1, "Tick": 5, "AgentData":1}

tick1 = collection.find({"AgentId": agentId, "AgentType": agentTypeToQuery, "Tick": 1}, projection).sort('AgentId', pymongo.ASCENDING)
# for agentInstance in tick1:
#     routeString = agentInstance["AgentData"]["route"]
#     splited_string = [x.strip() for x in routeString.split(';')]
#
#     lat = []
#     lon = []
#     route = []
#     for x in splited_string:
#         if x == "":
#             continue
#         if float(x) > 40:
#             lat.append(x)
#         else:
#             lon.append(x)
#
#     count = 0
#     for x,y in zip(lat,lon):
#         #print (x,y)
#         pos = [float(x),float(y)]
#         map.add_children(folium.Marker(pos, icon=folium.Icon(color='blue'), popup="Node " + str(count) + " Lat: " + str(pos[0]) + " Lon:" + str(pos[1])))
#         route.append(pos)
#        count += 1

#map.add_children(folium.PolyLine(route, color='blue'))

cursor = collection.find({"AgentId": agentId, "AgentType": agentTypeToQuery, "Tick": {"$gt": startQueryTick, "$lt": endQueryTick}}, projection).sort('AgentId', pymongo.ASCENDING)

locations = []
currentId = cursor[0]["AgentId"]
cursor.rewind()

col = '#%02X%02X%02X' % (r(), r(), r())
for elephant in cursor:
    #print(elephant)
    #if elephant["AgentData"]["Leading"]:
    #    col = "red"

    if currentId != elephant["AgentId"]:
        currentId = elephant["AgentId"]
        map.add_children(folium.PolyLine(locations, color='red'))
        col = '#%02X%02X%02X' % (r(), r(), r())
        locations = []
    # access a bit more complex, since position filed contains mongodb geoJSON object
    p1 = elephant["Position"]['_v'][1]
    p2 = elephant["Position"]['_v'][0]
    pos = [p1, p2]

    map.add_children(folium.Marker(pos, icon=folium.Icon(color='red'), popup="Tick " + str(elephant['Tick']) + " Lat: " + str(p1) + " Lon: " + str(p2)))

    locations.append(pos)
# also add last elephant
map.add_children(folium.PolyLine(locations, color=col))

#heatmap.add_children(plugins.HeatMap(locations))
map.save("trajectory-" + agentTypeToQuery + ".html")
