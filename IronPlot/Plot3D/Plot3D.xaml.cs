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
using System.Windows.Interop;
using System.Reflection;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Interaction logic for Plot3D.xaml
    /// </summary>
    public partial class Plot3D : UserControl
    {
        #region DependencyProperties
        public static readonly DependencyProperty ProjectionTypeProperty =
            DependencyProperty.Register("ProjectionType",
            typeof(ProjectionType), typeof(Plot3D),
            new FrameworkPropertyMetadata(ProjectionType.Perspective));

        public ProjectionType ProjectionType
        {
            set { SetValue(ProjectionTypeProperty, value); }
            get { return (ProjectionType)GetValue(ProjectionTypeProperty); }
        }
        #endregion

        public Plot3D()
        {
            InitializeComponent();
            AddContextMenu();
        }

        public IronPlot.Plotting3D.Viewport3D Viewport3D
        {
            get { return viewport3D; }
        }

        protected void AddContextMenu()
        {
            ContextMenu mainMenu = new ContextMenu();

            MenuItem item1 = new MenuItem();
            item1.Header = "Copy to Clipboard";
            mainMenu.Items.Add(item1);

            //MenuItem item2 = new MenuItem();
            //item2.Header = "Print...";
            //mainMenu.Items.Add(item2);

            MenuItem item1a = new MenuItem();
            item1a.Header = "96 dpi";
            item1.Items.Add(item1a);
            item1a.Click += OnClipboardCopy_96dpi;

            MenuItem item1b = new MenuItem();
            item1b.Header = "192 dpi";
            item1.Items.Add(item1b);
            item1b.Click += OnClipboardCopy_192dpi;

            MenuItem item1c = new MenuItem();
            item1c.Header = "288 dpi";
            item1.Items.Add(item1c);
            item1c.Click += OnClipboardCopy_288dpi;

            //MenuItem item2 = new MenuItem();
            //item2.Header = "Print...";
            //mainMenu.Items.Add(item2);
            //item2.Click += InvokePrint;

            this.ContextMenu = mainMenu;
        }

        private Point startPosition;

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            startPosition = e.GetPosition(this);
            base.OnMouseRightButtonUp(e);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            // Only allow Context Menu on a click, not on click and drag.
            Point endPosition = e.GetPosition(this);
            base.OnMouseRightButtonUp(e);
            if ((endPosition.Y - startPosition.Y) != 0) e.Handled = true;
        }

        protected void OnClipboardCopy_96dpi(object sender, EventArgs e)
        {
            ToClipboard(96);
        }

        protected void OnClipboardCopy_192dpi(object sender, EventArgs e)
        {
            ToClipboard(192);
        }

        protected void OnClipboardCopy_288dpi(object sender, EventArgs e)
        {
            ToClipboard(288);
        }

        public void ToClipboard(int dpi)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            VisualBrush sourceBrush = new VisualBrush(Viewport3D);
            double scale = dpi / 96.0;
            double actualWidth = Viewport3D.RenderSize.Width;
            double actualHeight = Viewport3D.RenderSize.Height;
            using (drawingContext)
            {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
            }
            Viewport3D.SetResolution(dpi);
            Viewport3D.InvalidateVisual();
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)(actualWidth * scale), (int)(actualHeight * scale), 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            Clipboard.SetImage(renderBitmap);
            Viewport3D.SetResolution(96);
        }
    }
}
