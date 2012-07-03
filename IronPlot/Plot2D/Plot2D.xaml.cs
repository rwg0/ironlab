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
using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Collections.ObjectModel;

namespace IronPlot
{
    /// <summary>
    /// Interaction logic for Plot.xaml
    /// </summary>
    public partial class Plot2D : UserControl
    {
        public Plot2D()
        {
            InitializeComponent();
            children = new ObservableCollection<Plot2DItem>();
            AddContextMenu();
            // Adapter used for templating; actually not needed for UserControl implementation.
            childrenAdapter = new ObservableCollectionListAdapter<Plot2DItem>();
            childrenAdapter.Collection = children;
            childrenAdapter.TargetList = PlotPanel.PlotItems;
            childrenAdapter.Populate();
        }

        private ObservableCollection<Plot2DItem> children;
        private ObservableCollectionListAdapter<Plot2DItem> childrenAdapter;

        public bool EqualAxes
        {
            set
            {
                PlotPanel.SetValue(PlotPanel.EqualAxesProperty, value);
            }
            get { return (bool)PlotPanel.GetValue(PlotPanel.EqualAxesProperty); }
        }

        public bool UseDirect2D
        {
            set
            {
                PlotPanel.SetValue(PlotPanel.UseDirect2DProperty, value);
            }
            get { return (bool)PlotPanel.GetValue(PlotPanel.UseDirect2DProperty); }
        }

        public Axes2D Axes
        {
            get { return PlotPanel.Axes; }
        }

        public Position LegendPosition
        {
            set { PlotPanel.SetPosition(Legend, value); }
            get { return PlotPanel.GetPosition(Legend); }
        }

        public Brush BackgroundPlotSurround
        {
            get { return PlotPanel.Background; }
            set { PlotPanel.Background = value; }
        }

        public Brush BackgroundPlot
        {
            get { return PlotPanel.BackgroundCanvas.Background; }
            set { PlotPanel.BackgroundCanvas.Background = value; }
        }

        protected void CommonConstructor()
        {
            children = new ObservableCollection<Plot2DItem>();
            AddContextMenu();
            childrenAdapter = new ObservableCollectionListAdapter<Plot2DItem>();
        }

        // To add templating; alternate to UserControl.
        //public override void OnApplyTemplate()
        //{
        //    base.OnApplyTemplate();
        //    plotPanel = GetTemplateChild("PlotPanel") as PlotPanel;
        //    childrenAdapter.Collection = children;
        //    childrenAdapter.TargetList = plotPanel.PlotItems;
        //    childrenAdapter.Populate();
        //}

        public Collection<Plot2DItem> Children
        {
            get { return children; }
        }

        protected void AddContextMenu()
        {
            ContextMenu mainMenu = new ContextMenu();

            MenuItem item1 = new MenuItem() { Header = "Copy to Clipboard" };
            mainMenu.Items.Add(item1);

            MenuItem item1a = new MenuItem();
            item1a.Header = "96 dpi";
            item1.Items.Add(item1a);
            item1a.Click += OnClipboardCopy_96dpi;

            MenuItem item1b = new MenuItem();
            item1b.Header = "300 dpi";
            item1.Items.Add(item1b);
            item1b.Click += OnClipboardCopy_300dpi;

            MenuItem item1c = new MenuItem() { Header = "Enhanced Metafile" }; ;
            item1.Items.Add(item1c);
            item1c.Click += CopyToEMF;

            MenuItem item2 = new MenuItem() { Header = "Print..." };
            mainMenu.Items.Add(item2);
            item2.Click += InvokePrint;

            this.ContextMenu = mainMenu;
        }

        protected void OnClipboardCopy_96dpi(object sender, EventArgs e)
        {
            ToClipboard(96);
        }

        protected void OnClipboardCopy_300dpi(object sender, EventArgs e)
        {
            ToClipboard(300);
        }

        protected void CopyToEMF(object sender, EventArgs e)
        {
            try
            {
                bool direct2D = UseDirect2D;
                if (direct2D) UseDirect2D = false;
                EMFCopy.CopyVisualToWmfClipboard((Visual)this, Window.GetWindow(this));
                if (direct2D) UseDirect2D = true;
            }
            catch (Exception)
            {
                // Swallow exception
                //throw new Exception("Writing to enhanced metafile failed for plot.");
            }
        }

