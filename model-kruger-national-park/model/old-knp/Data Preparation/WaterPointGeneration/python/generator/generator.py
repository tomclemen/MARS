import csv
import math
import logging
import locale

from . import distance


logger = logging.getLogger('potential_field_generator')


POTENTIAL_VALUE = 100


class PotentialFieldGenerator:
    """Generates a potential field for a given csv file
    """

    def __init__(self, latitude_top, longitude_left, latitude_bottom,
                 longitude_right, grid_length_in_meter,
                 potential_depth_in_cells):
        self.__latitude_top = float(latitude_top)
        self.__longitude_left = float(longitude_left)
        self.__latitude_bottom = float(latitude_bottom)
        self.__longitude_right = float(longitude_right)

        self.__grid_length_in_meter = int(grid_length_in_meter)
        self.__potential_depth_in_cells = int(potential_depth_in_cells)

        temporary_distance_top = distance.calculate_distance_from_lat_lon_as_km(
            latitude1=latitude_top, longitude1=longitude_left,
            latitude2=latitude_top, longitude2=longitude_right)
        missing_horizontal_distance = (
            distance.calculate_decimal_degrees_by_meters_for_longitude(
                self.__grid_length_in_meter
                - int(temporary_distance_top * 1000
                      % self.__grid_length_in_meter), latitude_top))
        temporary_distance_left = (
            distance.calculate_distance_from_lat_lon_as_km(
                longitude1=longitude_left, latitude1=latitude_top,
                longitude2=longitude_left, latitude2=latitude_bottom))
        missing_vertical_distance = (
            distance.calculate_decimal_degrees_by_meters(
                self.__grid_length_in_meter
                - int(temporary_distance_left * 1000
                      % self.__grid_length_in_meter)))

        self.__longitude_right += missing_horizontal_distance
        self.__latitude_bottom -= missing_vertical_distance

        self.__latitude_distance = math.fabs(self.__latitude_bottom
                                             - self.__latitude_top)
        self.__longitude_distance = math.fabs(self.__longitude_right
                                              - self.__longitude_left)

        distance_top = distance.calculate_distance_from_lat_lon_as_km(
            latitude1=self.__latitude_top, longitude1=self.__longitude_left,
            latitude2=self.__latitude_top, longitude2=self.__longitude_right)
        distance_left = distance.calculate_distance_from_lat_lon_as_km(
            latitude1=self.__latitude_top, longitude1=self.__longitude_left,
            latitude2=self.__latitude_bottom, longitude2=self.__longitude_left)
        self.__number_of_grid_cells_x = int(round(
            distance_top * 1000 / self.__grid_length_in_meter, 0))
        self.__number_of_grid_cells_y = int(round(
            distance_left * 1000 / self.__grid_length_in_meter, 0))

        logger.info('Number of cells in x direction: %d',
                    self.__number_of_grid_cells_x)
        logger.info('Number of cells in y direction: %d',
                    self.__number_of_grid_cells_y)

        self.__potential_field = [0.0 for _ in range(
            0, self.__number_of_grid_cells_x * self.__number_of_grid_cells_y)]

    def __get_neighbor_cells(self, current_cell):
        neighbors = []
        upper_most_row = current_cell < self.__number_of_grid_cells_x
        bottom_most_row = current_cell > self.__number_of_grid_cells_x * (
            self.__number_of_grid_cells_y - 1)
        left_column = (current_cell == 0) or (
            current_cell % self.__number_of_grid_cells_x == 0)
        right_column = (current_cell != 0) and (
            current_cell % self.__number_of_grid_cells_x
            == self.__number_of_grid_cells_x - 1)

        if not upper_most_row:
            cell = current_cell - self.__number_of_grid_cells_x
            neighbors.append(cell)
            if not left_column:
                neighbors.append(cell - 1)
            if not right_column:
                neighbors.append(cell + 1)
        if not left_column:
            neighbors.append(current_cell - 1)
            if not bottom_most_row:
                neighbors.append(current_cell
                                 + self.__number_of_grid_cells_x + 1)
        if not right_column:
            neighbors.append(current_cell + 1)
            if not bottom_most_row:
                neighbors.append(current_cell
                                 + self.__number_of_grid_cells_x + 1)
        if not bottom_most_row:
            neighbors.append(current_cell + self.__number_of_grid_cells_x)
        return neighbors

    def __propagate_potential(self, original_cell, current_cell,
                              remaining_depth):
        neighbors = self.__get_neighbor_cells(current_cell)
        for neighbor in neighbors:
            cell_x = original_cell % self.__number_of_grid_cells_x
            cell_y = original_cell / self.__number_of_grid_cells_x
            distance_x = abs(cell_x - (neighbor
                                       % self.__number_of_grid_cells_x))
            distance_y = abs(cell_y - (neighbor
                                       / self.__number_of_grid_cells_x))
            neighbor_cell_distance = math.sqrt(distance_x ** 2
                                               + distance_y ** 2)
            current_potential = (
                (self.__potential_depth_in_cells - neighbor_cell_distance)
                / self.__potential_depth_in_cells * 100)

            if ((neighbor < len(self.__potential_field))
                    and (self.__potential_field[neighbor] < current_potential)
                    and (current_potential > 0)):
                self.__potential_field[neighbor] = current_potential
                self.__propagate_potential(original_cell, neighbor,
                                           current_potential - 1.0)

    def __register_potential(self, longitude, latitude):
        longitude_in_grid = math.fabs(longitude - self.__longitude_left)
        x_position = int(longitude_in_grid / (self.__longitude_distance
                                              / self.__number_of_grid_cells_x))
        latitude_in_grid = math.fabs(latitude - self.__latitude_top)
        y_position = int(latitude_in_grid / (self.__latitude_distance
                                             / self.__number_of_grid_cells_y))
        current_cell = y_position * self.__number_of_grid_cells_x + x_position
        if current_cell >= len(self.__potential_field):
            logger.warning('Potential field lng=%f, lat=%f outside of specified'
                           ' area and thus ignored', longitude, latitude)
        else:
            self.__potential_field[current_cell] = POTENTIAL_VALUE
            self.__propagate_potential(current_cell, current_cell,
                                       self.__potential_depth_in_cells)

    def init_layer_from_file(self, input_path, longitude_index,
                               latitude_index, delimiter):
        with open(input_path) as file:
            reader = csv.reader(file, delimiter=delimiter)
            next(reader)
            for row in reader:
                self.__register_potential(
                    longitude=float(row[longitude_index]),
                    latitude=float(row[latitude_index]))

    def write_layer_to_file(self, destination_path):
        locale.setlocale(locale.LC_NUMERIC, 'en_US.UTF-8')
        with open(destination_path, mode='w') as file:
            print("LatitudeTop=%f" % self.__latitude_top, file=file)
            print("LongitudeLeft=%f" % self.__longitude_left, file=file)
            print("LatitudeBottom=%f" % self.__latitude_bottom, file=file)
            print("LongitudeRight=%f" % self.__longitude_right, file=file)
            print("CellSizeInM=%f" % self.__grid_length_in_meter, file=file)
            writer = csv.writer(file, delimiter=';', quoting=csv.QUOTE_MINIMAL)

            for y in range(0, self.__number_of_grid_cells_y):
                writer.writerow([int(x) for x in self.__potential_field[
                    y * self.__number_of_grid_cells_x
                    : (y + 1) * self.__number_of_grid_cells_x]])


def generate(latitude_top, latitude_bottom, longitude_left, longitude_right,
             grid_length_in_meter, potential_depth_in_cells, latitude_column,
             longitude_column, input_file, output_file):
    field_generator = PotentialFieldGenerator(
        latitude_top=latitude_top, longitude_left=longitude_left,
        latitude_bottom=latitude_bottom, longitude_right=longitude_right,
        grid_length_in_meter=grid_length_in_meter,
        potential_depth_in_cells=potential_depth_in_cells)
    field_generator.init_layer_from_file(
        input_path=input_file[0], latitude_index=latitude_column,
        longitude_index=longitude_column, delimiter=';')
    field_generator.write_layer_to_file(output_file)
