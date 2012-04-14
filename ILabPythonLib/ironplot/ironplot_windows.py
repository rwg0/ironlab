import clr

clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("IronPython")
clr.AddReferenceByPartialName("Microsoft.Scripting")

from math import *
import System
import IronPython
#from System.Windows import *
#from System.Windows.Media import *
#from System.Windows.Media.Animation import *
#from System.Windows.Controls import *
#from System.Windows.Shapes import *
#from System.Threading import *
#from System.Windows.Threading import *

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



