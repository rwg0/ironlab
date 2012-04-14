from ironplot_windows import *
import clr
import System
import System.Windows.Controls 
from System.Windows.Controls import *
clr.AddReferenceByPartialName("System.Windows.Forms.DataVisualization")
clr.AddReferenceByPartialName("System.Drawing")
clr.AddReferenceToFile("IronPlot.dll")
import System.Windows.Forms.DataVisualization as dv
import System.Drawing as dr
import System
import numpy as np
from System.Windows import Thickness, Visibility
from IronPlot import *
from IronPlot.Plotting3D import Plot3D

floatarray = System.Array[float]

numpyAvailable = True
try:
    import numpy as np
except ImportError:
    numpyAvailable = False
    
    
def radial(theta, r, **kwargs):
   """ Create a radial plot (or overwite current plot if hold is set)
   """
   if len(theta) != len(r):
      raise ValueError('Arrays must be of the same length.')
   if PlotContext.CurrentWindowIndex == None:
      PlotContext.OpenNextWindow()
   if (PlotContext.CurrentPlot == None) or (PlotContext.HoldState == False):
      # New plot or overwite plot
      host = MSChartHost()
      chart = host.Chart
      chartArea = dv.Charting.ChartArea(Name = "Default")
      chart.ChartAreas.Add(chartArea)
      PlotContext.AddPlot(host)
   else:
      # Add to current plot
	  chart = PlotContext.CurrentPlot.Chart
   seriesName = "Series" + str(chart.Series.Count)
   series = dv.Charting.Series(ChartType = dv.Charting.SeriesChartType.Polar, Name = seriesName)
   chart.Series.Add(series)
   for a, b in zip(theta, r):
      chart.Series[seriesName].Points.AddXY(float(a), float(b))
   # Apply kwargs
   setprops(series, **kwargs)
   return series