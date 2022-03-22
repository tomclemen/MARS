# This file is only for the first version of MARS KNP, where we want to look at a smaller area.
import csv
import datetime
import math


def diff(max_value, min_value):
    return abs(max_value - min_value)


def calculate_current_temperature(min_value, max_value, hour):
    difference = max_value - min_value
    if hour < 4:  # 0-3
        parts_from_min = 3 - hour
        x = math.pi - ((parts_from_min / 12.) * math.pi)
        cos_x = math.cos(x)
        percent_of_difference_to_add = (2. - diff(1., cos_x)) / 2.
        return min_value + difference * percent_of_difference_to_add
    elif hour > 3 and hour <= 15:  # 4-15
        parts_from_min = hour - 3
        x = math.pi - ((parts_from_min / 12.) * math.pi)
        cos_x = math.cos(x)
        percent_of_difference_to_add = (2. - diff(1., cos_x)) / 2.
        return min_value + difference * percent_of_difference_to_add
    else:  # 16-23
        parts_from_min = 27 - hour
        x = math.pi - ((parts_from_min / 12.) * math.pi)
        cos_x = math.cos(x)
        percent_of_difference_to_add = (2. - diff(1., cos_x)) / 2.
        return min_value + difference * percent_of_difference_to_add


def write_temperature_values_for_year(writer, year, min_value_row, max_value_row):
    for month in range(1, 13):
        for day in range(1, 32):
            for hour in range(0, 24):
                try:
                    date_time = datetime.date(int(year), month, day)
                    min_temperature = min_value_row[str(month)]
                    max_temperature = max_value_row[str(month)]
                    current_temperature = calculate_current_temperature(float(min_temperature), float(max_temperature),
                                                                        hour)
                    temperature_writer.writerow(
                        {'date': str(date_time), 'hour': "%d:00" % (hour), 'temperature': current_temperature})
                except ValueError:
                    pass
                    #print "ignored date: " + year + "-" + str(month) + "-" + str(day)


def write_precipitation_values_for_year(writer, year, precipitation_row) :
    for month in range(1, 13):
        date_time = datetime.date(int(year), month, 1)
        month_precipitation = precipitation_row[str(month)]
        writer.writerow({'date': str(date_time), 'hour': "00:00", 'precipitationInMm': month_precipitation})

def calculate_value_average(row) :
    sum = 0
    for month in range(1,13) :
        sum += float(row[str(month)])
    return sum / 12

def calculate_min_temperature_value(row) :
    min_value = 100
    for month in range(1, 13):
        value = float(row[str(month)])
        if value < min_value :
            min_value = value
    return min_value

def calculate_max_temperature_value(row) :
    max_value = 0
    for month in range(1, 13):
        value = float(row[str(month)])
        if value > max_value :
            max_value = value
    return max_value

with open('ipcc.txt', 'rb') as ipcc_source:
    reader = csv.DictReader(ipcc_source, delimiter=';')

    with open('ipcc temperature mars compatible.csv', 'w') as temperature_target:
        fieldnames = ['date', 'hour', 'temperature']
        temperature_writer = csv.DictWriter(temperature_target, fieldnames=fieldnames, delimiter=';',
                                            lineterminator='\n')
        temperature_writer.writeheader()

        with open('ipcc precipitation mars compatible.csv', 'w') as precipitation_target:
            fieldnames = ['date', 'hour', 'precipitationInMm']
            precipitation_writer = csv.DictWriter(precipitation_target, fieldnames=fieldnames, delimiter=';',
                                                  lineterminator='\n')
            precipitation_writer.writeheader()

            counter = 0
            min_values = None
            max_values = None
            precipitation_values = None
            for row in reader:
                if counter == 0:
                    precipitation_values = row
                    counter += 1
                elif counter == 1:
                    min_values = row
                    counter += 1
                    print "Min value average: " + str(calculate_value_average(min_values))
                    print "Min value: " + str(calculate_min_temperature_value(min_values))
                elif counter == 2:
                    counter = 0
                    max_values = row
                    print "Max value average:" + str(calculate_value_average(max_values))
                    print "Max value: " + str(calculate_max_temperature_value(max_values))
                    write_precipitation_values_for_year(precipitation_writer, row['year'], precipitation_values)
                    write_temperature_values_for_year(temperature_writer, row['year'], min_values, max_values)
