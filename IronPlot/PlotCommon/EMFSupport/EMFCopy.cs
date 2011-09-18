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
using TextViewerFind;
using System.IO;
using BCDev.XamlToys; // This comes from http://xamltoys.codeplex.com/ (including source)
using System.Windows.Xps.Packaging;
using System.IO.Packaging;
using System.Windows.Xps;
using System.Globalization;
using System.Windows.Markup;
using System.Xml;
using System.Windows.Interop;
using WpfToWmfClipboard;

namespace IronPlot
{
    public class EMFCopy
    {
        public static void CopyVisualToWmfClipboard(Visual visual, Window clipboardOwnerWindow)
        {
            CopyXAMLStreamToWmfClipBoard(visual, clipboardOwnerWindow);
            return;
        }

        public static object LoadXamlFromStream(Stream stream)
        {
            using (Stream s = stream)
                return XamlReader.Load(s);
        }

        public static System.Drawing.Graphics CreateEmf(Stream wmfStream, Rect bounds)
        {
            if (bounds.Width == 0 || bounds.Height == 0) bounds = new Rect(0, 0, 1, 1);
            using (System.Drawing.Graphics refDC = System.Drawing.Graphics.FromImage(new System.Drawing.Bitmap(1, 1)))
            {
                System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(new System.Drawing.Imaging.Metafile(wmfStream, refDC.GetHdc(), bounds.ToGdiPlus(), System.Drawing.Imaging.MetafileFrameUnit.Pixel, System.Drawing.Imaging.EmfType.EmfPlusDual));
                return graphics;
            }
        }

        public static T GetDependencyObjectFromVisualTree<T>(DependencyObject startObject)
            // don't restrict to DependencyObject items, to allow retrieval of interfaces
            //where T : DependencyObject
            where T : class
        {
            //Walk the visual tree to get the parent(ItemsControl) 
            //of this control
            DependencyObject parent = startObject;
            while (parent != null)
            {
                T pt = parent as T;
                if (pt != null)
                    return pt;
                else
                    parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private static void CopyXAMLStreamToWmfClipBoard(Visual visual, Window clipboardOwnerWindow)
        {
            // http://xamltoys.codeplex.com/
            var drawing = Utility.GetDrawingFromXaml(visual);

            var bounds = drawing.Bounds;
            Console.WriteLine("Drawing Bounds: {0}", bounds);

            MemoryStream wmfStream = new MemoryStream();

            using (var g = CreateEmf(wmfStream, bounds))
                Utility.RenderDrawingToGraphics(drawing, g);

            wmfStream.Position = 0;

            System.Drawing.Imaging.Metafile metafile = new System.Drawing.Imaging.Metafile(wmfStream);

            IntPtr hEMF, hEMF2;
            hEMF = metafile.GetHenhmetafile(); // invalidates mf
            if (!hEMF.Equals(new IntPtr(0)))
            {
                hEMF2 = NativeMethods.CopyEnhMetaFile(hEMF, new IntPtr(0));
                if (!hEMF2.Equals(new IntPtr(0)))
                {
                    if (NativeMethods.OpenClipboard(((IWin32Window)clipboardOwnerWindow.OwnerAsWin32()).Handle))
                    {
                        if (NativeMethods.EmptyClipboard())
                        {
                            NativeMethods.SetClipboardData(14 /*CF_ENHMETAFILE*/, hEMF2);
                            NativeMethods.CloseClipboard();
                        }
                    }
                }
                NativeMethods.DeleteEnhMetaFile(hEMF);
            }
        }
    }
}
