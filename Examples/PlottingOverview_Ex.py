from ironplot import *
from math import *

x = [i * 0.1 for i in range(100)]

# Prepare 2x2 plot area
subplot(2,2)

# Solid line, circular marker, red
plot(x, [sin(i) for i in x] , '-or')

subplot(1)
# Dashed blue line
plot(x, [cos(i) for i in x], '--b')
ylabel('cos(x)')

subplot(2)
# Dotted line, circular marker, red
y = [sin(i) for i in x]
for i in range(len(y)):
	if y[i] < 0: y[i] = 0
	
plot(x, y, Stroke = Brushes.Pink)

subplot(3)
# Create and plot simple image
x = [i * 0.005 for i in range(140)]
y = [i * 0.005 for i in range(200)]
z = [[exp(-i * -(j**2)) for i in x] for j in y]
image(z)