        public void ToClipboard(int dpi)
        {
            bool direct2D = UseDirect2D;
            if (direct2D) UseDirect2D = false;
            try
            {
                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();
                this.UpdateLayout();
                VisualBrush sourceBrush = new VisualBrush(this);
                double scale = dpi / 96.0;
                double actualWidth = this.RenderSize.Width;
                double actualHeight = this.RenderSize.Height;
                using (drawingContext)
                {
                    drawingContext.PushTransform(new ScaleTransform(scale, scale));
                    drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
                }
                this.InvalidateVisual();
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)(actualWidth * scale), (int)(actualHeight * scale), 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(drawingVisual);
                Clipboard.SetImage(renderBitmap);
            }
            catch (Exception)
            {
                // Swallow exception
                //throw new Exception("Creating image failed for plot.");
            }
            if (direct2D) UseDirect2D = true;
        }

        private bool? print;
        private PrintDialog printDialog;

        private void InvokePrint(object sender, RoutedEventArgs e)
        {
            printDialog = new PrintDialog();

            print = false;
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                new Action(delegate()
                                {
                                    print = printDialog.ShowDialog();
                                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                        new Action(AfterInvokePrint));
                                }));

        }

        private void AfterInvokePrint()
        {
            if (print == true)
            {
                string filename = System.IO.Path.GetTempPath() + "IronPlotPrint.xps";
                Package package = Package.Open(filename, FileMode.Create);
                XpsDocument xpsDoc = new XpsDocument(package);
                XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                bool direct2D = UseDirect2D;
                if (direct2D) UseDirect2D = false;
                try
                {
                    UpdateLayout();
                    xpsWriter.Write(PlotPanel);
                    xpsDoc.Close();
                    package.Close();
                }
                finally
                {
                    if (direct2D) UseDirect2D = true;
                }
                PrintQueue printQueue = printDialog.PrintQueue;
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        printQueue.AddJob("IronPlot Print", filename, false);
                    }));
            }
        }

        #region ConvenienceMethods

        public Plot2DCurve AddLine(double[] y)
        {
            double[] x = MathHelper.Counter(y.Length);
            Plot2DCurve plot2DCurve = AddLine(x, y);
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(double[] y, string quickLine)
        {
            double[] x = MathHelper.Counter(y.Length);
            Plot2DCurve plot2DCurve = AddLine(x, y);
            plot2DCurve.QuickLine = quickLine;
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(double[] x, double[] y, string quickLine)
        {
            Plot2DCurve plot2DCurve = AddLine(x, y);
            plot2DCurve.QuickLine = quickLine;
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(object x, object y, string quickLine)
        {
            Plot2DCurve plot2DCurve = AddLine(x, y);
            plot2DCurve.QuickLine = quickLine;
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(double[] x, double[] y)
        {
            Curve curve = new Curve(x, y);
            Plot2DCurve plot2DCurve = new Plot2DCurve(curve);
            this.Children.Add(plot2DCurve);
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(object x, object y)
        {
            Curve curve = new Curve(Plotting.Array(x), Plotting.Array(y));
            Plot2DCurve plot2DCurve = new Plot2DCurve(curve);
            this.Children.Add(plot2DCurve);
            return plot2DCurve;
        }

        public FalseColourImage AddFalseColourImage(double[,] image)
        {
            FalseColourImage falseColour = new FalseColourImage(image);
            this.Children.Add(falseColour);
            return falseColour;
        }

        public FalseColourImage AddFalseColourImage(IEnumerable<object> image)
        {
            FalseColourImage falseColour = new FalseColourImage(image);
            this.Children.Add(falseColour);
            return falseColour;
        }

        public FalseColourImage AddFalseColourImage(double[] x, double[] y, double[,] image)
        {
            FalseColourImage falseColour =
                new FalseColourImage(new Rect(new Point(x.Min(), y.Min()), new Point(x.Max(), y.Max())), image, true);
            this.Children.Add(falseColour);
            return falseColour;
        }

        public FalseColourImage AddFalseColourImage(IEnumerable<object> x, IEnumerable<object> y, IEnumerable<object> image)
        {
            int xLength, yLength;
            var xa = GeneralArray.ToImageEnumerator(x, out xLength, out yLength);
            var ya = GeneralArray.ToImageEnumerator(y, out xLength, out yLength);
            FalseColourImage falseColour =
                new FalseColourImage(new Rect(new Point(xa.Min(), ya.Min()), new Point(xa.Max(), ya.Max())), image, true);
            this.Children.Add(falseColour);
            return falseColour;
        }

        #endregion ConvenienceMethods
    }
}
