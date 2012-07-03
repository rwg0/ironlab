from ironplot_windows import *
import clr
import System
import System.Windows.Controls 
from System.Windows.Controls import *
from System.Windows import Thickness, Visibility
clr.AddReferenceToFile("IronPlot.dll")
from IronPlot import *
from IronPlot.Plotting3D import Plot3D

floatarray = System.Array[float]

numpyAvailable = True
ilnumericsAvailable = True
try:
    import numpy as np
except ImportError:
    numpyAvailable = False
try:
    import ilnumerics as il
except ImportError:
    ilnumericsAvailable = False
    
    
def plot(*args, **kwargs):
   """ Create a line plot (or overwite current plot if hold is set)
   Plot2DCurve plot(x, y): x and y are vectors of the same length
   Plot2DCurve plot(y): x vector will be created
   Also support PyLab-like line properties, e.g.:
   plot(x1, y1, '-or', x2, y2, '--sb')
   Line types are '-', '--', ':' , '-.'
   Marker types are 's', 'o', '^'
   Colours are 'r', 'g', 'b', 'y', 'c', 'm', 'k', 'w'
   Can also specify properties of the Curves to change, e.g.:
   plot(x1, y1, '-or', StrokeThickness = 2)
   """
   if PlotContext.CurrentWindowIndex == None:
      PlotContext.OpenNextWindow()
   if (PlotContext.CurrentPlot == None) or (PlotContext.HoldState == False):
      # New plot or overwite plot
      curves = Plotting.Plot2D(*args)
      plot = curves[0].Plot
      plot.Padding = Thickness(10)
      PlotContext.AddPlot(plot)
   else:
      # Add to current plot
      curves = Plotting.Plot2D(PlotContext.CurrentPlot, *args)
   # Apply kwargs
   for curve in curves:
      setprops(curve, **kwargs)
   if (curves.Count > 1):
      return curves
   elif (curves.Count == 1):
      return curves[0]


# def bar(*args, **kwargs):
   # """ Create a bar plot (or overwite current plot if hold is set).
   # """
   # if PlotContext.CurrentWindowIndex == None:
      # PlotContext.OpenNextWindow()
   # if (PlotContext.CurrentPlot == None) or (PlotContext.HoldState == False):
      # # New plot or overwite plot
      # plot = Plot2D()
      # PlotContext.AddPlot(plot)      
   # else:
      # # Add to current plot
      # plot = PlotContext.CurrentPlot
   # ILArrayType = ILArray[float]
   # if len(args) == 1 and isinstance(args[0], ILArrayType) and args[0].IsVector:
      # bars = plot.AddBars(args[0])
   # for bar in bars:
      # setprops(bar, **kwargs)
   # return plot    

 
def image(*args, **kwargs):
    """ Create a false-colour image plot of the specified 2D array (matrix) (or overwite current plot if hold is set).
    Plot2DImage image(image): image is a 2D array (matrix)
    Plot2DImage image(x, y, image): image is a 2D array (matrix); x and y are arrays. Max and min of x and y provide the ranges for the axes.
    """
    if PlotContext.CurrentWindowIndex == None:
        PlotContext.OpenNextWindow()
    if (PlotContext.CurrentPlot == None) or (PlotContext.HoldState == False):
        # New plot or overwite plot
        plot = Plot2D()
        PlotContext.AddPlot(plot)      
    else:
        # Add to current plot
        plot = PlotContext.CurrentPlot
    if len(args) == 1:
        dims = getsize(args[0])
        if len(dims) == 2:
            image = plot.AddFalseColourImage(args[0])
            plot.Margin = Thickness(10)
            setprops(image, **kwargs)
    elif len(args) == 3 and (type(args[0]) == type(args[1]) == type(args[2])):
        dims0 = getsize(args[0])
        dims1 = getsize(args[1])
        dims2 = getsize(args[2])
        if len(dims2) == 2:
            image = plot.AddFalseColourImage(args[0], args[1], args[2])
            setprops(image, **kwargs)
    return plot

    
def getsize(array):
    return GeneralArray.GetDimensions(array)
    

