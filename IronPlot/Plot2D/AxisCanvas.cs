using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public class AxisCanvas : Canvas
    {
        protected override System.Windows.Size MeasureOverride(Size constraint)
        {
            Size size = base.MeasureOverride(constraint);
            // Allows requests more space than allocated in order to trigger a Measure pass on the parent.
            // We assume that any change that can affect Measure in the AxisCanvas children should triggera full
            // layout pass by the PlotPanel.
            return new System.Windows.Size(ActualWidth + 1, ActualHeight + 1);
        }

        protected override System.Windows.Size ArrangeOverride(Size arrangeSize)
        {
            Size finalSize = base.ArrangeOverride(arrangeSize);
            return finalSize;
        }
    }
}
