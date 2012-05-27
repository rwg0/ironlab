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

namespace IronPlot
{
    /// <summary>
    /// Panel class derived from PlotPanel suitable for colour bars
    /// </summary>
    public class ColourBarPanel : PlotPanel
    {
        internal List<Slider> sliderList;
        
        internal ColourBarPanel() : base()
        {
            // Assume vertical alignment for now.
            Axes.xAxisBottom.LabelsVisible = Axes.xAxisTop.LabelsVisible = false;
            Axes.xAxisBottom.TicksVisible = Axes.xAxisTop.TicksVisible = false;
            var allAxes = Axes.XAxes.Concat(Axes.YAxes);
            foreach (Axis2D axis in allAxes) axis.GridLines.Visibility = Visibility.Collapsed; 
            Axes.Width = 20;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (Slider slider in sliderList) slider.Measure(availableSize);
            double thumbWidth = sliderList[0].DesiredSize.Width;
            double thumbSemiHeight = sliderList[0].DesiredSize.Height / 2;
            Axes.MinAxisMargin = new Thickness(thumbWidth, thumbSemiHeight, 0, thumbSemiHeight);
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double thumbWidth = sliderList[0].DesiredSize.Width;
            double thumbHeight = sliderList[0].DesiredSize.Height;
            Rect sliderLocation = new Rect(new Point(CanvasLocation.Left - thumbWidth, CanvasLocation.Top - thumbHeight/2),
                new Size(thumbWidth, CanvasLocation.Height + thumbHeight));
            foreach (Slider slider in sliderList) slider.Arrange(sliderLocation);
            return base.ArrangeOverride(finalSize);
        }

        internal void AddSliders(List<Slider> sliders)
        {
            this.sliderList = sliders;
            foreach (Slider slider in sliderList)
            {
                this.Children.Add(slider);
                slider.SetValue(Grid.ZIndexProperty, 400);
            }
        }

        internal void RemoveSliders()
        {
            if (sliderList == null) return;
            foreach (Slider slider in sliderList) this.Children.Remove(slider);
        }
    }
}
