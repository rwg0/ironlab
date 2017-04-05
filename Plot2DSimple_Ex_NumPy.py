from ironplot import *
import numpy as np
x = np.arange(0, 10, 0.1)
curve1 = plot(x, np.sin(x), 'r', Title="Line1")
hold(True)
curve2 = plot(x, np.cos(x), '-ob', Title="Line2")
hold(False)
