from ironplot import *
from numpy import *
[x, y] = mgrid[0:15:0.2, 0:10:0.2]
window(5)
tab(1)
plot3d(x, y, y**2 * sin(x))
p3d = currentplot()
tab(2)
image(y**2 * sin(x))

# customize the plot:
axes = p3d.Viewport3D.Axes
axes.XAxes.AxisLabels.Text = "New X label"
axes.YAxes.AxisLabels.Text = "New Y label"
axes.ZAxes.AxisLabels.Text = "New Z label"
axes.XAxes.TickLength = 0.05 # as fraction of axis length
#axes.LineThickness = 1