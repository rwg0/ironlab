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
using System.Threading;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{
    /// <summary>
    /// Interaction logic for ColourBar.xaml
    /// </summary>
    public partial class ColourBar : UserControl
    {
        internal ColourBarPanel colourBarPanel;
        internal ColourMap colourMap;
        private double[] interpolationPoints;
        private DispatcherTimer colourMapUpdateTimer;
        private bool updateInProgress = false;
        private FalseColourImage image;
        private List<Slider> sliderList;

        internal double Min
        {
            set
            {
                colourBarPanel.Axes.YAxes[0].Min = value;
                Rect newBounds = image.Bounds;
                image.Bounds = new Rect(newBounds.Left, value, newBounds.Width, newBounds.Bottom - value);
            }
            get { return colourBarPanel.Axes.YAxes[0].Min; }
        }

        internal double Max
        {
            set
            {
                colourBarPanel.Axes.YAxes[0].Max = value;
                Rect newBounds = image.Bounds;
                image.Bounds = new Rect(newBounds.Left, newBounds.Top, newBounds.Width, value - newBounds.Top);
            }
            get { return colourBarPanel.Axes.YAxes[0].Max; }
        }

        public static readonly RoutedEvent ColourMapChangedEvent =
            EventManager.RegisterRoutedEvent("ColourMapChangedEvent", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(ColourBar));

        public event RoutedEventHandler ColourMapChanged
        {
            add { AddHandler(ColourMapChangedEvent, value); }
            remove { RemoveHandler(ColourMapChangedEvent, value); }
        }

        void RaiseColourMapChangedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(ColourBar.ColourMapChangedEvent);
            RaiseEvent(newEventArgs);
        }

        public ColourBar(ColourMap colourMap)
        {
            InitializeComponent();
            this.colourMap = colourMap;
            colourBarPanel = new ColourBarPanel();
            image = new FalseColourImage(new Rect(0, Min, 1, Max), MathHelper.Counter(1, colourMap.Length), false);
            colourBarPanel.plotItems.Add(image);
            image.ColourMap = colourMap;
            colourBarPanel.Margin = new Thickness(0, 0, 5, 0);
            this.grid.Children.Add(colourBarPanel);
            colourMapUpdateTimer = new DispatcherTimer();
            colourMapUpdateTimer.Interval = new TimeSpan(1000); // 1/10 s
            colourMapUpdateTimer.Tick += OnColourMapUpdateTimerElapsed;
            AddSliders();
            AddContextMenu();
            FocusVisualStyle = null;
        }

        protected void AddSliders()
        {
            interpolationPoints = (double[])colourMap.InterpolationPoints.Clone();
            sliderList = new List<Slider>();
            byte[,] colourMapArray = colourMap.ToByteArray();
            colourBarPanel.RemoveSliders();
            for (int i = 1; i < colourMap.InterpolationPoints.Length - 1; ++i)
            {
                Slider slider = new Slider();
                slider.Orientation = Orientation.Vertical;
                slider.VerticalAlignment = VerticalAlignment.Stretch;
                slider.Minimum = 0.0;                
                slider.Maximum = 1.0;
                slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider_ValueChanged);
                sliderList.Add(slider);
                slider.Value = colourMap.InterpolationPoints[i];
                int index = (int)(slider.Value * (double)(colourMap.Length - 1));
                slider.Foreground = new SolidColorBrush
                    (Color.FromRgb(colourMapArray[index, 1], colourMapArray[index, 2], colourMapArray[index, 3]));
                slider.Template = (ControlTemplate)(this.Resources["colourBarVerticalSlider"]);
            }
            colourBarPanel.AddSliders(sliderList);
        }

        protected void slider_ValueChanged(object obj, RoutedPropertyChangedEventArgs<double> args)
        {
            int index = sliderList.IndexOf((Slider)obj);
            if (index < (sliderList.Count - 1))
            {
                if (args.NewValue > sliderList[index + 1].Value)
                {
                    ((Slider)obj).Value = sliderList[index + 1].Value;
                    args.Handled = true;
                }
            }
            if (index > 0)
            {
                if (args.NewValue < sliderList[index - 1].Value)
                {
                    ((Slider)obj).Value = sliderList[index - 1].Value;
                    args.Handled = true;
                }
            }
            interpolationPoints.SetValue(((Slider)obj).Value, index + 1);
            colourMapUpdateTimer.Start();
            RaiseColourMapChangedEvent();
        }

        private void OnColourMapUpdateTimerElapsed(object sender, EventArgs e)
        {
            if (updateInProgress)
            {
                colourMapUpdateTimer.Start();
                return;
            }
            colourMapUpdateTimer.Stop();
            updateInProgress = true;
            object state = new object();
            ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateColourMapAndBar), (object)state);
        }

        private void UpdateColourMapAndBar(Object state)    
        {
            int i = 0;
            foreach (double value in interpolationPoints)
            {
                colourMap.InterpolationPoints[i] = value;
                ++i;
            }
            colourMap.UpdateColourMap();
            image.OnColourMapChanged(null, new RoutedEventArgs());
            updateInProgress = false;
        }

        protected void AddContextMenu()
        {
            ContextMenu mainMenu = new ContextMenu();

            MenuItem item1 = new MenuItem();
            item1.Header = "Colourmap";
            mainMenu.Items.Add(item1);

            //MenuItem item2 = new MenuItem();
            //item2.Header = "Print...";
            //mainMenu.Items.Add(item2);

            MenuItem item1a = new MenuItem();
            item1a.Header = "Jet";
            item1.Items.Add(item1a);
            item1a.Click += Jet;

            MenuItem item1b = new MenuItem();
            item1b.Header = "HSV";
            item1.Items.Add(item1b);
            item1b.Click += HSV;

            MenuItem item1c = new MenuItem();
            item1c.Header = "Gray";
            item1.Items.Add(item1c);
            item1c.Click += Gray;

            MenuItem item2 = new MenuItem();
            item2.Header = "Show/Hide handles";
            mainMenu.Items.Add(item2);
            item2.Click += ShowHideHandles;

            this.ContextMenu = mainMenu;
        }

        private void ShowHideHandles(object sender, EventArgs args)
        {
            foreach (Slider slider in colourBarPanel.sliderList)
            {
                if (slider.Visibility == Visibility.Visible) slider.Visibility = Visibility.Collapsed;
                else slider.Visibility = Visibility.Visible;
            }
            colourBarPanel.InvalidateMeasure();
        }

        private void Gray(object sender, EventArgs args)
        {
            colourMap.Gray();
            ResetHandles();
        }

        private void Jet(object sender, EventArgs args)
        {
            colourMap.Jet();
            ResetHandles();
        }

        private void HSV(object sender, EventArgs args)
        {
            colourMap.HSV();
            ResetHandles();
        }

        private void ResetHandles()
        {
            colourMap.UpdateColourMap();
            AddSliders();
            colourMapUpdateTimer.Start();
            RaiseColourMapChangedEvent();
            colourBarPanel.InvalidateMeasure();
        }
    }
}
