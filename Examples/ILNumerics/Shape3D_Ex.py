import ilab as il
#from math import *
pi = 3.142
# Create the data.
dphi, dtheta = pi/250.0, pi/250.0
[phi,theta] = il.mgrid[0:pi+dphi*1.5:dphi,0:2*pi+dtheta*1.5:dtheta]
m0 = 4.; m1 = 3; m2 = 4.; m3 = 3; m4 = 6.; m5 = 2; m6 = 6.; m7 = 4;
r = il.sin(m0*phi)**m1 + il.cos(m2*phi)**m3 + il.sin(m4*theta)**m5 + il.cos(m6*theta)**m7
x = r*il.sin(phi)*il.cos(theta)
y = r*il.cos(phi)
z = r*il.sin(phi)*il.sin(theta)
# Plot the shape
il.plot3d(x, y, z)

