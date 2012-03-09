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
    public partial class PlotPanel : PlotPanelBase
    {
        protected override void CreateLegends()
        {
            plotItems = new UniqueObservableCollection<Plot2DItem>();
            plotItems.CollectionChanged += new NotifyCollectionChangedEventHandler(annotations_CollectionChanged);
            base.CreateLegends();
        }
    }
}