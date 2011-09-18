import ilab as il
import math

def spectrum(tseries):
   pspec = il.fft(tseries)/il.length(tseries)
   pspec *= il.conj(pspec)
   return il.real(pspec)
   
# Sine waves with wavenumbers of 10 and 55: 
delta = 1. / 512.
time = il.counter(512) * delta
tseries = il.sin(time * 2 * math.pi * 10.) + 0.5 * il.cos(time * 2 * math.pi * 55.)

il.subplot(2, 1)
il.plot(time, tseries)
il.xlabel('Time [s]')

# Plot single-sided spectrum.
# No windowing is applied in this example!
interval = (1. / delta) / 512.
il.subplot(1)
il.plot(il.vector(0, interval, 127 * interval), 2 * spectrum(tseries)[0:127])
il.xlabel('Frequency [Hz]')
