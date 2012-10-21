// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Reflection;

namespace IronPlot.Plotting3D
{
    public class Axis3DCollection : DependencyObject
    {
        public static DependencyProperty AxisTypeProperty =
            DependencyProperty.Register("AxisType",
            typeof(AxisType), typeof(Axis3DCollection), new PropertyMetadata(AxisType.Linear));

        public AxisType AxisType
        {
            get { return (AxisType)GetValue(AxisTypeProperty); }
            set { SetValue(AxisTypeProperty, value); }
        }
        
        public static DependencyProperty NumberOfTicksProperty =
            DependencyProperty.Register("NumberOfTicks",
            typeof(int), typeof(Axis3DCollection), new PropertyMetadata(10));
        
        public int NumberOfTicks
        {
            get { return (int)GetValue(NumberOfTicksProperty); }
            set { SetValue(NumberOfTicksProperty, value); }
        }

        public static DependencyProperty TickLengthProperty =
            DependencyProperty.Register("TickLength",
            typeof(double), typeof(Axis3DCollection), new PropertyMetadata(0.05));

        public double TickLength
        {
            get { return (double)GetValue(TickLengthProperty); }
            set { SetValue(TickLengthProperty, value); }
        }

        public static DependencyProperty LabelsVisibleProperty =
            DependencyProperty.Register("LabelsVisible",
            typeof(bool), typeof(Axis3DCollection), new PropertyMetadata(true));

        public bool LabelsVisible
        {
            get { return (bool)GetValue(LabelsVisibleProperty); }
            set { SetValue(LabelsVisibleProperty, value); }
        }

        public static DependencyProperty TicksVisibleProperty =
            DependencyProperty.Register("TicksVisible",
            typeof(bool), typeof(Axis3DCollection), new PropertyMetadata(true));

        public bool TicksVisible
        {
            get { return (bool)GetValue(TicksVisibleProperty); }
            set { SetValue(TicksVisibleProperty, value); }
        }

        private Axis3DCollectionInternal axis3DCollection = new Axis3DCollectionInternal();

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

        protected void BindAxis3D(Axis3D axis)
        {
            FieldInfo[] fields = this.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                DependencyProperty dp = (DependencyProperty)field.GetValue(this);
                FieldInfo fieldInfo = axis.GetType().GetField(string.Concat(dp.Name, "Property"));
                if (fieldInfo == null) fieldInfo = axis.GetType().BaseType.GetField(string.Concat(dp.Name, "Property"));
                if (fieldInfo == null) fieldInfo = axis.GetType().BaseType.BaseType.GetField(string.Concat(dp.Name, "Property"));
                DependencyProperty dpAxis = (DependencyProperty)(fieldInfo.GetValue(axis));
                Binding bindingTransform = new Binding(dp.Name);
                bindingTransform.Source = this;
                bindingTransform.Mode = BindingMode.OneWay;
                BindingOperations.SetBinding(axis, dpAxis, bindingTransform);
            }
        }

        internal void AddAxis(Axis3D axis)
        {
            axis3DCollection.Add(axis);
            BindAxis3D(axis);
        }

        public Axis3D this[int index]
        {
            set { axis3DCollection[index] = value; }
            get { return axis3DCollection[index]; }
        }
    }
    
    internal class Axis3DCollectionInternal : Collection<Axis3D>
    {

        public Axis3DCollectionInternal() : base()
        {
        }

        protected override void InsertItem(int index, Axis3D newItem)
        {
            base.InsertItem(index, newItem);
        }

        protected override void SetItem(int index, Axis3D newItem)
        {
            base.SetItem(index, newItem);
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
        }
    }
}
