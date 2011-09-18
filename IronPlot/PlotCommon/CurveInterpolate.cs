// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot
{
    /// <summary>Boundary types for Curve interpolation</summary>
    /// <remarks><para></para>
    /// </remarks>
    public enum BoundaryType { Parabolic, FirstDerivativeSpecified, SecondDerivativeSpecified }

    public partial class Curve
    {
        /// <summary>Sort a curve ascending in preparation for interpolation</summary>
        /// <remarks><para></para>
        /// </remarks>
        public void Sort()
        {
            //double[] indices;
            //double[] xSorted = ILMath.sort(x, out indices, 0, false);
            Array.Sort(x, y);
            //double[] ySorted = y[indices];
            //x = xSorted;
            //y = ySorted;
        }
        
        // Calculation coefficients once, interpolate multiple times
        // Different sets for different interpolation types to allow simultaneous use
        protected double[] linearSplineCoefficients = null;
        protected double[] cubicSplineCoefficients = null;
        protected double[] monotoneCubicSplineCoefficients = null;
        protected double[] hermiteSplineCoefficients = null;
        
        /// <summary>Calculates interpolated values using Linear Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated Y values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesLinear(double[] xi)
        {
            if (linearSplineCoefficients == null) UpdateLinearSplineCoefficients();
            
            double[] interpolatedValues = new double[xi.Length];
            for (int i = 0; i < xi.Length; ++i)
            {
                double xit = xi[i];
                int p = 0; int r = n - 1; int q = 0;       
                while (p != r - 1)
                {
                    q = (p + r) / 2;
                    if (x[q] >= xit) r = q;
                    else p = q;
                }
                xit = xit - x[p];
                q = p * 2;
                interpolatedValues[i] = linearSplineCoefficients[q] + xit * linearSplineCoefficients[q + 1];
            }
            return interpolatedValues;
        }

        /// <summary>Calculates interpolated values using default cubic interpolation which is Monotone Piecewise Cubic Hermite Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated Y values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesCubic(double[] xv)
        {
            return GetValuesMonotoneCubicSpline(xv);
        }

        /// <summary>Calculates interpolated values using Cubic Spline Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated Y values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesSpline(double[] xv)
        {
            return GetValuesCubicSpline(xv);
        }

        /// <summary>Calculates interpolated values using 'natural' Cubic Spline Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesCubicSpline(double[] xv)
        {
            return GetValuesCubicSpline(xv, BoundaryType.SecondDerivativeSpecified, 0.0,
            BoundaryType.SecondDerivativeSpecified, 0.0);
        }

        /// <summary>Calculates interpolated values using Cubic Spline Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <param name="leftBoundaryType">BoundaryType enumeration (Parabolic, FirstDerivative Specified, SecondDerivative Specified)</param>
        /// <param name="leftBoundaryTypeParameter">Parameter required (ignored if Parabolic)</param>
        /// <param name="rightBoundaryType">BoundaryType enumeration (Parabolic, FirstDerivative Specified, SecondDerivative Specified)</param>
        /// <param name="rightBoundaryTypeParameter">Parameter required (ignored if Parabolic)</param>
        /// <returns>Interpolated values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesCubicSpline(double[] xi, BoundaryType leftBoundaryType, double leftBoundaryTypeParameter,
            BoundaryType rightBoundaryType, double rightBoundaryTypeParameter)
        {
            if (cubicSplineCoefficients == null) UpdateCubicSplineCoefficients(leftBoundaryType, leftBoundaryTypeParameter, rightBoundaryType, rightBoundaryTypeParameter);
            
            double[] interpolatedValues = new double[xi.Length];
            for (int i = 0; i < xi.Length; ++i)
            {
                double xit = xi[i];
                int p = 0; int r = n - 1; int q = 0;
                while (p != r - 1)
                {
                    q = (p + r) / 2;
                    if (x[q] >= xit) r = q;
                    else p = q;
                }
                xit = xit - x[p];
                q = p * 4;
                interpolatedValues[i] = cubicSplineCoefficients[q] + xit * (cubicSplineCoefficients[q + 1]
                    + xit * (cubicSplineCoefficients[q + 2] + xit * cubicSplineCoefficients[q + 3]));
            }
            return interpolatedValues;
        }

        /// <summary>Calculates interpolated values using Monotone Piecewise Cubic Hermite Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesMonotoneCubicSpline(double[] xi)
        {
            if (monotoneCubicSplineCoefficients == null)  UpdateMonotoneCubicSplineCoefficients();

            double[] interpolatedValues = new double[xi.Length];
            for (int i = 0; i < xi.Length; ++i)
            {
                double xit = xi[i];
                int p = 0; int r = n - 1; int q = 0;
                while (p != r - 1)
                {
                    q = (p + r) / 2;
                    if (x[q] >= xit) r = q;
                    else p = q;
                }
                xit = xit - x[p];
                q = p * 4;
                interpolatedValues[i] = monotoneCubicSplineCoefficients[q] + xit * (monotoneCubicSplineCoefficients[q + 1]
                    + xit * (monotoneCubicSplineCoefficients[q + 2] + xit * monotoneCubicSplineCoefficients[q + 3]));
            }
            return interpolatedValues;
        }

        /// <summary>Update or create coefficients for linear spline</summary>
        public void UpdateLinearSplineCoefficients()
        {
            linearSplineCoefficients = new double[2 * n];
            for (int i = 0; i <= n - 2; i++)
            {
                linearSplineCoefficients[2 * i + 0] = y[i];
                linearSplineCoefficients[2 * i + 1] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
            }
        }

        /// <summary>Update or create coefficients for cubic spline</summary>
        public void UpdateCubicSplineCoefficients(BoundaryType leftBoundaryType, double leftBoundaryTypeParameter,
            BoundaryType rightBoundaryType, double rightBoundaryTypeParameter)
        {
            // TODO Raise error if < 2 points
            // Sort if points are unsorted?

            double[] a1 = new double[n];
            double[] a2 = new double[n];
            double[] a3 = new double[n];
            double[] b = new double[n];
            double[] deriv = new double[n];

            // If 2 points, apply parabolic end conditions
            if (n == 2)
            {
                leftBoundaryType = BoundaryType.Parabolic;
                rightBoundaryType = BoundaryType.Parabolic;
            }

            #region LeftBoundary
            if (leftBoundaryType == BoundaryType.Parabolic)
            {
                a1[0] = 0;
                a2[0] = 1;
                a3[0] = 1;
                b[0] = 2 * (y[1] - y[0]) / (x[1] - x[0]);
            }
            if (leftBoundaryType == BoundaryType.FirstDerivativeSpecified)
            {
                a1[0] = 0;
                a2[0] = 1;
                a3[0] = 0;
                b[0] = leftBoundaryTypeParameter;
            }
            if (leftBoundaryType == BoundaryType.SecondDerivativeSpecified)
            {
                a1[0] = 0;
                a2[0] = 2;
                a3[0] = 1;
                b[0] = 3 * (y[1] - y[0]) / (x[1] - x[0]) - 0.5 * leftBoundaryTypeParameter * (x[1] - x[0]);
            }
            #endregion

            for (int i = 1; i <= n - 2; i++)
            {
                a1[i] = x[i + 1] - x[i];
                a2[i] = 2 * (x[i + 1] - x[i - 1]);
                a3[i] = x[i] - x[i - 1];
                b[i] = 3 * ((y[i] - y[i - 1]) / a3[i]) * a1[i]
                    + 3 * ((y[i + 1] - y[i]) / a1[i]) * a3[i];
            }

            #region RightBoundary
            if (rightBoundaryType == BoundaryType.Parabolic)
            {
                a1[n - 1] = 1;
                a2[n - 1] = 1;
                a3[n - 1] = 0;
                b[n - 1] = 2 * (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]);
            }
            if (rightBoundaryType == BoundaryType.FirstDerivativeSpecified)
            {
                a1[n - 1] = 0;
                a2[n - 1] = 1;
                a3[n - 1] = 0;
                b[n - 1] = rightBoundaryTypeParameter;
            }
            if (rightBoundaryType == BoundaryType.SecondDerivativeSpecified)
            {
                a1[n - 1] = 1;
                a2[n - 1] = 2;
                a3[n - 1] = 0;
                b[n - 1] = 3 * (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]) 
                    + 0.5 * rightBoundaryTypeParameter * (x[n - 1] - x[n - 2]);
            }
            #endregion

            double temp = 0;
            
            a1[0] = 0;
            a3[n - 1] = 0;
            for (int i = 1; i <= n - 1; i++)
            {
                temp = a1[i] / a2[i - 1];
                a2[i] = a2[i] - temp * a3[i - 1];
                b[i] = b[i] - temp * b[i - 1];
            }
            deriv[n - 1] = b[n - 1] / a2[n - 1];
            for (int i = n - 2; i >= 0; i--)
            {
                deriv[i] = (b[i] - a3[i] * deriv[i + 1]) / a2[i];
            }

            cubicSplineCoefficients = GetHermiteSplineCoefficients(deriv);
        }

        protected double[] GetHermiteSplineCoefficients(double[] deriv)
        {
            double delta = 0;
            double delta2 = 0;
            double delta3 = 0;
            double[] coeffs = new double[(n - 1) * 4];
            for (int i = 0; i <= n - 2; i++)
            {
                delta = x[i + 1] - x[i];
                delta2 = delta * delta;
                delta3 = delta * delta2;
                coeffs[4 * i + 0] = y[i];
                coeffs[4 * i + 1] = deriv[i];
                coeffs[4 * i + 2] = (3 * (y[i + 1] - y[i]) - 2 * deriv[i] * delta - deriv[i + 1] * delta) / delta2;
                coeffs[4 * i + 3] = (2 * (y[i] - y[i + 1]) + deriv[i] * delta + deriv[i + 1] * delta) / delta3;
            }
            return coeffs;
        }


        public void UpdateMonotoneCubicSplineCoefficients()
        {
            double[] a1 = new double[n]; // secant
            double[] a2 = new double[n]; // derivative
            a1[0] = (y[1] - y[0]) / (x[1] - x[0]);
            a2[0] = a1[0];
            for (int i = 1; i < n - 3; i++)
            {
                a1[i] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
                a2[i] = (a1[i - 1] + a1[i]) / 2.0;
            }
            a1[n - 2] = (y[n-1] - y[n-2]) / (x[n-1] - x[n-2]);
            a2[n - 2] = (a1[n - 3] + a1[n - 2]) / 2.0;
            a2[n - 1] = a1[n - 2];
            double alpha, beta, dist, tau;
            for (int i = 0; i < n - 2; i++)
            {
                alpha = a2[i] / a1[i];
                beta = a2[i + 1] / a1[i];
                dist = alpha * alpha + beta * beta;
                if (dist > 9.0)
                {
                    tau = 3.0 / Math.Sqrt(dist);
                    a2[i] = tau * alpha * a1[i];
                    a2[i + 1] = tau * beta * a1[i];
                }
            }
            monotoneCubicSplineCoefficients = GetHermiteSplineCoefficients(a2);
        }

        /// <summary>Calculates interpolated values using Numerical Recipes spline routine: for cross test only</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated Y values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public unsafe double[] GetValuesCubicSpline2(double[] xi)
        {
            int n = x.Length;
            double[] y2 = new double[n + 1];
            double[] xt = new double[n + 1];
            double[] yt = new double[n + 1];
            for (int i = 0; i < n; ++i)
            {
                xt[i + 1] = x[i];
                yt[i + 1] = y[i];
            }
            nr_spline(xt, yt, n, 2e31, 2e31, ref y2);
            double[] yout = new double[xi.Length];
            fixed (double* youtp0 = &yout[0])
            {
                double* youtp = youtp0;
                foreach (double xit in xi)
                {
                    splint(xt, yt, y2, n, xit, youtp);
                    youtp++;
                }
            }
            return yout;
        }

        private unsafe void nr_spline(double[] x, double[] y, int n, double yp1, double ypn, ref double[] y2)
        {
            int i,k;
            double p, qn, sig, un;
            double[] u = new double[n];
            //u=vector(1,n-1);
            if (yp1 > 0.99e30) 
                y2[1]=u[1]=0.0;
            else {
                y2[1] = -0.5;
                u[1]=(3.0/(x[2]-x[1]))*((y[2]-y[1])/(x[2]-x[1])-yp1);
            }
            for (i=2;i<=n-1;i++) {
                sig=(x[i]-x[i-1])/(x[i+1]-x[i-1]);
                p=sig*y2[i-1]+2.0;
                y2[i]=(sig-1.0)/p;
                u[i]=(y[i+1]-y[i])/(x[i+1]-x[i]) - (y[i]-y[i-1])/(x[i]-x[i-1]);
                u[i]=(6.0*u[i]/(x[i+1]-x[i-1])-sig*u[i-1])/p;
            }
            if (ypn > 0.99e30)
                qn=un=0.0; 
            else {
                qn=0.5;
                un=(3.0/(x[n]-x[n-1]))*(ypn-(y[n]-y[n-1])/(x[n]-x[n-1]));
            }
            y2[n]=(un-qn*u[n-1])/(qn*y2[n-1]+1.0);
            for (k=n-1;k>=1;k--) 
                y2[k]=y2[k]*y2[k+1]+u[k]; 
            //free_vector(u,1,n-1);
        }

        private unsafe void splint(double[] xa, double[] ya, double[] y2a, int n, double x, double* y)
        {
            int klo,khi,k;
            double h, b, a;
            klo=1; 
            khi=n;
            while (khi-klo > 1) {
                k=(khi+klo) >> 1;
                if (xa[k] > x) khi=k;
                    else klo=k;
            } 
            h=xa[khi]-xa[klo];
            a=(xa[khi]-x)/h; 
            b=(x-xa[klo])/h; 
            *y = a*ya[klo] + b*ya[khi] + ((a*a*a-a)*y2a[klo] + (b*b*b-b)*y2a[khi]) * (h*h)/6.0;
        }
    }
}
