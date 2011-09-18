from ilab import *
x = counter(100)
y = 0.4 * x**2 + 2 * x + 50 * rand(100)
cfit1 = polyfit(x, y, 1)
yfit1 = cfit1[0] * x + cfit1[1]
cfit2 = polyfit(x, y, 2)
yfit2 = cfit2[0] * x**2 + cfit2[1] * x + cfit2[2]
hold(False)
plot(x, y)
hold(True)
plot(x, yfit1, 'r')
plot(x, yfit2, 'b')

