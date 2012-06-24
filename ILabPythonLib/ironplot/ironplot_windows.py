import clr

clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("IronPython")
clr.AddReferenceByPartialName("Microsoft.Scripting")

from math import *
import System
import IronPython
from System.Windows import *
from System.Windows.Media import *
from System.Windows.Media.Animation import *
from System.Windows.Controls import *
from System.Windows.Shapes import *
from System.Threading import *
from System.Windows.Threading import *
from IronPython.Runtime import PythonContext
from IronPython.Compiler import CallTarget0

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

are = AutoResetEvent(False)

def on_startup(*args):
    global dispatcher
    dispatcher = Dispatcher.FromThread(t)
    are.Set()

def start():
    try:
        global app
        app = Application()
        app.Startup += on_startup
	app.ShutdownMode = ShutdownMode.OnExplicitShutdown
        app.Run()
    finally:
        clr.SetCommandDispatcher(None)

# If dispatcher is set, then assume that UI issues are already taken care of.	
currentDispatcher = clr.GetCurrentRuntime().GetLanguage(PythonContext).GetCommandDispatcher()
if currentDispatcher is None:
    t = Thread(ThreadStart(start))
    t.IsBackground = True
    t.ApartmentState = ApartmentState.STA
    t.Start()
    are.WaitOne()

def DispatchConsoleCommand(consoleCommand):
    if consoleCommand:
        dispatcher.Invoke(DispatcherPriority.Normal, consoleCommand)
	
def dispatch(function):
    dispatcher.Invoke(DispatcherPriority.Normal, CallTarget0(function))

if currentDispatcher is None:
    clr.SetCommandDispatcher(DispatchConsoleCommand)
    


