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
            // Assume vertical alignment for now
            axes.xAxisBottom.LabelsVisible = axes.xAxisTop.LabelsVisible = false;
            axes.xAxisBottom.TicksVisible = axes.xAxisTop.TicksVisible = false;
            axes.xAxisBottom.desiredLength = axes.xAxisTop.desiredLength = 20;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override void BeforeArrange()
        {
            foreach (Slider slider in sliderList)
            {
                slider.Height = canvasLocation.Height;
                slider.SetValue(Canvas.TopProperty, canvasLocation.Top - axesCanvasLocation.Top);
                minimumAxesMargin.Left = Math.Max(minimumAxesMargin.Left, slider.ActualWidth);
            }
            base.BeforeArrange();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }
    }
}
