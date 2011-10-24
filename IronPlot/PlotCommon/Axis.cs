// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Navigation;
using System.Windows.Data;
using System.Linq;
using System.Text;
using System.Windows.Documents;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{
    public enum AxisType { Number, Date }

    public struct Range
    {
        public double Min;
        public double Max;

        public Range(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double Length
        {
            get { return Max - Min; }
        }

        public Range Union(Range otherRange)
        {
            return new Range(Math.Min(this.Min, otherRange.Min), Math.Max(this.Max, otherRange.Max));
        }
    }

    /// <summary>
    /// An Axis class is a Canvas on which the axis and annotations
    /// are presented (one Canvas per Axis). The Canvas typically spans the entire plot region. 
    /// This general approach is suitable for axes of 3D and 2D plots.
    /// </summary>
    public abstract class Axis : ContentControl
    {
        protected Canvas canvas;
        
        internal double[] Ticks;
        internal double[] Coefficient;
        internal int[] Exponent;
        internal int[] RequiredDPs;
        
        protected string[] labels;
        protected double[] cachedTicksOverride;
        protected double[] cachedCoefficientOverride;
        protected int[] cachedExponentOverride;
        protected int[] cachedRequiredDPsOverride;

        protected double[] ticksOverride;
        protected string[] tickLabelsOverride;

        public static DependencyProperty AxisTypeProperty =
            DependencyProperty.Register("AxisTypeProperty",
            typeof(AxisType), typeof(Axis), new PropertyMetadata(AxisType.Number));

        public static DependencyProperty NumberOfTicksProperty =
            DependencyProperty.Register("NumberOfTicksProperty",
            typeof(int), typeof(Axis), new PropertyMetadata(10, OnNumberOfTicksChanged));

        public static DependencyProperty TickLengthProperty =
            DependencyProperty.Register("TickLengthProperty",
            typeof(double), typeof(Axis), new PropertyMetadata(5.0));

        public static DependencyProperty LabelsVisibleProperty =
            DependencyProperty.Register("LabelsVisibleProperty",
            typeof(bool), typeof(Axis), new PropertyMetadata(true));

        public static DependencyProperty TicksVisibleProperty =
            DependencyProperty.Register("TicksVisibleProperty",
            typeof(bool), typeof(Axis), new PropertyMetadata(true));

        // TODO
        //public static DependencyProperty TicksOverrideProperty =
        //    DependencyProperty.Register("TicksOverrideProperty",
        //    typeof(IEnumerable<double>), typeof(Axis), new PropertyMetadata());

        public double[] TicksOverride
        {
            set
            {
                ticksOverride = value;
            }
            get { return ticksOverride; }
        }

        public string[] TickLabels
        {
            set
            {
                tickLabelsOverride = value;
            }
            get { return tickLabelsOverride; }
        }

        public int NumberOfTicks
        {
            set { SetValue(NumberOfTicksProperty, value); }
            get { return (int)GetValue(NumberOfTicksProperty); }
        }

        public double TickLength
        {
            set { SetValue(TickLengthProperty, value); }
            get { return (double)GetValue(TickLengthProperty); }
        }

        public bool LabelsVisible
        {
            set { SetValue(LabelsVisibleProperty, value); }
            get { return (bool)GetValue(LabelsVisibleProperty); }
        }

        public bool TicksVisible
        {
            set { SetValue(TicksVisibleProperty, value); }
            get { return (bool)GetValue(TicksVisibleProperty); }
        }

        public Axis()
        {
            canvas = new Canvas();
            this.Content = canvas;
        }

        protected static void OnNumberOfTicksChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis)obj).DeriveTicks();
        }

        public AxisType AxisType
        {
            get { return (AxisType)GetValue(AxisTypeProperty) ; }
            set { SetValue(AxisTypeProperty, value); }
        }

        internal virtual void DeriveTicks()
        {
            int n = NumberOfTicks;
            if (n == 0)
            {
                Ticks = new double[0];
                Coefficient = new double[0];
                Exponent = new int[0];
                RequiredDPs = new int[0];
                return;
            }
            else if (ticksOverride != null && ticksOverride.Length > 0)
            {
                DeriveTicksFromOverrides();
                return;
            }
            const double fracTolerance = 1e-6;
            // Algorithm to derive tick positions given that there should be at most n ticks on the axis
            // First the interval that gives exactly n ticks is found, then this is adjusted to give sensible
            // tick points
            double max = Max; double min = Min;
            double delta = max - min;
            if (delta == 0) delta = 1; // Check for improperly initialised axis size
            if (n == 0) n = 1; // Check for improperly initialised n
            double nd = (double)n;
            double approxInterval = delta / nd;
            // Exponent of the least significant figure of the interval
            int intervalExp = (int)Math.Floor(Math.Log10(approxInterval));
            int intervalIntCoeff = (int)Math.Floor(approxInterval / Math.Pow(10, intervalExp) + fracTolerance);
            // LSF must be either 1, 2, 5 or 10, whichever is directly above the current value      
            if (intervalIntCoeff > 5) { intervalIntCoeff = 1; intervalExp++; }
            else if (intervalIntCoeff > 2) { intervalIntCoeff = 5; }
            else if (intervalIntCoeff > 1) { intervalIntCoeff = 2; }
            else { intervalIntCoeff = 1; }
            //
            double intervalCoeff = ((double)intervalIntCoeff);
            double interval = intervalIntCoeff * Math.Pow(10, intervalExp);
            //
            double firstTick;
            double absTolerance = fracTolerance * interval;
            if ((min - absTolerance) < 0)
            {
                firstTick = (min - absTolerance) - ((min - absTolerance) % interval);
            }
            else
            {
                firstTick = interval - ((min - absTolerance) % interval) + (min - absTolerance);
            }
            int firstTickExp = (firstTick == 0) ? intervalExp : (int)Math.Floor(Math.Log10(Math.Abs(firstTick)));
            double firstTickCoeff = firstTick / Math.Pow(10, firstTickExp);
            //
            int nTicks = (int)Math.Floor((max - firstTick + absTolerance) / interval) + 1;
            Ticks = new double[nTicks];
            Coefficient = new double[nTicks];
            Exponent = new int[nTicks];
            RequiredDPs = new int[nTicks];
            double tempTick = firstTick;
            double tempCoeff = firstTickCoeff;
            int tempExp = firstTickExp;
            for (int i = 0; i < nTicks; ++i)
            {
                Ticks[i] = tempTick;
                Coefficient[i] = tempCoeff;
                Exponent[i] = tempExp;
                tempTick += interval;
                RequiredDPs[i] = Math.Abs(intervalExp - tempExp);
                //
                if (tempExp > intervalExp)
                {
                    tempCoeff += intervalCoeff / Math.Pow(10, (tempExp - intervalExp));
                }
                else
                {
                    tempExp = intervalExp;
                    tempCoeff = intervalCoeff + tempCoeff / Math.Pow(10, (intervalExp - tempExp));
                }
                if (tempCoeff >= 10)
                {
                    tempCoeff /= 10;
                    tempExp++;
                }
                if ((tempCoeff > -1) && (tempCoeff < -absTolerance))
                {
                    tempCoeff *= 10;
                    tempExp--;
                }
                tempCoeff = Math.Round(tempCoeff, RequiredDPs[i]);
            }
        }

        protected void DeriveTicksFromOverrides()
        {
            int startTick = -1, endTick = -1; // The index of the first and last ticks in the viewed region
            double max = Max; double min = Min;
            int nTicks = ticksOverride.Length;
            int intervalExp = 1;
            if (cachedTicksOverride == null || cachedTicksOverride.Length != nTicks)
            {
                cachedTicksOverride = new double[nTicks];
                cachedCoefficientOverride = new double[nTicks];
                cachedExponentOverride = new int[nTicks];
                cachedRequiredDPsOverride = new int[nTicks];
            }
            for (int i = 0; i < ticksOverride.Length; ++i)
            {
                if (startTick == -1 && ticksOverride[i] >= min && ticksOverride[i] <= max)
                {
                    startTick = i;
                    endTick = nTicks - 1;
                }
                if (startTick != -1)
                {
                    if (ticksOverride[i] > max)
                    {
                        endTick = i - 1;
                        intervalExp = (int)Math.Floor(Math.Log10(ticksOverride[endTick] - ticksOverride[startTick]));
                        break;
                    }
                    // Tick is in viewed range, so check that its properties are cached and if not cache them
                    if (cachedTicksOverride[i] != ticksOverride[i])
                    {
                        // Not cached
                        cachedTicksOverride[i] = ticksOverride[i];
                        cachedExponentOverride[i] = (int)Math.Floor(Math.Log10(cachedTicksOverride[i]));
                        cachedCoefficientOverride[i] = (int)Math.Floor(cachedTicksOverride[i] / Math.Pow(10, cachedExponentOverride[i]));
                        cachedRequiredDPsOverride[i] = intervalExp - cachedExponentOverride[i];
                    }
                }
            }
            int index = 0;
            if (endTick == -1)
            {
                Ticks = new double[0];
                Coefficient = new double[0];
                Exponent = new int[0];
                RequiredDPs = new int[0];
                return;
            }
            nTicks = endTick - startTick + 1;
            Ticks = new double[nTicks];
            Coefficient = new double[nTicks];
            Exponent = new int[nTicks];
            RequiredDPs = new int[nTicks];
            bool addLabels = (tickLabelsOverride != null) && (tickLabelsOverride.Length == ticksOverride.Length);
            if (addLabels) labels = new string[nTicks];
            for (int i = startTick; i <= endTick; ++i)
            {
                Ticks[index] = cachedTicksOverride[i];
                Coefficient[index] = cachedCoefficientOverride[i];
                Exponent[index] = cachedExponentOverride[i];
                RequiredDPs[index] = cachedRequiredDPsOverride[i];
                if (addLabels) labels[index] = tickLabelsOverride[i];
                index++;
            }
        }

        protected void AddTextToBlock(TextBlock textblock, int i)
        {
            textblock.Text = "";
            textblock.Inlines.Clear();
            if (labels != null && labels.Length > 0)
            {
                textblock.Text = labels[i];
            }
            else if ((AxisType)GetValue(AxisTypeProperty) == AxisType.Date)
            {
                textblock.Text = DateTime.FromOADate(Ticks[i]).ToShortDateString();
            }
            else if ((Exponent[i] < 4) && (Exponent[i] > -4))
            {
                textblock.Text = Ticks[i].ToString("F" + Math.Max(RequiredDPs[i] - Exponent[i], 0).ToString());
            }
            else
            {
                Run coefficient = new Run(Coefficient[i].ToString("F" + RequiredDPs[i].ToString()) + "\u00D7" + "10");
                coefficient.FontSize = textblock.FontSize;
                Run exponent = new Run(Exponent[i].ToString());
                exponent.BaselineAlignment = BaselineAlignment.Superscript;
                exponent.FontSize = textblock.FontSize * 10.0 / 12.0;
                textblock.Inlines.Add(coefficient);
                textblock.Inlines.Add(exponent);
            }
        }

        public abstract double Min { get; set; }

        public abstract double Max { get; set; }
    }
}