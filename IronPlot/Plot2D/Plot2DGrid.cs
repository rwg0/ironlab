using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace IronPlot
{
    struct HorizontalAlignmentPosition
    {
        public int Column;
        public int ColumnSpan;
    }

    struct VerticalAlignmentPosition
    {
        public int Row;
        public int RowSpan;
    }

    public class Plot2DGrid : Grid
    {   
        public void PlaceAxesFull()
        {
            List<PlotPanel> plotPanels = new List<PlotPanel>();
            List<HorizontalAlignmentPosition> horizontalAligment = new List<HorizontalAlignmentPosition>();
            List<VerticalAlignmentPosition> verticalAligment = new List<VerticalAlignmentPosition>();
            foreach (UIElement element in Children)
            {
                if (element is Plot2D) plotPanels.Add((element as Plot2D).PlotPanel);
                horizontalAligment.Add(new HorizontalAlignmentPosition() 
                { 
                    Column = (int)element.GetValue(ColumnProperty),
                    ColumnSpan = (int)element.GetValue(ColumnSpanProperty) 
                });
                verticalAligment.Add(new VerticalAlignmentPosition()
                {
                    Row = (int)element.GetValue(RowProperty),
                    RowSpan = (int)element.GetValue(RowSpanProperty)
                });
            }

            int iter = 0;
            while (iter <= 1)
            {
                // Step 1: for each PlotPanel in isolation, set the margins for each axis, given the currently visible
                // labels
                
                // Step 2: taking into account all aligned axes across all PlotPanels, 
                // increase axis margins, keeping the total axis length the same, or if this cannot be done
                // (axis length specified, or available space too little), change axis length. 

                // Step 3: take account of any equal axes, across all PlotPanels
                
                iter++;
            }
        }
    }
}
