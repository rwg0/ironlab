from ironplot import *
import numpy as np
plot(np.random.randn(10000))
# WPF can be quite slow at plotting a large number of overlapping lines. Using Direct2D
# we can elect to multi-sample antialias if this is available rather than apply antialiasing per line,
# which is faster.
# Note Direct2D is not available on XP.
plot1 = currentplot()
# Compare speed before and after using this command:
plot1.UseDirect2D = True