// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Reflection;

namespace IronPlot
{
    public class XAxis2DCollection : Axis2DCollection
    {   
        public XAxis2DCollection()
            : base()
        {
        }

        public XAxis Top { get { return this[1] as XAxis; } }
        public XAxis Bottom { get { return this[0] as XAxis; } }
    }

    public class YAxis2DCollection : Axis2DCollection
    {
        public YAxis2DCollection()
            : base()
        {
        }

        public YAxis Left { get { return this[0] as YAxis; } }
        public YAxis Right { get { return this[1] as YAxis; } }
    }
    
    public class Axis2DCollection : DependencyObject
    {
        public static DependencyProperty AxisTypeProperty =
            DependencyProperty.Register("AxisTypeProperty",
            typeof(AxisType), typeof(Axis2DCollection), new PropertyMetadata(AxisType.Number));

        public AxisType AxisType
        {
            get { return (AxisType)GetValue(AxisTypeProperty); }
            set { SetValue(AxisTypeProperty, value); }
        }

        public static DependencyProperty NumberOfTicksProperty =
            DependencyProperty.Register("NumberOfTicksProperty",
            typeof(int), typeof(Axis2DCollection), new PropertyMetadata(10));

        public int NumberOfTicks
        {
            get { return (int)GetValue(NumberOfTicksProperty); }
            set { SetValue(NumberOfTicksProperty, value); }
        }

        public static DependencyProperty TickLengthProperty =
            DependencyProperty.Register("TickLengthProperty",
            typeof(double), typeof(Axis2DCollection), new PropertyMetadata(5.0));

        public double TickLength
        {
            get { return (double)GetValue(TickLengthProperty); }
            set { SetValue(TickLengthProperty, value); }
        }

        public static DependencyProperty LabelsVisibleProperty =
            DependencyProperty.Register("LabelsVisibleProperty",
            typeof(bool), typeof(Axis2DCollection), new PropertyMetadata(true));

        public bool LabelsVisible
        {
            get { return (bool)GetValue(LabelsVisibleProperty); }
            set { SetValue(LabelsVisibleProperty, value); }
        }

        public static DependencyProperty TicksVisibleProperty =
            DependencyProperty.Register("TicksVisibleProperty",
            typeof(bool), typeof(Axis2DCollection), new PropertyMetadata(true));

        public bool TicksVisible
        {
            get { return (bool)GetValue(TicksVisibleProperty); }
            set { SetValue(TicksVisibleProperty, value); }
        }

        private GridLines gridLines;
        public GridLines GridLines { get { return gridLines; } }

        private Axis2DCollectionInternal axis2DCollection = new Axis2DCollectionInternal();

        private LabelProperties axisLabelProperties = new LabelProperties();

        public LabelProperties AxisLabels
        {
            get { return axisLabelProperties; }
        }

        private LabelProperties tickLabelProperties = new LabelProperties();

        public LabelProperties TickLabels
        {
            get { return tickLabelProperties; }
        }

        protected void BindAxis2D(Axis2D axis)
        {
            FieldInfo[] fields = typeof(Axis2DCollection).GetFields();
            foreach (FieldInfo field in fields)
            {
                DependencyProperty dp = (DependencyProperty)field.GetValue(this);
                FieldInfo fieldInfo = axis.GetType().GetField(dp.Name);
                if (fieldInfo == null) fieldInfo = axis.GetType().BaseType.GetField(dp.Name);
                if (fieldInfo == null) fieldInfo = axis.GetType().BaseType.BaseType.GetField(dp.Name);
                DependencyProperty dpAxis = (DependencyProperty)(fieldInfo.GetValue(axis));
                Binding bindingTransform = new Binding(dp.Name);
                bindingTransform.Source = this;
                bindingTransform.Mode = BindingMode.OneWay;
                BindingOperations.SetBinding(axis, dpAxis, bindingTransform);
            }
        }

        internal void AddAxis(Axis2D axis)
        {
            axis2DCollection.Add(axis);
            BindAxis2D(axis);
            if (gridLines == null) gridLines = new GridLines(this);
        }

        public Axis2D this[int index]
        {
            set { axis2DCollection[index] = value; }
            get { return axis2DCollection[index]; }
        }
    }

    internal class Axis2DCollectionInternal : Collection<Axis2D>
    {

        public Axis2DCollectionInternal()
            : base()
        {
        }

        protected override void InsertItem(int index, Axis2D newItem)
        {
            base.InsertItem(index, newItem);
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

