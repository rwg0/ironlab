from ilab import *
[x, y] = mgrid[0:15:0.2, 0:10:0.2]
window(1)
tab(1)
tab(2)
plot3d(x, y, x**2 * sin(y))
