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
using System.Windows.Forms.Integration;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace IronPlot
{   
    /// <summary>
    /// Interaction logic for WindowsFormsPlotHost.xaml
    /// </summary>
    public partial class MSChartHost : UserControl
    {
        public MSChartHost()
        {
            InitializeComponent();
            AddContextMenu();
            windowsFormsHost.Child = chart;
            chart.MouseDown += new System.Windows.Forms.MouseEventHandler(chart_MouseDown);
        }

        Chart chart = new Chart();
        public Chart Chart { get { return chart; } set { chart = value; } } 

        public void OpenContextMenu(object sender, System.Windows.Forms.MouseEventArgs args)
        {
            if (args.Button == System.Windows.Forms.MouseButtons.Right) windowsFormsHost.ContextMenu.IsOpen = true;
        }

        public void chart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            windowsFormsHost.ContextMenu.IsOpen = true;
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

            //MenuItem item2 = new MenuItem() { Header = "Print..." };
            //mainMenu.Items.Add(item2);
            //item2.Click += InvokePrint;
            windowsFormsHost.ContextMenu = mainMenu;
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
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                chart.SaveImage(stream, ChartImageFormat.Emf);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                Metafile metafile = new Metafile(stream);
                ClipboardMetafileHelper.PutEnhMetafileOnClipboard(chart.Handle, metafile);
            }
        }

        public void ToClipboard(int dpi)
        {
            int width = chart.Width * dpi / 96;
            int height = chart.Height * dpi / 96;

            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                // Suitable for low resolution only:
                //chart.SaveImage(stream, ChartImageFormat.Png);
                //Bitmap bitmap = new Bitmap(stream);
                //System.Windows.Forms.Clipboard.SetDataObject(bitmap);

                chart.SaveImage(stream, ChartImageFormat.Emf);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                Metafile metafile = new Metafile(stream);

                Bitmap bitmap = new Bitmap(width, height);
                Graphics graphics = Graphics.FromImage(bitmap);
                graphics.DrawImage(metafile, 0, 0, width, height);
                System.Windows.Forms.Clipboard.SetDataObject(bitmap);
            }
        }
    }

    public class ClipboardMetafileHelper
    {
        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();
        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("user32.dll")]
        static extern bool CloseClipboard();
        [DllImport("gdi32.dll")]
        static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNULL);
        [DllImport("gdi32.dll")]
        static extern bool DeleteEnhMetaFile(IntPtr hemf);

        // Metafile mf is set to a state that is not valid inside this function.
        static public bool PutEnhMetafileOnClipboard(IntPtr hWnd, Metafile mf)
        {
            bool bResult = false;
            IntPtr hEMF, hEMF2;
            hEMF = mf.GetHenhmetafile(); // invalidates mf
            if (!hEMF.Equals(new IntPtr(0)))
            {
                hEMF2 = CopyEnhMetaFile(hEMF, new IntPtr(0));
                if (!hEMF2.Equals(new IntPtr(0)))
                {
                    if (OpenClipboard(hWnd))
                    {
                        if (EmptyClipboard())
                        {
                            IntPtr hRes = SetClipboardData(14 /*CF_ENHMETAFILE*/, hEMF2);
                            bResult = hRes.Equals(hEMF2);
                            CloseClipboard();
                        }
                    }
                }
                DeleteEnhMetaFile(hEMF);
            }
            return bResult;
        }
    }
}
