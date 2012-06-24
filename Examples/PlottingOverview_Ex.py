from ironplot import *
from numpy import *

x = arange(0, 10, 0.1)

# Prepare 2x2 plot area
subplot(2,2)

# Solid line, circular marker, red
plot(x, sin(x), '-or')

subplot(1)
# Dashed blue line
plot(x, cos(x), '--b')
ylabel('cos(x)')

subplot(2)
# Dotted line, circular marker, red
y = sin(x)
y[y < 0] = 0.
plot(x, y, Stroke = Brushes.Pink)

subplot(3)
# Create and plot simple image
[x,y] = mgrid[0:0.7:0.005, 0:1:0.005]
image(exp(-x * -(y**2)))