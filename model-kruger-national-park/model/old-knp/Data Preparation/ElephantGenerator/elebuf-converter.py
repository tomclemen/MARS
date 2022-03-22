import csv
import utm


def write_line(writer, write_lat, write_lon, write_herd_id, elephant_type, leading, reproduction):
    writer.writerow({'lat': write_lat, 'lon': write_lon, 'herdId': write_herd_id, 'elephantType': elephant_type,
                     'isLeading': leading, 'reproductionAge': reproduction})


with open('elebuf1989.txt', 'rb') as elebuf_source:
    reader = csv.DictReader(elebuf_source, delimiter=',')
    with open('elebuf1989 - onlyElephants.csv', 'w') as elephant_target:
        with open('elephant1989_linear_population_15k', 'w') as marsElephant_target:
            fieldnames = ['year', 'type', 'count', 'calfCount', 'lat', 'lon', 'reproductionAge']
            elephantsOnlyWriter = csv.DictWriter(elephant_target, fieldnames=fieldnames, delimiter=';',
                                                 lineterminator='\n')
            elephantsOnlyWriter.writeheader()
            marsElephantsWriter = csv.DictWriter(marsElephant_target,
                                                 fieldnames=['lat', 'lon', 'herdId', 'elephantType', 'isLeading',
                                                             'reproductionAge'], delimiter=';', lineterminator='\n')
            marsElephantsWriter.writeheader()

            herd_id = 0
            for row in reader:
                for x in range(0, 2):

                    elephantType = row['type']
                    count = int(float(row['count']))
                    calfCount = int(float(row['calfCount']))
                    utmX = float(row['utmX'])
                    utmY = float(row['utmY'])
                    reproduction = "[12, 30, 48]"
                    if elephantType in ['O', 'OB']:
                        latLong = utm.to_latlon(utmX, utmY, 36, 'J')
                        lat = latLong[0]
                        lon = latLong[1]
                        # fill file only with elephant data
                        elephantsOnlyWriter.writerow(
                            {'year': row['year'], 'type': elephantType, 'count': count,
                             'calfCount': calfCount, 'lat': lat, 'lon': lon, 'reproductionAge': reproduction})

                        # create file with fields for MARS model elephant
                        for currentAnimalNumber in range(0, count):
                            if elephantType == 'O':
                                if currentAnimalNumber < calfCount:
                                    write_line(marsElephantsWriter, lat, lon, herd_id, 'ELEPHANT_CALF', 'false',reproduction)
                                else:
                                    if currentAnimalNumber == calfCount:
                                        write_line(marsElephantsWriter, lat, lon, herd_id, 'ELEPHANT_COW', 'true',reproduction)
                                    else:
                                        write_line(marsElephantsWriter, lat, lon, herd_id, 'ELEPHANT_COW', 'false', reproduction)
                            else:
                                if currentAnimalNumber == 0:
                                    write_line(marsElephantsWriter, lat, lon, herd_id, 'ELEPHANT_BULL', 'true', reproduction)
                                else:
                                    write_line(marsElephantsWriter, lat, lon, herd_id, 'ELEPHANT_BULL', 'false', reproduction)
                        herd_id += 1



