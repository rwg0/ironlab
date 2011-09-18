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
using System.Windows.Threading;
using System.Collections.Specialized;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace IronPlot
{
    [ContentProperty("Annotations")]
    public partial class PlotPanel : Panel
    {
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.RegisterAttached(
            "Position",
            typeof(Position),
            typeof(PlotPanel),
            new PropertyMetadata(Position.Right, OnPositionPropertyChanged));

        private static void OnPositionPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            PlotPanel parent = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(element)) as PlotPanel;
            if (parent != null)
            {
                parent.AddOrRemoveAnnotation((UIElement)element, (Position)e.OldValue, AddOrRemove.Remove);
                parent.AddOrRemoveAnnotation((UIElement)element, (Position)e.NewValue, AddOrRemove.Add);
            }
        }

        public static Position GetPosition(UIElement element)
        {
            return (Position)element.GetValue(PositionProperty);
        }

        public static void SetPosition(UIElement element, Position position)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(PositionProperty, position);
        }

        public Collection<UIElement> Annotations
        { 
            get { return annotations; }
            set { throw new NotSupportedException(); }
        }
        
        internal UniqueObservableCollection<UIElement> annotations;

        protected void annotations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (UIElement element in e.OldItems)
                {
                    AddOrRemoveAnnotation(element, (Position)element.GetValue(PositionProperty), AddOrRemove.Remove);
                }
            }
            if (e.NewItems != null)
            {
                foreach (UIElement element in e.NewItems)
                {
                    AddOrRemoveAnnotation(element, (Position)element.GetValue(PositionProperty), AddOrRemove.Add);
                }
            }
        }

        protected enum AddOrRemove { Add, Remove };
        protected void AddOrRemoveAnnotation(UIElement element, Position position, AddOrRemove action)
        {
            StackPanel parent;
            switch (position)
            {
                case Position.Left:
                    parent = annotationsLeft;
                    if (element is FrameworkElement)
                    {
                        (element as FrameworkElement).HorizontalAlignment = HorizontalAlignment.Right;
                        (element as FrameworkElement).VerticalAlignment = VerticalAlignment.Center;
                    }
                    break;
                case Position.Right:
                    parent = annotationsRight;
                    if (element is FrameworkElement)
                    {
                        (element as FrameworkElement).HorizontalAlignment = HorizontalAlignment.Left;
                        (element as FrameworkElement).VerticalAlignment = VerticalAlignment.Center;
                    }
                    break;
                case Position.Top:
                    parent = annotationsTop;
                    if (element is FrameworkElement)
                    {
                        (element as FrameworkElement).HorizontalAlignment = HorizontalAlignment.Center;
                        (element as FrameworkElement).VerticalAlignment = VerticalAlignment.Bottom;
                    }
                    break;
                default:
                    parent = annotationsBottom;
                    if (element is FrameworkElement)
                    {
                        (element as FrameworkElement).HorizontalAlignment = HorizontalAlignment.Center;
                        (element as FrameworkElement).VerticalAlignment = VerticalAlignment.Top;
                    }
                    break;

            }
            if (action == AddOrRemove.Add)
                parent.Children.Add(element);
            else parent.Children.Remove(element);
        }

        // An alternate to using Styles: more suited to REPL use.
        //LabelProperties labelProperties;

        protected void CreateLegends()
        {
            plotItems = new UniqueObservableCollection<Plot2DItem>();
            plotItems.CollectionChanged += new NotifyCollectionChangedEventHandler(annotations_CollectionChanged);
            
            //labelProperties = new LabelProperties();
            //
            // Left annotations
            annotationsLeft = new StackPanel();
            annotationsLeft.Orientation = Orientation.Horizontal;
            //annotationsLeft.FlowDirection = FlowDirection.RightToLeft;
            this.Children.Add(annotationsLeft);
            //
            // Right legend
            annotationsRight = new StackPanel();
            annotationsRight.Orientation = Orientation.Horizontal;
            //labelProperties.BindTextBlock(rightLabel);
            this.Children.Add(annotationsRight);
            //
            // Top legend
            annotationsTop = new StackPanel();
            this.Children.Add(annotationsTop);
            //
            // Bottom legend
            annotationsBottom = new StackPanel();
            this.Children.Add(annotationsBottom);
            //
        }
    }
}