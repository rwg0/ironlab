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
    public enum AxisType { Linear, Log, Date, LinearReversed }

    public struct DecomposedNumber
    {
        public double Value;
        public double Coefficient;
        public int Exponent;

        public DecomposedNumber(double value, double coefficient, int exponent)
        {
            Value = value; Coefficient = coefficient; Exponent = exponent;
        }
    }

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

        private const double fractionalTolerance = 1e-6;

        // The transform applied to graph coordinates before conversion to canvas coordinates.
        internal Func<double, double> GraphTransform = value => value;

        // The transform applied to canvas coordinates after conversion to graph coordinates, as final step.
        internal Func<double, double> CanvasTransform = value => value;

        // Transformed tick positions.
        // The transform is a log10 transform for log axes and a multiplication be -1 for reversed axes.
        internal double[] TicksTransformed;
        internal double MinTransformed;
        internal double MaxTransformed;

        public static DependencyProperty AxisTypeProperty =
            DependencyProperty.Register("AxisTypeProperty",
            typeof(AxisType), typeof(Axis), new PropertyMetadata(AxisType.Linear, OnAxisTypeChanged));

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

        public abstract double Min { get; set; }

        public abstract double Max { get; set; }
        
        public Axis()
        {
            canvas = new Canvas();
            this.Content = canvas;
        }

        protected static void OnNumberOfTicksChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis)obj).DeriveTicks();
        }

        protected static void OnAxisTypeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis)obj).OnAxisTypeChanged();
        }

        protected virtual void OnAxisTypeChanged()
        {
            switch (AxisType)
            {
                case AxisType.Log:
                    GraphTransform = value => Math.Log10(value);
                    CanvasTransform = value => Math.Pow(10.0, value);
                    break;
                case AxisType.LinearReversed:
                    GraphTransform = value => -value;
                    CanvasTransform = value => -value;
                    break;
                default:
                    GraphTransform = value => value;
                    CanvasTransform = value => value;
                    break;
            }
            DeriveTicks();
            UpdateTicksAndLabels();
        }

        public AxisType AxisType
        {
            get { return (AxisType)GetValue(AxisTypeProperty) ; }
            set { SetValue(AxisTypeProperty, value); }
        }


        internal virtual void DeriveTicks()
        {
            MaxTransformed = GraphTransform(Max);
            MinTransformed = GraphTransform(Min);
            switch (AxisType)
            {
                case AxisType.Linear:
                    DeriveTicksLinear();
                    break;
                case AxisType.Date:
                    DeriveTicksLinear();
                    break;
                case AxisType.Log:
                    DeriveTicksLog();
                    break;
                case AxisType.LinearReversed:
                    DeriveTicksLinear();
                    double temp = MaxTransformed;
                    MaxTransformed = MinTransformed;
                    MinTransformed = temp;
                    Ticks = Ticks.Reverse().ToArray();
                    TicksTransformed = TicksTransformed.Reverse().ToArray();
                    break;
            }
        }

        protected virtual void DeriveTicksLinear()
        {
            if (NumberOfTicks == 0)
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
            
            // Algorithm to derive tick positions given that there should be at most n ticks on the axis
            // First the interval that gives exactly n ticks is found, then this is adjusted to give sensible
            // tick points.
            double max = Max; double min = Min;
            double delta = max - min;
            if (delta <= 0) delta = 1; // Potect against improperly initialised axis size.
            double approxInterval = delta / (double)NumberOfTicks;
            
            DecomposedNumber interval, firstTick;
            IntervalFromRange(new Range(min, max), approxInterval, out interval, out firstTick);

            double absTolerance = fractionalTolerance * interval.Value;
            int nTicks = (int)Math.Floor((max - firstTick.Value + absTolerance) / interval.Value) + 1;
            Ticks = new double[nTicks];
            Coefficient = new double[nTicks];
            Exponent = new int[nTicks];
            RequiredDPs = new int[nTicks];
            double tempTick = firstTick.Value;
            double tempCoeff = firstTick.Coefficient;
            int tempExp = firstTick.Exponent;
            for (int i = 0; i < nTicks; ++i)
            {
                Ticks[i] = tempTick;
                Coefficient[i] = tempCoeff;
                Exponent[i] = tempExp;
                tempTick += interval.Value;
                RequiredDPs[i] = Math.Abs(interval.Exponent - tempExp);
                //
                if (tempExp > interval.Exponent)
                {
                    tempCoeff += interval.Coefficient / Math.Pow(10, (tempExp - interval.Exponent));
                }
                else
                {
                    tempExp = interval.Exponent;
                    tempCoeff = interval.Coefficient + tempCoeff / Math.Pow(10, (interval.Exponent - tempExp));
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
            TicksTransformed = new double[Ticks.Length];
            for (int i = 0; i < TicksTransformed.Length; ++i) TicksTransformed[i] = GraphTransform(Ticks[i]);
        }

        private void IntervalFromRange(Range range, double approxInterval, out DecomposedNumber interval, out DecomposedNumber firstTick) 
        {
            // Exponent of the least significant figure of the interval.
            int intervalExponent = (int)Math.Floor(Math.Log10(approxInterval));
            int intervalIntCoefficient = (int)Math.Floor(approxInterval / Math.Pow(10, intervalExponent) + fractionalTolerance);

            // LSF must be either 1, 2, 5 or 10, whichever is directly above the current value.      
            if (intervalIntCoefficient > 5) { intervalIntCoefficient = 1; intervalExponent++; }
            else if (intervalIntCoefficient > 2) { intervalIntCoefficient = 5; }
            else if (intervalIntCoefficient > 1) { intervalIntCoefficient = 2; }
            else { intervalIntCoefficient = 1; }
            //
            interval = new DecomposedNumber(intervalIntCoefficient * Math.Pow(10, intervalExponent), (double)intervalIntCoefficient, intervalExponent);
            //
            // Now get the first tick
            double firstTickValue;
            double absoluteTolerance = fractionalTolerance * interval.Value;
            if ((range.Min - absoluteTolerance) < 0)
            {
                firstTickValue = (range.Min - absoluteTolerance) - ((range.Min - absoluteTolerance) % interval.Value);
            }
            else
            {
                firstTickValue = interval.Value - ((range.Min - absoluteTolerance) % interval.Value) + (range.Min - absoluteTolerance);
            }
            int firstTickExp = (firstTickValue == 0) ? interval.Exponent : (int)Math.Floor(Math.Log10(Math.Abs(firstTickValue)));
            firstTick = new DecomposedNumber(firstTickValue, firstTickValue / Math.Pow(10, firstTickExp), firstTickExp);
        }

        static double[] logsOfCountingNumbers = null;

        protected void DeriveTicksLog()
        {
            if (Min == 0)
            {

            }
            
            // In this case, a transform of log10 has been applied to the max and min.
            double min = MinTransformed;
            double max = MaxTransformed;
            // Within each integer range, we need points at the starting point + ln(2), ln(3), ln(4) etc
            if (logsOfCountingNumbers == null)
            {
                logsOfCountingNumbers = new double[9];
                for (int i = 1; i < 10; ++i) logsOfCountingNumbers[i - 1] = Math.Log10(i);
            }
            // Range is assumed to be inclusive.
            int rangeStart = (int)Math.Ceiling(min - fractionalTolerance);
            int rangeEnd = (int)Math.Floor(max + fractionalTolerance);

            int nPossibleTicks = rangeEnd - rangeStart + 1;

            if (nPossibleTicks < 2)
            {
                // We are back in a linear-(ish) regime, so use the normal algorithm, just adjust the positions of course. 
                DeriveTicksLinear();
                //for (int i = 0; i < Ticks.Length; ++i) TicksTransformed[i] = GraphTransform(Ticks[i]);
            }
            else
            {
                int firstTick = rangeStart;
                int lastTick = rangeEnd;
                int interval = 1;
                if (nPossibleTicks > NumberOfTicks)
                {
                    double approxInterval = (double)nPossibleTicks / (double)NumberOfTicks;
                    int intervalExponent = 0;
                    // log10 of largest double is about 300:
                    if (approxInterval > 100) { approxInterval /= 10; interval *= 10; intervalExponent++; }
                    if (approxInterval > 10) { approxInterval /= 10; interval *= 10; intervalExponent++; }
                    int intervalIntCoefficient = (int)Math.Floor(approxInterval);
                    if (intervalIntCoefficient > 5) { intervalIntCoefficient = 1; interval *= 10; intervalExponent++; }
                    else if (intervalIntCoefficient > 2) { intervalIntCoefficient = 5; }
                    else if (intervalIntCoefficient > 1) { intervalIntCoefficient = 2; }
                    else { intervalIntCoefficient = 1; }
                    interval *= intervalIntCoefficient;
                    int modulo = rangeStart % interval;

                    if (firstTick < 0)
                        firstTick = (modulo == 0) ? firstTick : firstTick - modulo;
                    else firstTick = (modulo == 0) ? firstTick : firstTick + (interval - modulo);
                   
                    modulo = rangeEnd % interval;

                    if (lastTick < 0)
                        lastTick = (modulo == 0) ? lastTick : lastTick + modulo;
                    else lastTick = (modulo == 0) ? lastTick : lastTick - modulo;
                }

                int nTicks = (lastTick - firstTick) / interval + 1;

                TicksTransformed = new double[nTicks];
                Ticks = new double[nTicks];
                Coefficient = new double[nTicks];
                Exponent = new int[nTicks];
                RequiredDPs = new int[nTicks];

                int index = 0;
                for (int i = firstTick; i <= lastTick; i += interval)
                {
                    TicksTransformed[index] = i;
                    Ticks[index] = Math.Pow(10, i);
                    Coefficient[index] = 1;
                    Exponent[index] = i;
                    RequiredDPs[index] = 0;
                    index++;
                    //for (int j = 1; j < 10; ++j)
                    //{
                    //    double trial = i + logsOfCountingNumbers[j - 1];
                    //    if (trial > MinTransformed && trial < MaxTransformed)
                    //    {
                    //        TicksTransformed[index] = i + logsOfCountingNumbers[j - 1];
                    //        Ticks[index] = rangeUntransformed * j;
                    //        index++;
                    //    }
                    //}
                }
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

        protected abstract void UpdateTicksAndLabels();
    }
}