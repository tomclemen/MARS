import argparse
from generator import generator


def main():
    parser = argparse.ArgumentParser(
        usage='Generates a potential field grid for a specified area from a '
              'list of potentials (e.g. waterpoints)')
    parser.add_argument('--latitude-top', dest='latitude_top',
                        action='store', required=True, type=float)
    parser.add_argument('--latitude-bottom', dest='latitude_bottom',
                        action='store', required=True, type=float)
    parser.add_argument('--longitude-left', dest='longitude_left',
                        action='store', required=True, type=float)
    parser.add_argument('--longitude-right', dest='longitude_right',
                        action='store', required=True, type=float)
    parser.add_argument('--grid-length-in-meter', dest='grid_length_in_meter',
                        default=1000, action='store', type=int)
    parser.add_argument('--potential-depth-in-cells', action='store',
                        dest='potential_depth_in_cells', default=7, type=int)
    parser.add_argument('--latitude-column', dest='latitude_column', default=1,
                        action='store')
    parser.add_argument('--longitude-column', dest='longitude_column',
                        default=0, action='store')
    parser.add_argument('input_file', nargs=1, action='store')
    parser.add_argument('-o', '--output', dest='output_file', action='store',
                        required=True)
    return generator.generate(**parser.parse_args().__dict__)


if __name__ == '__main__':
    main()