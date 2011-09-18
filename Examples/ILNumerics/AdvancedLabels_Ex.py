# All plots use WPF classes
# XLabel is a TextBlock and more complex labels are therefore possible by adding suitable inlines
# Partial LaTeX-style text support (i.e. ^{-1}) can help simplify superscript / subscript syntax
# But until this is added:   

from ilab import *
plot(counter(10))
pl1 = currentplot()

# Add a complex label
import System.Windows.BaselineAlignment as BaselineAlignment
import System.Windows.Documents.Run as Run
import System.Windows.Visibility as Visibility
exponent = Run('-1')
exponent.BaselineAlignment = BaselineAlignment.Superscript
exponent.FontSize = 10
pl1.XLabel.Text = ''
pl1.XLabel.Visibility = Visibility.Visible
pl1.XLabel.Inlines.Add(Run(u'Wavenumber, \u03BD [m'))
pl1.XLabel.Inlines.Add(exponent)
pl1.XLabel.Inlines.Add(']')

# The easy way:
ylabel('Y Label')

