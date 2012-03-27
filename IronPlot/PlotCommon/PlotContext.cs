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
using System.Windows.Xps;
using System.Printing;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Windows.Markup;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot
{
    /// <summary>
    /// A wrapper class that allows Plotting API to be used with state information
    /// For example, PlotContext keeps track of the current Plot2D, allowing a condensed syntax
    /// This is mainly intended for use in a scripting language. States are otherwise to be avoided!
    /// </summary>
    public class PlotContext
    {
        static FrameworkElement currentPlot = null; 
        static int? currentPlotIndex = null;
        static Window currentWindow = null;
        static int? currentWindowIndex = null;
        static TabItem currentTabItem = null;
        static int? currentTabItemIndex = null;
        static FrameworkElement currentGrid = null;
        // Dictionary to obtain Window by index
        static Dictionary<int?, Window> windowDictionary = new Dictionary<int?, Window>();
        // Look up by TabControl to obtain Dictionary of TabIem by index
        static Dictionary<TabControl, Dictionary<int?, TabItem>> tabItemDictionaryLookup = new Dictionary<TabControl, Dictionary<int?, TabItem>>();
        // Dictionary of TabItem by index for current TabControl (TabControl of current TabItem)
        static Dictionary<int?, TabItem> tabItemDictionary = null;
        // Look up by Window or TabItem to obtain Dictionary of plot by index
        static Dictionary<FrameworkElement, Dictionary<int?, FrameworkElement>> plotDictionaryLookup = new Dictionary<FrameworkElement, Dictionary<int?, FrameworkElement>>();
        // Dictionary of plot by index for current TabItem (if present) or Window
        static Dictionary<int?, FrameworkElement> plotDictionary = null;

        static bool holdState = false;

        static public bool HoldState
        {
            get { return holdState; }
            set { holdState = value; }
        }

        static public Window CurrentWindow
        {
            get { return currentWindow; }
        }

        static public FrameworkElement CurrentPlot
        {
            get { return currentPlot; }
        }

        static public FrameworkElement NextPlot
        {
            get { return currentPlot; }
            //set { currentPlot = value; }
        }

        static public TabItem CurrentTabItem
        {
            get { return currentTabItem; }
        }

        static public FrameworkElement CurrentGrid
        {
            get { return currentGrid; }
            set { currentGrid = value; }
        }

        static public int? CurrentWindowIndex
        {
            get { return currentWindowIndex; }
            set
            {
                currentWindow = null;
                if (value == null || windowDictionary.TryGetValue(value, out currentWindow))
                {
                    // Window exists in the Dictionary: set to use this Window
                    currentWindowIndex = value;
                    if (currentWindow != null) currentWindow.Focus();
                    
                    // If the Window has a TabControl, select the minimum index
                    tabItemDictionary = null;
                    if ((currentWindow.Content != null) && (currentWindow.Content.GetType() == typeof(TabControl))
                        && (tabItemDictionaryLookup.TryGetValue((TabControl)(currentWindow.Content), out tabItemDictionary)))
                    {
                        ((TabControl)currentWindow.Content).Focus();
                        CurrentTabItemIndex = tabItemDictionary.Keys.Min();
                    }
                    
                    // If the Window or TabItem contains a Grid of plots, select the minimum index
                    plotDictionary = null;
                    if ((currentWindow.Content != null) && (currentWindow.Content.GetType() == typeof(Grid))
                        && (plotDictionaryLookup.TryGetValue((Grid)(currentWindow.Content), out plotDictionary)))
                    {
                        currentGrid = (Grid)(currentWindow.Content);
                        CurrentPlotIndex = plotDictionary.Keys.Min();
                    }
                    if ((currentTabItemIndex != null) && (CurrentTabItem.Content != null) && (currentTabItem.Content.GetType() == typeof(Grid))
                        && (plotDictionaryLookup.TryGetValue((Grid)(currentTabItem.Content), out plotDictionary)))
                    {
                        currentGrid = (Grid)(CurrentTabItem.Content);
                        CurrentPlotIndex = plotDictionary.Keys.Min();
                    }
                    
                    // If the Window or TabItem contains a plot, set this to be the current plot
                    if ((currentWindow.Content != null) && (currentWindow.Content.GetType() == typeof(Plot2D)))
                    {
                        currentPlot = (Plot2D)(currentWindow.Content);
                        currentPlotIndex = null;
                    }
                    else if ((currentTabItemIndex != null) && (currentTabItem.Content != null) && (currentTabItem.Content.GetType() == typeof(Plot2D)))
                    {
                        currentPlot = (Plot2D)(currentTabItem.Content);
                        currentPlotIndex = null;
                    }
                }
                else
                {
                    Window newWindow = new Window() { Width = 640, Height = 480, Background = Brushes.White };
                    newWindow.Closed += new EventHandler(window_Closed);
                    newWindow.Title = "Plot Window " + value.ToString();
                    newWindow.Show();
                    windowDictionary.Add(value, newWindow);
                    currentWindow = newWindow;
                    currentWindowIndex = value;
                    currentWindow.Focus();
                    currentWindow.BringIntoView();
                    currentTabItem = null; currentTabItemIndex = null;
                    currentGrid = null; 
                    tabItemDictionary = null; plotDictionary = null;
                }
            }
        }

        public static void OpenNextWindow()
        {
            if (PlotContext.windowDictionary.Keys.Max() == null) CurrentWindowIndex = 0;
            else
            {
                // find first unassigned integer
                int index = 0;
                while (windowDictionary.Keys.Contains(index)) index++;
                CurrentWindowIndex = index;
            }
        }

        private static void window_Closed(Object sender, EventArgs e)
        {
            var entry = windowDictionary.Single(t => t.Value == (Window)sender);
            windowDictionary.Remove(entry.Key);
            plotDictionaryLookup.Remove(entry.Value);
            Window windowToRemove = (Window)sender;
            if ((windowToRemove.Content != null) && (windowToRemove.Content.GetType() == typeof(TabControl)))
            {
                tabItemDictionaryLookup.Remove((TabControl)(windowToRemove.Content));
                plotDictionaryLookup.Remove((TabControl)(windowToRemove.Content));
            }
            plotDictionary = null; tabItemDictionary = null;
            if (PlotContext.currentWindow == (Window)sender)
            {
                PlotContext.currentWindow = null;
                PlotContext.currentWindowIndex = null;
                PlotContext.currentTabItem = null;
                PlotContext.currentTabItemIndex = null;
                PlotContext.currentGrid = null;
                PlotContext.currentPlot = null;
            }
        }

        static public int? CurrentTabItemIndex
        {
            get { return currentTabItemIndex; }
            set
            {
                if (currentWindow != null)
                {
                    currentWindow.Focus();
                    tabItemDictionary = null;
                    // Ensure a TabControl exists
                    if ((currentWindow.Content == null) || (currentWindow.Content.GetType() != typeof(TabControl))
                        || !(tabItemDictionaryLookup.TryGetValue((TabControl)(currentWindow.Content), out tabItemDictionary)))
                    {
                        TabControl newTabControl = new TabControl();
                        newTabControl.Background = Brushes.White;
                        currentWindow.Content = newTabControl;
                        // Create Dictionary to map int to TabItem for the new Control
                        tabItemDictionary = new Dictionary<int?, TabItem>();
                        tabItemDictionaryLookup.Add(newTabControl, tabItemDictionary);
                    }
                    TabControl currentTabControl = (TabControl)(currentWindow.Content);
                    tabItemDictionary = tabItemDictionaryLookup[currentTabControl];
                    currentTabItem = null;

                    if (value == null || tabItemDictionary.TryGetValue(value, out currentTabItem))
                    {
                        // TabItem exists
                        currentTabItemIndex = value;
                        if (currentTabItem != null) currentTabItem.Focus();

                        // If the TabItem contains a Grid of plots, select the minimum index
                        plotDictionary = null;
                        if (currentTabItem != null && CurrentTabItem.Content != null && CurrentTabItem.Content.GetType() == typeof(Grid)
                            && plotDictionaryLookup.TryGetValue((Grid)(CurrentTabItem.Content), out plotDictionary))
                        {
                            currentGrid = (Grid)(CurrentTabItem.Content);
                            CurrentPlotIndex = plotDictionary.Keys.Min();
                        }

                        // If the TabItem contains a plot, set this to be the current plot
                        if (currentTabItem != null && CurrentTabItem.Content != null && CurrentTabItem.Content.GetType() == typeof(Plot2D))
                        {
                            currentPlot = (Plot2D)(currentTabItem.Content);
                            currentPlotIndex = null;
                        }

                    }
                    else
                    {
                        // Create new TabItem
                        currentTabItem = new TabItem();
                        currentTabItem.Header = "Tab " + value.ToString();
                        currentTabControl.Items.Add(currentTabItem);
                        tabItemDictionary.Add(value, currentTabItem);
                        currentTabItemIndex = value;
                        currentTabItem.Focus();
                        currentGrid = null; 
                        plotDictionary = null;
                    }
                }
            }
        }

        static public int? CurrentPlotIndex
        {
            get { return currentPlotIndex; }
            set
            {
                if (currentGrid == null)
                {
                    throw new Exception("No Grid or GridSplitter element present");
                }
                else
                {
                    int total = ((Grid)currentGrid).ColumnDefinitions.Count * ((Grid)currentGrid).RowDefinitions.Count;
                    if (value < 0 || value > total - 1)
                    {
                        throw new Exception("Index out of range");
                    }
                    currentPlotIndex = value;
                    FrameworkElement plot = null;
                    if (plotDictionary != null)
                    {
                        if (plotDictionary.TryGetValue(currentPlotIndex, out plot)) currentPlot = (Plot2D)plot;
                    }
                }
            }
        }

        /// <summary>
        /// Add a subplot (Grid) to the current Window or TabItem 
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        static public void AddSubPlot(int rows, int columns)
        {
            Grid newGrid = new Grid();
            ColumnDefinition col;
            RowDefinition row;
            for (int i = 0; i < rows; ++i)
            {
                row = new RowDefinition(); row.Height = new GridLength(1, GridUnitType.Star);
                newGrid.RowDefinitions.Add(row);
            }
            for (int i = 0; i < columns; ++i)
            {
                col = new ColumnDefinition(); col.Width = new GridLength(1, GridUnitType.Star);
                newGrid.ColumnDefinitions.Add(col);
            }
            Dictionary<int?, FrameworkElement> newPlotDictionary = new Dictionary<int?, FrameworkElement>(); ;
            if (currentTabItem != null)
            {
                currentTabItem.Content = newGrid;
                PlotContext.plotDictionaryLookup.Remove(currentTabItem);
                
            }
            else if (currentWindow != null)
            {
                currentWindow.Content = newGrid;
                PlotContext.plotDictionaryLookup.Remove(currentWindow);
            }
            else
            {
                OpenNextWindow();
                currentWindow.Content = newGrid;
            }
            PlotContext.plotDictionaryLookup.Add(currentWindow, newPlotDictionary);
            PlotContext.plotDictionary = newPlotDictionary;
            PlotContext.currentGrid = newGrid;
            PlotContext.currentPlotIndex = 0;
        }

        static public void AddPlot(FrameworkElement plot)
        {
            // If there is a currentGrid then add to this
            if (currentGrid != null)
            {
                FrameworkElement oldPlot = null;
                if (plotDictionary.TryGetValue(currentPlotIndex, out oldPlot))
                {
                    ((Grid)currentGrid).Children.Remove(oldPlot);
                    plotDictionary.Remove(currentPlotIndex);
                }
                ((Grid)currentGrid).Children.Add(plot);
                plotDictionary.Add(currentPlotIndex, plot);
                int columns = ((Grid)currentGrid).ColumnDefinitions.Count;
                plot.SetValue(Grid.ColumnProperty, currentPlotIndex % columns);
                plot.SetValue(Grid.RowProperty, currentPlotIndex / columns);
            }
            // otherwise, if there is a currentTabItem then add to this
            else if (currentTabItem != null)
            {
                currentTabItem.Content = plot;
            }
            // otherwise, if there is a currentWindow then add to this
            else if (currentWindow != null)
            {
                currentWindow.Content = plot;
            }
            currentPlot = plot;
        }

        //GridSplitter 
    }
}
