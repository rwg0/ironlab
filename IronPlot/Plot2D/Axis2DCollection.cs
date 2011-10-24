// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Reflection;
using System.Windows.Controls;

namespace IronPlot
{
    public class XAxis2DCollection : Axis2DCollection
    {
        public XAxis2DCollection(PlotPanel panel)
            : base(panel) { }

        public XAxis Top { get { return this[1] as XAxis; } }
        public XAxis Bottom { get { return this[0] as XAxis; } }
    }

    public class YAxis2DCollection : Axis2DCollection
    {
        public YAxis2DCollection(PlotPanel panel)
            : base(panel) { }

        public YAxis Left { get { return this[0] as YAxis; } }
        public YAxis Right { get { return this[1] as YAxis; } }
    }

    public class Axis2DCollection : Collection<Axis2D>
    {
        // The panel to which the axes belong.
        PlotPanel panel;   

        private GridLines gridLines;
        public GridLines GridLines { get { return gridLines; } }

        public Axis2DCollection(PlotPanel panel)
            : base()
        {
            this.panel = panel;
        }

        protected override void InsertItem(int index, Axis2D newItem)
        {
            base.InsertItem(index, newItem);
            newItem.PlotPanel = panel;
            panel.Children.Add(newItem);
            newItem.SetValue(Grid.ZIndexProperty, 201);
            //if (gridLines == null) gridLines = new GridLines(this);
        }

        protected override void SetItem(int index, Axis2D newItem)
        {
            base.SetItem(index, newItem);
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
        }
    }
}

