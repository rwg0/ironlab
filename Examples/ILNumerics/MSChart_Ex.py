from ilab import *
# Plot blank chart
chart = chart()

# Create chart area and make 3D
chart.ChartAreas.Add(Charting.ChartArea())
chart.ChartAreas[0].Area3DStyle.Enable3D = True

# Add Doughnut-Chart Series
series = Charting.Series("Doughnut-Chart Example")
series.ChartType = Charting.SeriesChartType.Doughnut

# Add data
data = array([20.3, 19.3, 10.2, 15.8, 30.2])
label = ['France', 'Italy', 'Germany', 'UK', 'Netherlands']
for i in range(length(data)):
   series.Points.AddY(data.GetValue(i))
   series.Points[i].Label = str(label[i])  
chart.Series.Add(series)

# Add legend
legend = Charting.Legend()
chart.Legends.Add(legend)
legend.Docking = Charting.Docking.Bottom
chart.Legends[0].Enabled = True

# Define appearance
chart.Series[0]["PieDrawingStyle"] = "SoftEdge"
chart.Series[0]["PieLabelStyle"] = "Inside"

