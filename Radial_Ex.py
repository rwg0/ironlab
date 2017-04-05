from ironplot import *
import numpy as np
angle = np.arange(0, 365, 5)
val = (1.0 + np.sin(angle * 4 / 180.0 * np.pi)) * 10.0
series = radial(angle, val, Name="Test1")

# Additional customizations to try:
series["PolarDrawingStyle"] = "Marker"
from System.Drawing import Color
from System.Windows.Forms.DataVisualization.Charting import MarkerStyle, Legend
series.MarkerBorderColor = Color.Blue
series.MarkerColor = Color.Transparent
series.MarkerStyle = MarkerStyle.Square
hold(True)
series2 = radial(angle, val * 2, Color=Color.Red, Name="Test2")
hold(False)
legend = Legend(Name = "Default")
currentplot().Chart.Legends.Add(legend)