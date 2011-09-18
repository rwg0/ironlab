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
using System.Collections.Specialized;
using System.Collections;
using System.Collections.ObjectModel;

namespace IronPlot
{
    public partial class PlotPanel : Panel
    {
        internal UniqueObservableCollection<Plot2DItem> plotItems;

        public Collection<Plot2DItem> PlotItems { get { return plotItems; } }

        protected void InitialiseChildenCollection()
        {
            plotItems = new UniqueObservableCollection<Plot2DItem>();
            annotations = new UniqueObservableCollection<UIElement>();
            plotItems.CollectionChanged += new NotifyCollectionChangedEventHandler(plotItems_CollectionChanged);
            annotations.CollectionChanged +=new NotifyCollectionChangedEventHandler(annotations_CollectionChanged);
        }

        protected void plotItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Plot2DItem child in e.OldItems)
                {
                    child.Host = null;
                }
            }
            if (e.NewItems != null)
            {
                foreach (Plot2DItem child in e.NewItems)
                {
                    child.Host = this;
                }
            }
            ViewedRegion = GetBoundsFromChildren();
        }

        protected Rect GetBoundsFromChildren()
        {
            Rect bounds = new Rect(new Size(10, 10)); // default if there are no children.
            Plot2DItem child;
            for (int i = 0; i < plotItems.Count; ++i)
            {
                child = plotItems[i];
                if (i == 0) bounds = child.PaddedBounds;
                else bounds.Union(child.PaddedBounds);
            }
            return bounds;
        }
    }
}
