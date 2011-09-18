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

        protected void OnHostChanged(Plot2D oldHost)
        {
            // Here the derived class should add its components to the host's PlotPanel canvas, and bind any 
            // transforms.
            // It should also remove its components from the old host.
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

        protected virtual void OnHostChanged(PlotPanel host)
        {
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
