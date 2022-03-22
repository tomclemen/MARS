import re

import os
from Tkinter import *
import matplotlib.pyplot as plt
durationRegex = re.compile("Executed Tick (\d+) in (\d+) ms.")
initRegex = re.compile("...done in (\d+)ms.*")

for root, dirs, filenames in os.walk('.'):
    for f in filenames:
        if f.endswith('.txt'):
            durationAry = []
            initDuration = 0
            with open(f) as input_data:
                for line in input_data:
                    if(initDuration == 0):
                        initdur = initRegex.search(line)
                        if(initdur):
                            initDuration = initdur.group(1)
                    dur = durationRegex.search(line)
                    if(dur):
                        durationAry.append(int(dur.group(2)))

            print('Stats for file ' + f + ':')
            print('Initialization: ' + str(initDuration) + 'ms or ' + str(int(initDuration)/1000/60) + 'm')
            print('Mean Tick Duration: ' + str(sum(durationAry) / len(durationAry)) + 'ms')
            print('\n')


            plt.plot(durationAry)
            plt.ylabel('Duration in ms')
            plt.show()#plt.savefig(f+'.png')
