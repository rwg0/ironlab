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
using System.Windows.Xps;
using System.Printing;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

namespace IronPlot
{
    /// <summary>
    /// Base class for any items that are displayed on a Plot2D
    /// </summary>
    public class Plot2DItem : DependencyObject
    {
        protected Rect bounds;
        protected PlotPanel host;

        protected XAxis xAxis;
        public XAxis XAxis
        {
            get { return xAxis; }
            set
            {
                if (host == null) xAxis = value;
                else if (host.axes.XAxes.Contains(value)) xAxis = value;
                else throw new Exception("Axis does not belong to plot.");
            }
        }

        protected YAxis yAxis;
        public YAxis YAxis
        {
            get { return yAxis; }
            set
            {
                if (host == null) yAxis = value;
                else if (host.axes.YAxes.Contains(value)) yAxis = value;
                else throw new Exception("Axis does not belong to plot.");
            }
        }

        public Plot2D Plot
        {
            get
            {
                DependencyObject parent = host;
                while ((parent != null) && !(parent is Plot2D))
                {
                    parent = LogicalTreeHelper.GetParent(parent);
                }
                return (parent as Plot2D);
            }
        }
    
        public virtual Rect TightBounds
        {
            get { return bounds; }
        }

        public virtual Rect PaddedBounds
        {
            get { return bounds; }
        }

        internal PlotPanel Host
        {
            get { return host; }
            set
            {
                OnHostChanged(value);
            }
        }

        protected virtual void OnHostChanged(PlotPanel host)
        {
            // Update axis to default if null or it it does not belong to the new plot.
            if ((xAxis == null) || (!host.axes.XAxes.Contains(xAxis)))
            {
                xAxis = host.axes.XAxes.Bottom;
            }

            if ((yAxis == null) || (!host.axes.YAxes.Contains(yAxis)))
            {
                yAxis = host.axes.YAxes.Left;
            }
        }

        internal virtual void OnViewedRegionChanged()
        {
        }

        internal virtual void BeforeArrange()
        {
        }

        internal virtual void OnRender()
        {
        }
    }
}
