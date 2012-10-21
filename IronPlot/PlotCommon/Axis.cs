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

    public struct LabelText
    {
        public string Text;
        public string Coefficient;
        public string Exponent;
        public bool IsText;
        public double FontSize;

        public LabelText(string text)
        {
            Text = text; IsText = true;
            Coefficient = Exponent = String.Empty; FontSize = 0;
        }

        public LabelText(string coefficient, string exponent, double fontSize)
        {
            Text = String.Empty; IsText = false;
            Coefficient = coefficient; Exponent = exponent; FontSize = fontSize;
        }

        public string Key
        {
            get { return IsText ? Text : Coefficient + "_" + Exponent + "_" + FontSize.ToString(); }
        }

    }

    public static class FormatOverrides
    {
        public static Func<double, string> Currency = value => value.ToString("N");
        //public static Func<double, string> Currency = value => value.ToString("N");
    }

    /// <summary>
    /// An Axis class contains a Canvas on which the axis and annotations
    /// are presented (one Canvas per Axis). The Canvas typically spans the entire plot region. 
    /// This general approach is suitable for axes of 3D and 2D plots.
    /// </summary>
    public abstract class Axis : ContentControl
    {
        protected AxisCanvas canvas;
       
        internal double[] Ticks;
        internal double[] Coefficient;
        internal int[] Exponent;
        internal int[] RequiredDPs;
        internal LabelText[] LabelText;

        protected string[] labels;
        protected double[] cachedTicksOverride;
        protected double[] cachedCoefficientOverride;
        protected int[] cachedExponentOverride;
        protected int[] cachedRequiredDPsOverride;

        protected double[] ticksOverride;
        protected string[] tickLabelsOverride;

        // We generally want to show ticks which are 'just' outside the Min and Max range. We define 'just'
        // as this tolerance times Max - Min (the difference is always much smaller than a pixel)
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

        protected static double minDate = DateTime.MinValue.AddYears(100).ToOADate();
        protected static double maxDate = DateTime.MaxValue.ToOADate();

        public static DependencyProperty AxisTypeProperty =
            DependencyProperty.Register("AxisType",
            typeof(AxisType), typeof(Axis), new PropertyMetadata(AxisType.Linear, OnAxisTypeChanged));

        public static DependencyProperty NumberOfTicksProperty =
            DependencyProperty.Register("NumberOfTicks",
            typeof(int), typeof(Axis), new PropertyMetadata(10, OnNumberOfTicksChanged));

        public static DependencyProperty TickLengthProperty =
            DependencyProperty.Register("TickLength",
            typeof(double), typeof(Axis), new PropertyMetadata(5.0));

        public static DependencyProperty LabelsVisibleProperty =
            DependencyProperty.Register("LabelsVisible",
            typeof(bool), typeof(Axis), new PropertyMetadata(true));

        public static DependencyProperty TicksVisibleProperty =
            DependencyProperty.Register("TicksVisible",
            typeof(bool), typeof(Axis), new PropertyMetadata(true));

        public static DependencyProperty FormatOverrideProperty =
            DependencyProperty.Register("FormatOverride",
            typeof(Func<double, string>), typeof(Axis), new PropertyMetadata(null));

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

        /// <summary>
        /// The maximum number of ticks visible on the axis; fewer ticks may be shown to make
        /// the tick values 'nice'.
        /// </summary>
        public int NumberOfTicks
        {
            set { SetValue(NumberOfTicksProperty, value); }
            get { return (int)GetValue(NumberOfTicksProperty); }
        }

        /// <summary>
        /// Tick length; this may also be negative.
        /// </summary>
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

        public Func<double, string> FormatOverride
        {
            set { SetValue(FormatOverrideProperty, value); }
            get { return (Func<double, string>)GetValue(FormatOverrideProperty); }
        }

        public abstract double Min { get; set; }

        public abstract double Max { get; set; }
        
        public Axis()
        {
            canvas = new AxisCanvas();
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
                    DeriveTicksDateTime();
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

        int month = 1;
        int dayOfMonth = 1;

        protected virtual void DeriveTicksDateTime()
        {
            // We find the most appropriate tick interval given the maximum number of ticks.
            // First we find the minimum interval.
            DateTime start = DateTime.FromOADate(Min);
            DateTime end = DateTime.FromOADate(Max);
            double minInterval = (Max - Min) / NumberOfTicks;
            // If this is 1 year or more, we increase interval to a nice number of years (1, 2 or 5 x10^n).
            // The first point is the start of a years
            DateTime currentDate;
            List<DateTime> ticksDateTime = new List<DateTime>();
            List<double> ticks = new List<double>();
            List<LabelText> labelText = new List<LabelText>();
            try
            {
                if (minInterval > 365)
                {
                    int yearsInterval = IntervalFromRange((int)(minInterval / 365));
                    DateTime startInWholeYears = new DateTime(start.Year, month, dayOfMonth);
                    currentDate = (start > startInWholeYears) ? startInWholeYears.AddYears(1) : startInWholeYears;
                    int modulus = currentDate.Year % yearsInterval;
                    currentDate = currentDate.AddYears(modulus == 0 ? 0 : -modulus + yearsInterval);
                    while (currentDate <= end)
                    {
                        ticksDateTime.Add(currentDate);
                        ticks.Add(currentDate.ToOADate());
                        labelText.Add(new LabelText(DateString(currentDate)));
                        currentDate = currentDate.AddYears(yearsInterval);
                    }
                }
                // If this is months, we use a nice number of months (1, 2, 3, 4, 6 or 12)
                else if (minInterval >= 28)
                {
                    DateTime startInWholeMonths = new DateTime(start.Year, start.Month, dayOfMonth);
                    currentDate = (start > startInWholeMonths) ? startInWholeMonths.AddMonths(1) : startInWholeMonths;
                    int monthsInterval;
                    if (minInterval < 30) { monthsInterval = 1; }
                    else if (minInterval < 60) { monthsInterval = 2; }
                    else if (minInterval < 90) { monthsInterval = 3; }
                    else if (minInterval < 120) { monthsInterval = 4; }
                    else if (minInterval < 180) { monthsInterval = 6; }
                    else monthsInterval = 12;

                    int modulus = currentDate.Month % monthsInterval;
                    currentDate = currentDate.AddMonths(modulus == 0 ? 0 : -modulus + monthsInterval);
                    while (currentDate <= end)
                    {
                        ticksDateTime.Add(currentDate);
                        ticks.Add(currentDate.ToOADate());
                        labelText.Add(new LabelText(DateString(currentDate)));
                        currentDate = currentDate.AddMonths(monthsInterval);
                    }
                }
                // If this is days, use a nice number of days
                else if (minInterval >= 0.5)
                {
                    DateTime startInWholeDays = new DateTime(start.Year, start.Month, start.Day);
                    int daysInterval = IntervalFromRange((int)Math.Ceiling(minInterval));
                    currentDate = (start > startInWholeDays) ? startInWholeDays.AddDays(1) : startInWholeDays;
                    int modulus = currentDate.Day % daysInterval;
                    currentDate = currentDate.AddDays(modulus == 0 ? 0 : -modulus + daysInterval);
                    while (currentDate <= end)
                    {
                        ticksDateTime.Add(currentDate);
                        ticks.Add(currentDate.ToOADate());
                        labelText.Add(new LabelText(DateString(currentDate)));
                        currentDate = currentDate.AddDays(daysInterval);
                    }
                }
                // ...and display date
                // If this is hours, use a nice number of hours (1, 2, 3, 6, 12, 24)
                else if (minInterval >= 0.5 / 24.0) // half an hour
                {
                    DateTime startInWholeHours = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
                    int hoursInterval;
                    currentDate = (start > startInWholeHours) ? startInWholeHours.AddMinutes(60) : startInWholeHours;
                    double minIntervalHours = minInterval * 24.0;
                    if (minIntervalHours < 1) { hoursInterval = 1; }
                    else if (minIntervalHours < 2) { hoursInterval = 2; }
                    else if (minIntervalHours < 3) { hoursInterval = 3; }
                    else if (minIntervalHours < 4) { hoursInterval = 4; }
                    else if (minIntervalHours < 6) { hoursInterval = 6; }
                    else hoursInterval = 12;
                    int modulus = currentDate.Hour % hoursInterval;
                    currentDate = currentDate.AddHours(modulus == 0 ? 0 : -modulus + hoursInterval);
                    while (currentDate <= end)
                    {
                        ticksDateTime.Add(currentDate);
                        ticks.Add(currentDate.ToOADate());
                        labelText.Add(new LabelText(TimeString(currentDate)));
                        currentDate = currentDate.AddHours(hoursInterval);
                    }
                }
                // If this is minutes, use a nice number of minutes (1, 2, 5, 10, 15, 30, 60)
                else if (minInterval >= 0.5 / 1440.0) // half a minute
                {
                    DateTime startInWholeMinutes = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0);
                    int minutesInterval;
                    currentDate = (start > startInWholeMinutes) ? startInWholeMinutes.AddMinutes(1) : startInWholeMinutes;
                    double minIntervalMinutes = minInterval * 1400;
                    if (minIntervalMinutes < 1) { minutesInterval = 1; }
                    else if (minIntervalMinutes < 2) { minutesInterval = 2; }
                    else if (minIntervalMinutes < 5) { minutesInterval = 5; }
                    else if (minIntervalMinutes < 10) { minutesInterval = 10; }
                    else if (minIntervalMinutes < 15) { minutesInterval = 15; }
                    else minutesInterval = 30;
                    int modulus = currentDate.Minute % minutesInterval;
                    currentDate = currentDate.AddMinutes(modulus == 0 ? 0 : -modulus + minutesInterval);
                    while (currentDate <= end)
                    {
                        ticksDateTime.Add(currentDate);
                        ticks.Add(currentDate.ToOADate());
                        labelText.Add(new LabelText(TimeString(currentDate)));
                        currentDate = currentDate.AddMinutes(minutesInterval);
                    }
                }
                // If this is seconds, use a nice number of seconds (1, 2, 5, 10, 15, 30, 60)
                else if (minInterval >= 0.5 / 86400.0) // half a second
                {
                    DateTime startInWholeSeconds = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second);
                    int secondsInterval;
                    currentDate = (start > startInWholeSeconds) ? startInWholeSeconds.AddSeconds(1) : startInWholeSeconds;
                    double minIntervalSeconds = minInterval * 86400;
                    if (minIntervalSeconds < 1) { secondsInterval = 1; }
                    else if (minIntervalSeconds < 2) { secondsInterval = 2; }
                    else if (minIntervalSeconds < 5) { secondsInterval = 5; }
                    else if (minIntervalSeconds < 10) { secondsInterval = 10; }
                    else if (minIntervalSeconds < 15) { secondsInterval = 15; }
                    else secondsInterval = 30;
                    int modulus = currentDate.Second % secondsInterval;
                    currentDate = currentDate.AddSeconds(modulus == 0 ? 0 : -modulus + secondsInterval);
                    while (currentDate <= end)
                    {
                        ticksDateTime.Add(currentDate);
                        ticks.Add(currentDate.ToOADate());
                        labelText.Add(new LabelText(LongTimeString(currentDate)));
                        currentDate = currentDate.AddSeconds(secondsInterval);
                    }
                }
                // ...and display time
                // If less, just display seconds.
                else
                {
                    DateTime startInWholeSeconds = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second);
                    double secondsStart = start.Subtract(startInWholeSeconds).TotalSeconds;
                    double secondsEnd = end.Subtract(startInWholeSeconds).TotalSeconds;
                    DecomposedNumber firstTick, interval;
                    IntervalFromRange(new Range(secondsStart, secondsEnd), minInterval * 86400, out interval, out firstTick);
                    currentDate = startInWholeSeconds.AddSeconds(firstTick.Value);
                    while (currentDate <= end)
                    {
                        ticksDateTime.Add(currentDate);
                        ticks.Add(currentDate.ToOADate());
                        labelText.Add(new LabelText(LongTimeString(currentDate, -interval.Exponent)));
                        currentDate = currentDate.AddSeconds(interval.Value);
                    }
                }
            }
            catch (Exception) { } // In case of out-of-range DateTimes
            // Phew!
            TicksTransformed = Ticks = ticks.ToArray();
            LabelText = labelText.ToArray();
        }

        private string DateString(DateTime date)
        {
            return date.ToShortDateString();
        }

        private string TimeString(DateTime date)
        {
            return DateString(date) + "\r" + date.ToString("HH:mm");
        }

        private string LongTimeString(DateTime date)
        {
            return DateString(date) + "\r" + date.ToString("HH:mm:ss");
        }

        private string LongTimeString(DateTime date, int secondsDecimalPlaces)
        {
            double seconds = date.Second + (double)date.Millisecond / 1000;
            return DateString(date) + "\r" + date.ToString("HH:mm") + ":" + seconds.ToString("F" + secondsDecimalPlaces.ToString());
        }

        protected virtual void DeriveTicksLinear()
        {
            if (NumberOfTicks == 0)
            {
                Ticks = new double[0]; Coefficient = new double[0]; Exponent = new int[0]; RequiredDPs = new int[0];
                return;
            }
            else if (ticksOverride != null && ticksOverride.Length > 0)
            {
                DeriveTicksFromOverrides();
                return;
            }
            
            // Algorithm to derive tick positions given that there should be at most n ticks on the axis
            // First the interval that gives exactly n ticks is found, then this is adjusted up if necessary 
            // to give sensible tick points.
            double max = Max; double min = Min;
            double delta = max - min;
            if (delta <= 0) delta = 1; // Potect against improperly initialised axis size.
            double minInterval = delta / (double)NumberOfTicks;
            
            DecomposedNumber interval, firstTick;
            IntervalFromRange(new Range(min, max), minInterval, out interval, out firstTick);

            double absTolerance = fractionalTolerance * interval.Value;
            int nTicks = (int)Math.Floor((max - firstTick.Value + absTolerance) / interval.Value) + 1;
            Ticks = new double[nTicks]; Coefficient = new double[nTicks]; Exponent = new int[nTicks]; 
            RequiredDPs = new int[nTicks]; LabelText = new LabelText[nTicks];
            double tempTick = firstTick.Value;
            double tempCoeff = firstTick.Coefficient;
            int tempExp = firstTick.Exponent;
            for (int i = 0; i < nTicks; ++i)
            {
                Ticks[i] = tempTick; Coefficient[i] = tempCoeff; Exponent[i] = tempExp;
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

        protected void DeriveTicksLog()
        {
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
                Coefficient = new double[nTicks]; Exponent = new int[nTicks];
                RequiredDPs = new int[nTicks]; LabelText = new LabelText[nTicks];

                int index = 0;
                for (int i = firstTick; i <= lastTick; i += interval)
                {
                    TicksTransformed[index] = i;
                    Ticks[index] = Math.Pow(10, i);
                    Coefficient[index] = 1;
                    Exponent[index] = i;
                    RequiredDPs[index] = 0;
                    index++;
                }
            }
        }

        private int IntervalFromRange(int minInterval)
        {
            // Exponent of the least significant figure of the interval.
            int intervalExponent = (int)Math.Floor(Math.Log10((double)minInterval));
            double factor = Math.Pow(10, intervalExponent);
            int intervalIntCoefficient = (int)Math.Ceiling(minInterval / factor);

            // LSF must be either 1, 2, 5 or 10, whichever is directly above the current value.      
            if (intervalIntCoefficient > 5) { intervalIntCoefficient = 1; factor *= 10; }
            else if (intervalIntCoefficient > 2) { intervalIntCoefficient = 5; }
            else if (intervalIntCoefficient > 1) { intervalIntCoefficient = 2; }
            else { intervalIntCoefficient = 1; }

            return intervalIntCoefficient * (int)factor;
        }

        private void IntervalFromRange(Range range, double minInterval, out DecomposedNumber interval, out DecomposedNumber firstTick) 
        {
            // Exponent of the least significant figure of the interval.
            int intervalExponent = (int)Math.Floor(Math.Log10(minInterval));
            int intervalIntCoefficient = (int)Math.Floor(minInterval / Math.Pow(10, intervalExponent) + fractionalTolerance);

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

        protected void UpdateLabelText(int i)
        {
            if (FormatOverride != null)
            {
                LabelText[i] = new LabelText(FormatOverride(Ticks[i]));
                return;
            }
            if (AxisType == AxisType.Date) return;
            if (labels != null && labels.Length > 0) LabelText[i] = new LabelText(labels[i]);
            else if ((Exponent[i] >= 4) || (Exponent[i] <= -4))
            {
                string coefficient = Coefficient[i].ToString("F" + RequiredDPs[i].ToString()) + "\u00D7" + "10";
                string exponent = Exponent[i].ToString();
                LabelText[i] = new LabelText(coefficient, exponent, FontSize);
            }
            else LabelText[i] = new LabelText(Ticks[i].ToString("F" + Math.Max(RequiredDPs[i] - Exponent[i], 0).ToString()));
        }

        protected void AddTextToBlock(TextBlock textblock, int i)
        {
            textblock.Text = "";
            textblock.Inlines.Clear();
            if (LabelText[i].IsText) textblock.Text = LabelText[i].Text;
            else 
            {
                Run coefficient = new Run(LabelText[i].Coefficient);
                coefficient.FontSize = textblock.FontSize;
                Run exponent = new Run(LabelText[i].Exponent);
                exponent.BaselineAlignment = BaselineAlignment.Superscript;
                exponent.FontSize = textblock.FontSize * 10.0 / 12.0;
                textblock.Inlines.Add(coefficient);
                textblock.Inlines.Add(exponent);
            }
        }

        protected abstract void UpdateTicksAndLabels();
    }
}