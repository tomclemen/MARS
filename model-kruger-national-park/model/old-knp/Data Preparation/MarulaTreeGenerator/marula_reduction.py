# This file is only for the first version of MARS KNP, where we want to look at a smaller area.
import csv

with open('marula_trees_knp_generated.csv', 'rb') as marula_source:
    reader = csv.DictReader(marula_source, delimiter=';')

    with open('reduced marulas for southern KNP.csv', 'w') as marula_target:
        fieldnames = ['Lat', 'Lon', 'Height', 'Diameter', 'Age', 'Sex']
        marulaWriter = csv.DictWriter(marula_target, fieldnames=fieldnames, delimiter=';', lineterminator='\n')
        marulaWriter.writeheader()

        print '--> start copying'

        for row in reader:
            lat = float(row['Lat'])
            lng = float(row['Lon'])
            if lat < -24.886:
                marulaWriter.writerow(
                    {'Lat': row['Lat'], 'Lon': row['Lon'], 'Height': row['Height'], 'Diameter': row['Diameter'],
                     'Age': row['Age'], 'Sex': row['Sex']})
