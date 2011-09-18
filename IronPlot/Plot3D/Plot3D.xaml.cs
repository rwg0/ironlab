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

        public void ToClipboard(int dpi)
        {
            int width = (int)(this.ActualWidth * (double)dpi / 96.0);
            int height = (int)(this.ActualHeight * (double)dpi / 96.0);

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            // All WPF elements will take care of themselves when the RenderTargetBitmap Render method is called.
            // However, we need to tell the DirectX components that they need to re-render for the different 
            // resolution (this includes scaling of lines etc: resolution-dependent items)
            Viewport3D.SetResolution(dpi);
            Viewport3D.InvalidateVisual();
            
            // Interesting alternative:
            //BitmapSource source
            //MethodInfo method = image.GetType().GetMethod("CopyBackBuffer", BindingFlags.Instance | BindingFlags.NonPublic);
            //BitmapSource source = (BitmapSource)method.Invoke(image, null);
           
            renderBitmap.Render(Viewport3D);
            Clipboard.SetImage(renderBitmap);

            Viewport3D.SetResolution(96);
        }
    }
}