def plot3d(*args, **kwargs):
    """ Create a 3D surface plot of the specified 2D array (matrix).
    Plot3D image(surface): surface is a 2D array (matrix).
    Plot3D image(x, y, surface): surface is a 2D array (matrix); x and y are matrices of the same size that provide the x and y coordinates.
    x and y are expected in the format provided by mgrid, e.g., [x, y] = mgrid[0:127, 0:127]
    """
    if PlotContext.CurrentWindowIndex == None:
        PlotContext.OpenNextWindow()
    if (PlotContext.CurrentPlot == None) or (PlotContext.HoldState == False):
        # New plot or overwite plot
        plot = Plotting3D.Plot3D()
        PlotContext.AddPlot(plot)      
    else:
        # Add to current plot
        plot = PlotContext.CurrentPlot
    if len(args) == 1:
        plot.Viewport3D.Models.Add(Plotting3D.SurfaceModel3D(args[0]))
    elif len(args) == 3:
        plot.Viewport3D.Models.Add(Plotting3D.SurfaceModel3D(args[0], args[1], args[2]))
    return plot

  
def xlabel(arg):
   """ Set X-Axis label of current plot.
   """
   if isinstance(arg, str) and PlotContext.CurrentPlot != None:
      PlotContext.CurrentPlot.Axes.XAxes.Bottom.AxisLabel.Text = arg

    
def ylabel(arg):
   """ Set Y-Axis label of current plot.
   """  
   if isinstance(arg, str) and PlotContext.CurrentPlot != None:
      PlotContext.CurrentPlot.Axes.YAxes.Left.AxisLabel.Text = arg
      #PlotContext.CurrentPlot.LeftLabel.Text = arg
    
       
def title(arg):
   """ Set Title of current plot.
   """
   if isinstance(arg, str) and PlotContext.CurrentPlot != None:
      PlotContext.CurrentPlot.Title.Visibility = Visibility.Visible 
      PlotContext.CurrentPlot.Title.Text = arg

      
def equalaxes(*args):
   """ Set axes of current plot to be equal (or get current status if no argument) 
   """
   if (len(args) == 1 and isinstance(args[0], bool)): 
      PlotContext.CurrentPlot.EqualAxes = args[0]
   else:
      return PlotContext.CurrentPlot.EqualAxes


def window(*args):
   """ Set current window (or create window0 by index or get current windows index (if no argument)
   """
   if (len(args) == 1 and isinstance(args[0], int)): 
      PlotContext.CurrentWindowIndex = args[0]
      return PlotContext.CurrentWindow
   else:
      return PlotContext.CurrentWindowIndex


def currentplot(**kwargs):
   """ Get the current plot (alternative to PlotContext.CurrentPlot)
   Can also be used to set properties of current plot.
   """
   setprops(PlotContext.CurrentPlot, **kwargs)
   return PlotContext.CurrentPlot


def currplot(**kwargs):
   """ Get the current plot (alternative to PlotContext.CurrentPlot) 
   Can also be used to set properties of current plot.
   """
   return currentplot(kwargs)


def tab(*args):
   """ Get (with no argument) or set the current tab index.
   """
   if (len(args) == 1 and isinstance(args[0], int)): 
      PlotContext.CurrentTabItemIndex = args[0]
   else:
      return PlotContext.CurrentTabItemIndex
 

def hold(*args):
   """ Set to true to hold the plot (plot on top of current plot); false to create new plot.
   (alternative to PlotContext.HoldState)
   """
   if (len(args) == 1 and isinstance(args[0], bool)): 
      PlotContext.HoldState = args[0]
   else:
      return PlotContext.HoldState
 
     
def subplot(*args):
   """ Create sub-plot grids and select position in grid 
   subplot(self, int subplot): select sub-plot 
   subplot(self, int rows, int columns): create new grid with given dimensions 
   subplot(self, int rows, int columns, int subplot): create new grid with given dimensions and
   select position in grid
   """
   if (len(args) == 1 and isinstance(args[0], int)): 
      PlotContext.CurrentPlotIndex = args[0]
   elif (len(args) == 2 and isinstance(args[0], int) and isinstance(args[1], int)): 
      PlotContext.AddSubPlot(args[0], args[1])
   elif (len(args) == 3 and isinstance(args[0], int) and isinstance(args[1], int) and isinstance(args[2], int)): 
      PlotContext.AddSubPlot(args[0], args[1])
      PlotContext.CurrentPlotIndex = args[2]


def setprops(object, **kwargs):
   """ Set properties of specified object
   e.g. setprops(currentplot(), Margin = Thickness(20,20,20,20))
   """
   for key in kwargs:  
      property = object.GetType().GetProperty(key)
      if property != None:
         try:
            property.SetValue(object, kwargs[key], None)
         except Exception:
            pass
         

   

      

    
   
    