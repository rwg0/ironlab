from math import *
from ironplot import *
x = [i * 0.1 for i in range(100)]
curve1 = plot(x, [sin(xi) for xi in x], 'r', Title="Line1")
hold(True)
curve2 = plot(x, [cos(xi) for xi in x], '-ob', Title="Line2")
hold(False)
