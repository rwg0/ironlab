import ironplot as ip
import numpy as np
pi = np.pi
dphi, dtheta = pi/100.0, pi/100.0
[phi,theta] = np.mgrid[0:pi+dphi*1.5:dphi,0:2*pi+dtheta*1.5:dtheta]
m0 = 4.; m1 = 3; m2 = 4.; m3 = 3; m4 = 6.; m5 = 2; m6 = 6.; m7 = 4;
r = np.sin(m0*phi)**m1 + np.cos(m2*phi)**m3 + np.sin(m4*theta)**m5 + np.cos(m6*theta)**m7
x = r*np.sin(phi)*np.cos(theta)
y = r*np.cos(phi)
z = r*np.sin(phi)*np.sin(theta)
# Plot the shape
ip.plot3d(x, y, z)