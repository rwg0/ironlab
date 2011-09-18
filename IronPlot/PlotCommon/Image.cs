// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot
{    
    public static class Image
    {
#if ILNumerics
        public static ILCell LoadImage(string path)
        {
            BitmapSource bitmapSource = new BitmapImage(new Uri(System.IO.Path.GetFullPath(path)));
            if (bitmapSource.Format != PixelFormats.Bgra32)
            {
                bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0);

            }
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int bytes = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            int stride = (width * bytes);
            //Stream stream = (bitmapSource as BitmapImage).StreamSource;
            //BinaryReader br = new BinaryReader(stream);
            byte[] ba = new byte[width * height * bytes];
            bitmapSource.CopyPixels(ba, stride, 0);
            ILArray<double> blue = new ILArray<double>(width, height);
            ILArray<double> green = new ILArray<double>(width, height);
            ILArray<double> red = new ILArray<double>(width, height);
            ILArray<double> alpha = new ILArray<double>(width, height);
            double[] b = blue.InternalArray4Experts;
            double[] g = green.InternalArray4Experts;
            double[] r = red.InternalArray4Experts;
            double[] a = alpha.InternalArray4Experts;
            for (int i = 0, j = 0; i < width * height * bytes; i += bytes)
            {
                b[j] = ba[i];
                g[j] = ba[i + 1];
                r[j] = ba[i + 2];
                a[j] = ba[i + 3];
                ++j;
                //b[i] = br.ReadByte();
                //g[i] = br.ReadByte();
                //r[i] = br.ReadByte();
                //a[i] = br.ReadByte();
            }
            ILCell cell = new ILCell(4); 
            cell[0] = blue; cell[1] = green; cell[2] = red; cell[3] = alpha;
            return cell;
        }

        public static BitmapSource ILArrayToBitmapSource(ILArray<double> surface)
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int width = surface.Dimensions[0];
            int height = surface.Dimensions[1];
            int bytes = (pf.BitsPerPixel + 7) / 8;
            int rawStride = (width * bytes);
            byte[] rawImage = new byte[rawStride * height];
            int index = 0;
            ColourMap ColourMap = new ColourMap(ColourMapType.Jet, 256);
            byte[,] cmap = ColourMap.ToByteArray();
            double range = surface.MaxValue - surface.MinValue;
            double min = surface.MinValue;
            int magnitude = 0;
            ILArray<int> scaled = (ILArray<int>)ILMath.convert(NumericType.Int32,ILMath.floor((surface - min) * 256.0 / range));
            ILIterator<int> iterator = scaled.CreateIterator();
            Stopwatch sw = Stopwatch.StartNew();
            sw.Reset();
            sw.Start();
            magnitude = iterator.Value;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (magnitude == 256) { magnitude = 255; }
                    rawImage[index] = cmap[magnitude, 3];
                    rawImage[index + 1] = cmap[magnitude, 2];
                    rawImage[index + 2] = cmap[magnitude, 1];
                    rawImage[index + 3] = cmap[magnitude, 0];
                    index += bytes;
                    magnitude = iterator.Increment();
                }
            }
            sw.Stop();
            string result;
            result = "Elapsed time: " + sw.ElapsedMilliseconds.ToString() + " ms";
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }
#endif
    }
}
