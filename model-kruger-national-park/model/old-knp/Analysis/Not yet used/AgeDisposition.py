from pymongo import MongoClient
from bson.son import SON
import matplotlib.pyplot as plt
import numpy as np


# configure MongoDB Connection
mongoDBAddress = "mongodb://dock-three.mars.haw-hamburg.de:27017"
dbName = "SimResults"

# SimulationID is used as the name of the collection
simulationId = 'feae37ed-d898-4dc7-890e-ab9993ac4838'

# connect to database
client = MongoClient(mongoDBAddress)
db = client[dbName]
collection = db[simulationId]

# Projection for fields to output
projection = {"_id": 0, "AgentData.Age": 1}

agentTypeToQuery = "Elephant"
tickToQuery = 1

pipeline = [
    {"$match": {
        "$and": [{
            "Tick": {"$eq": tickToQuery},
            "AgentType": {"$eq": agentTypeToQuery}
        }]
    }
    },
    {"$project": projection},
    {"$group": {"_id": "$AgentData.Age", "count": {"$sum": 1}}},
    {"$sort": SON([("_id", 1)])}
]

coll = collection.aggregate(pipeline)

a = []
c = []

for elem in coll:
    c.append(elem['count'])
    a.append(elem['_id'])

ages_aranged = np.arange(1, len(a) + 1)

plt.title('Number of '+agentTypeToQuery+'s per Age in Tick '+ str(tickToQuery))
# add bars
plt.bar(ages_aranged, c, align='center')
# format x-axis to be aligned with the aranged ages
plt.xticks(ages_aranged, a, rotation=45)
# label axis
plt.ylabel('# '+agentTypeToQuery + 's')
plt.xlabel('Age in Years for ' + agentTypeToQuery + 's')

# add values above bars
for i, v in enumerate(c):
    plt.text(i + .75, v + 1.5, str(v), fontweight='bold')

plt.show()

