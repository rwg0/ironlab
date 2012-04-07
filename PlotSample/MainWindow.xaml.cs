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
using System.Data; 
using IronPlot;
using IronPlot.Plotting3D;

namespace PlotTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Plot2DMultipleAxes();

            DatesPlot();
      
            // Example false colour plot: 
            double[,] falseColour = new double[128, 128];
            for (int i = 0; i < 128; ++i)
                for (int j = 0; j < 128; ++j) falseColour[i, j] = i + j;
            plot2.AddFalseColourImage(falseColour);
            //plot2.Axes.XAxes.GridLines.Visibility = Visibility.Collapsed;
            //plot2.Axes.YAxes.GridLines.Visibility = Visibility.Collapsed;

            // Example surface plot:
            int nx = 96;
            int ny = 96;
            var x2 = MathHelper.MeshGridX(Enumerable.Range(1, nx).Select(t => (double)t - nx/2), ny);
            var y2 = MathHelper.MeshGridY(Enumerable.Range(1, ny).Select(t => (double)t - ny/2), nx);
            var z2 = x2.Zip(y2, (u, v) => Math.Exp((-u*u - v*v) / 400)); // .NET4 method
            //var z2 = x2.Select(u => u * u);
            SurfaceModel3D surface = new SurfaceModel3D(x2, y2, z2, nx, ny);
            surface.Transparency = 5;
            surface.MeshLines = MeshLines.None;
            surface.SurfaceShading = SurfaceShading.Smooth;
            plot3.Viewport3D.Models.Add(surface);
            //plot3.LeftLabel.Text = "Left Label"; plot3.BottomLabel.Text = "Bottom Label";
            // Some events.
            //plot1.Axes.YAxes.Right.MouseEnter += new MouseEventHandler(Bottom_MouseEnter);

            Plot2DCurve curve4 = plot4.AddLine(new double[] { 1.2, 1.3, 2.8, 5.6, 1.9, -5.9 });
            plot4.Axes.Height = plot4.Axes.Width = 500;
            //plot4.Height = plot4.Width = 500;
        }

        void Plot2DMultipleAxes()
        {
            // Example 2D plot:
            // First curve:
            Plot2DCurve curve1 = plotMultipleAxes.AddLine(new double[] { 1.2, 1.3, 2.8, 5.6, 1.9, -5.9 });
            curve1.Stroke = Brushes.Blue; curve1.StrokeThickness = 1.5; curve1.MarkersType = MarkersType.Square;
            // Second curve:
            int nPoints = 2000;
            double[] x = new double[nPoints];
            double[] y = new double[nPoints];
            Random rand = new Random();
            for (int i = 0; i < nPoints; ++i)
            {
                x[i] = i * 10.0 / (double)nPoints;
                y[i] = Math.Sin(x[i]) + 0.1 * Math.Sin(x[i] * 100);
            }
            Plot2DCurve curve2 = new Plot2DCurve(new Curve(x, y)) { QuickLine = "r-" };
            plotMultipleAxes.Children.Add(curve2);
            // Third curve:
            Plot2DCurve curve3 = new Plot2DCurve(new Curve(new double[] { 1, 3, 1.5, 7 }, new double[] { 4.5, 9.0, 3.2, 4.5 })) { StrokeThickness = 3.0, Stroke = Brushes.Green };
            curve3.QuickLine = "o";
            curve3.MarkersFill = Brushes.Blue;
            curve3.Title = "Test3";
            //curve3.QuickStrokeDash = QuickStrokeDash.Dash;
            plotMultipleAxes.Children.Add(curve3);
            // Can use Direct2D acceleration, but requires DirectX10 (Windows 7)
            //plot1.UseDirect2D = true; 
            //plot1.EqualAxes = true;

            // If you want to lose the gradient background:
            //plot1.Legend.Background = Brushes.White;
            //plot1.BackgroundPlot = Brushes.White;

            // Additional labels:
            //plot1.BottomLabel.Text = "Bottom label";
            //plot1.LeftLabel.Text = "Left label";
            plotMultipleAxes.FontSize = 14;
            plotMultipleAxes.Axes.XAxes[0].AxisLabel.Text = "Innermost X Axis";
            plotMultipleAxes.Axes.YAxes[0].AxisLabel.Text = "Innermost Y Axis";
            XAxis xAxisOuter = new XAxis(); YAxis yAxisOuter = new YAxis();
            xAxisOuter.AxisLabel.Text = "Added X Axis";
            yAxisOuter.AxisLabel.Text = "Added Y Axis";
            plotMultipleAxes.Axes.XAxes.Add(xAxisOuter);
            plotMultipleAxes.Axes.YAxes.Add(yAxisOuter);
            yAxisOuter.Position = YAxisPosition.Left;
            plotMultipleAxes.Axes.XAxes[0].FontStyle = plotMultipleAxes.Axes.YAxes[0].FontStyle = FontStyles.Oblique;
            //curve3.XAxis = xAxisOuter;
            curve3.YAxis = yAxisOuter;
            // Other alyout options to try:
            //plot1.Axes.EqualAxes = new AxisPair(plot1.Axes.XAxes.Bottom, plot1.Axes.YAxes.Left);
            //plot1.Axes.SetAxesEqual();
            //plot1.Axes.Height = 100;
            //plot1.Axes.Width = 500;
            //plot1.Axes.MinAxisMargin = new Thickness(200, 0, 0, 0);
            plotMultipleAxes.Axes.XAxes.Top.TickLength = 5;
            plotMultipleAxes.Axes.YAxes.Left.TickLength = 5;

            xAxisOuter.Min = 6.5e-5;
            xAxisOuter.Max = 7.3e-3;
            xAxisOuter.AxisType = AxisType.Log;
            plotMultipleAxes.Children.Add(new Plot2DCurve(new Curve(new double[] { 0.01, 10 }, new double[] { 5, 6 })) { XAxis = xAxisOuter });
            //plotMultipleAxes.UseDirect2D = true;
        }

        void DatesPlot()
        {
            DateTime start = new DateTime(2006, 4, 26);
            DateTime[] dates = Enumerable.Range(0, 10000).Select(t => start.AddMonths(t)).ToArray();
            Random random = new Random();
            double[] values = Enumerable.Range(0, 10000).Select(t => random.NextDouble()).ToArray();
            var curve = datesPlot.AddLine(dates, values);
            curve.XAxis.AxisType = AxisType.Date;
            datesPlot.UseDirect2D = true;
        }
    }
}
