import ironplot as ip
from math import *
dphi, dtheta = pi/100.0, 2 * pi/200.0
phis = [i * dphi for i in range(101)]
thetas = [i * dtheta for i in range(201)]

m0 = 4.; m1 = 3; m2 = 4.; m3 = 3; m4 = 6.; m5 = 2; m6 = 6.; m7 = 4;
r = []; x = []; y = []; z = []
for (i, phi) in enumerate(phis):
	x.append([0.0 for theta in thetas]); y.append([0.0 for theta in thetas]); z.append([0.0 for theta in thetas])
	for (j, theta) in enumerate(thetas):
		r = sin(m0*phi)**m1 + cos(m2*phi)**m3 + sin(m4*theta)**m5 + cos(m6*theta)**m7 
		x[i][j] = r*sin(phi)*cos(theta) 
		y[i][j] = r*cos(phi)
		z[i][j] = r*sin(phi)*sin(theta)
	
# Plot the shape
ip.plot3d(x, y, z)