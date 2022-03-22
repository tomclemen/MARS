import pymongo as pymongo

mongoDBAddress = "mongodb://141.22.29.141:27017"
dbName = "SimResults"

#current simulation with mitjas potential field layer
simulationId = '69428a35-d0f2-4fdf-9649-ccb138e8f53e'


# connect to database
client = pymongo.MongoClient(mongoDBAddress)
db = client[dbName]
collection = db[simulationId]


projection = {"_id": 0, "AgentId": 1, "AgentType": 1, "Tick": 1, "AgentData": 1}
agentTypeToQuery = "VegetationLayer"

cursor = collection.find({"AgentType": agentTypeToQuery, "Tick": 1}, projection)#.sort('Tick', pymongo.ASCENDING)

for vegMap in cursor:
    with open(simulationId + '-Tick-' + str(vegMap['Tick']) + '-asc.asc', 'a') as result:
        # first write header information:
        result.write('NCOLS ' + str(vegMap['AgentData']['layerDTO']['NumberOfGridCellsX']) + '\n')
        result.write('NROWS ' + str(vegMap['AgentData']['layerDTO']['NumberOfGridCellsY']) + '\n')
        result.write('XLLCORNER ' + str(vegMap['AgentData']['layerDTO']['BottomLat']) + '\n')
        result.write('YLLCORNER ' + str(vegMap['AgentData']['layerDTO']['LeftLong']) + '\n')
        result.write('CELLSIZE ' + str(vegMap['AgentData']['layerDTO']['CellSizeInM']) + '\n')
        result.write('NODATA_VALUE ' + '-9999' + '\n')

        # then content
        i = 1
        for elem in vegMap['AgentData']['layerDTO']['Content']:
            result.write(str(elem) + ' ')

            if (i == vegMap['AgentData']['layerDTO']['NumberOfGridCellsX']):
                result.write('\n')
                i = 0
            i += 1

        result.close()

