# This file is only for the first version of MARS KNP, where we want to look at a smaller area.
import csv
import datetime
import math
from calendar import monthrange


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
    elif 3 < hour <= 15:  # 4-15
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


def write_temperature_values_for_day(_year, _month, day, min_value, max_value):
    for hour in range(0, 24):
        try:
            date_time = datetime.date(int(_year), _month, day)
            current_temperature = calculate_current_temperature(float(min_value), float(max_value),
                                                                hour)
            temperature_writer.writerow(
                {'date': date_time, 'hour': "%d:00" % (hour), 'temperature': current_temperature})
        except ValueError:
            pass
            # print "ignored date: " + year + "-" + str(month) + "-" + str(day)


def write_precipitation_values_for_month(_year, _month, month_precipitation):
    date_time = datetime.date(int(_year), _month, 1)
    precipitation_writer.writerow({'date': str(date_time), 'hour': "00:00", 'precipitationInMm': month_precipitation})


def calculate_value_average(_row):
    _sum = 0
    for _month in range(1, 13):
        _sum += float(_row[str(_month)])
    return _sum / 12


def calculate_min_temperature_value(_row):
    min_value = 100
    for _month in range(1, 13):
        value = float(_row[str(_month)])
        if value < min_value:
            min_value = value
    return min_value


def calculate_max_temperature_value(_row):
    max_value = 0
    for month in range(1, 13):
        value = float(_row[str(month)])
        if value > max_value:
            max_value = value
    return max_value


with open('aDGVM_1979-2099.csv', 'rb') as dgvmSource:
    reader = csv.DictReader(dgvmSource, delimiter=',')

    with open('dgvm_temperature.csv', 'w') as temperature_target:
        fieldnames = ['date', 'hour', 'temperature']
        temperature_writer = csv.DictWriter(temperature_target, fieldnames=fieldnames, delimiter=';',
                                            lineterminator='\n')
        temperature_writer.writeheader()

        with open('dgvm_precipitation.csv', 'w') as precipitation_target:
            fieldnames = ['date', 'hour', 'precipitationInMm']
            precipitation_writer = csv.DictWriter(precipitation_target, fieldnames=fieldnames, delimiter=';',
                                                  lineterminator='\n')
            precipitation_writer.writeheader()

            year = 1979
            month = 1
            days = monthrange(year, month)[1]

            precipitationSum = 0.0

            dayCounter = 1

            for row in reader:
                if row == 0:
                    continue

                skipTemp = False

                temp_min = row["temp_min"]
                temp_max = row["temp_max"]

                # leap year
                if month == 2 and dayCounter == 29:

                    write_precipitation_values_for_month(year, month, precipitationSum)
                    precipitationSum = 0.0

                    write_temperature_values_for_day(year, month, dayCounter, temp_min, temp_max)

                    dayCounter = 1
                    month += 1
                    days = monthrange(year, month)[1]

                elif dayCounter == (days + 1):
                    dayCounter = 1

                    write_precipitation_values_for_month(year, month, precipitationSum)
                    precipitationSum = 0.0

                    month += 1
                    # print "month " + str(month)
                    if month == 13:
                        month = 1
                        year += 1
                        print "year: " + str(year)
                        if year == 2100:
                            print "year 2100 reached"
                            break
                    days = monthrange(year, month)[1]

                write_temperature_values_for_day(year, month, dayCounter, temp_min, temp_max)

                precipitationSum += float(row["precip_in_mm"])

                dayCounter += 1
