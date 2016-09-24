import ironplot as ip
import numpy as np

# plot Rastrigrin function
x1 = np.arange(-5.12, 5.12, 0.1024)
x2 = np.arange(-5.12, 5.12, 0.1024)
X1, X2 = np.meshgrid(x1, x1)
Z = (X1**2 - 10 * np.cos(2 * np.pi * X1)) + (X2**2 - 10 * np.cos(2 * np.pi * X2)) + 20
ip.plot3d(x1, x1, Z)