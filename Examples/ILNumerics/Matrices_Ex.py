from ilnumerics import *
# Find (complex) eigenvalues:
print eig(array([[1., -1.], [1., 1.]]))

# Matrix multiplication ('*' operator does element-wise multiplication)
# Calls MKL library if available, so should be fast!
print multiply(counter(1000, 1500), counter(1500, 1000))