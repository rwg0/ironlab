#import distutils.sysconfig
#newPath = distutils.sysconfig.get_python_lib() 
# Add a bin directory for dlls to the path:
newPath = __path__[0] + "\\bin"
import sys
sys.path.append(newPath)

import ironplot_windows
import ironplot_functions
import ironplot_mscharts

from ironplot_windows import dispatch
from ironplot_functions import plot, image, plot3d, xlabel, ylabel, title, equalaxes, window \
    , currentplot, currplot, tab, hold, subplot \
    , MarkersType, Position \
    , Plot2D, Plot2DCurve, FalseColourImage, QuickStrokeDash, Plot3D, XAxis, YAxis, XAxisPosition, YAxisPosition \
    , MSChartHost, FormatOverrides
from ironplot_mscharts import radial

import clr
from System.Windows import Thickness, Visibility, FontStyles, FontWeights
from System.Windows.Controls import Orientation
from System.Windows.Media import Brushes


