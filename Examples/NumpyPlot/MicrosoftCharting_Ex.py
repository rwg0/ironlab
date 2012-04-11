# Example showing how WinForms controls can be incorporated, in this case
# using Microsoft Chart Controls to make a polar plot
import clr
clr.AddReferenceByPartialName("System.Windows.Forms.DataVisualization")
clr.AddReferenceByPartialName("WindowsFormsIntegration")
clr.AddReferenceByPartialName("System.Drawing")
import System.Windows.Forms.DataVisualization as dv
import System.Windows.Forms.Integration as int
import System.Drawing as dr
import IronPlot as ip
import System
import numpy as np

chart = dv.Charting.Chart()
host = int.WindowsFormsHost()
host.Child = chart
ip.PlotContext.OpenNextWindow()
ip.PlotContext.AddPlot(host)

chart.BackColor = System.Drawing.Color.White
chartArea = dv.Charting.ChartArea()
chartArea.Name = "Default"
chart.ChartAreas.Add(chartArea);

series = dv.Charting.Series()
series.Name = "Series1"
series.ChartType = dv.Charting.SeriesChartType.Polar;
chart.Series.Add(series)
series.ChartArea = "Default"
series.MarkerBorderColor = dr.Color.Blue
series.MarkerColor = dr.Color.Blue
series.MarkerSize = 7
series.MarkerStyle = dv.Charting.MarkerStyle.Diamond

chartArea.BackColor = System.Drawing.Color.White
legend = dv.Charting.Legend()
legend.Name = "Default";
chart.Legends.Add(legend)

for angle in np.arange(0, 360, 10):
    val = (1.0 + np.sin(angle / 180.0 * np.pi)) * 10.0;
    chart.Series["Series1"].Points.AddXY(float(angle), float(val))

chart.Series["Series1"]["PolarDrawingStyle"] = "Marker"

#chart.ChartAreas["Default"].Area3DStyle.Enable3D = True