// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using IronPlot;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{
    public enum ColourMapMode { RGB, HSV }

    public enum ColourMapType { Jet, HSV, Gray }

    public enum ColorBarOrientation { Horizontal, Vertical };

    public class ColourMap
    {
        private int colorMapLength = 64;
        private byte[,] colourMap;
        private int[] intColourMap;
        private object locker = new object();
        private double[] interpPoints;
        private Curve red, blue, green;
        private Curve hue, saturation, value;
        private ColourMapMode colourMapMode;
        private int alphaValue = 255;
        private ColourMapType colourMapType;
    
        public ColourMap(ColourMapType colourMapType, int length)
        {
            this.colourMapType = colourMapType;
            colorMapLength = length;
            switch (colourMapType)
            {
                case ColourMapType.Jet:
                    Jet();
                    break;
                case ColourMapType.HSV:
                    HSV();
                    break;
                case ColourMapType.Gray:
                    Gray();
                    break;
                default:
                    Jet();
                    break;
            }
            UpdateColourMap();
        }

        public ColourMap(int length)
        {
            this.colourMapType = ColourMapType.Jet;
            colorMapLength = length;
            Jet();
            UpdateColourMap();
        }

        public int Length
        {
            get { return colorMapLength; }
        }

        public double[] InterpolationPoints
        {
            get{ return interpPoints; }
        }

        public byte[,] ToByteArray()
        {
            return colourMap;
        }

        public int[] ToIntArray()
        {
            lock (locker)
            {
                return intColourMap;
            }
        }

        // If either the colourMapType or the colorMapLength or the interpPoints
        // have changed, then recalculate the colourMap
        public void UpdateColourMap()
        {
            lock (locker)
            {
                double[] pixelPositions = MathHelper.Counter(colorMapLength);
                for (int i = 0; i < pixelPositions.Length; ++i) pixelPositions[i] = (pixelPositions[i] - 0.5) / (double)colorMapLength;
                switch (colourMapMode)
                {
                    case ColourMapMode.RGB:
                        red.UpdateLinearSplineCoefficients();
                        green.UpdateLinearSplineCoefficients();
                        blue.UpdateLinearSplineCoefficients();
                        double[] interpolatedRed = red.GetValuesLinear(pixelPositions);
                        interpolatedRed.MultiplyBy(255.0);
                        double[] interpolatedGreen = green.GetValuesLinear(pixelPositions);
                        interpolatedGreen.MultiplyBy(255.0);
                        double[] interpolatedBlue = blue.GetValuesLinear(pixelPositions);
                        interpolatedBlue.MultiplyBy(255.0);
                        colourMap = new byte[colorMapLength, 4];
                        intColourMap = new int[colorMapLength];
                        for (int i = 0; i < colorMapLength; i++)
                        {
                            colourMap[i, 0] = (byte)alphaValue;
                            colourMap[i, 1] = (byte)interpolatedRed[i];
                            colourMap[i, 2] = (byte)interpolatedGreen[i];
                            colourMap[i, 3] = (byte)interpolatedBlue[i];
                            intColourMap[i] = ((int)interpolatedRed[i] << 16) // R
                                    | ((int)interpolatedGreen[i] << 8)     // G
                                    | ((int)interpolatedBlue[i] << 0);     // B
                        }
                        break;
                    case ColourMapMode.HSV:
                        hue.UpdateLinearSplineCoefficients();
                        saturation.UpdateLinearSplineCoefficients();
                        value.UpdateLinearSplineCoefficients();
                        double[] interpolatedHue = hue.GetValuesLinear(pixelPositions);
                        double[] interpolatedSaturation = saturation.GetValuesLinear(pixelPositions);
                        double[] interpolatedValue = value.GetValuesLinear(pixelPositions);
                        colourMap = new byte[colorMapLength, 4];
                        intColourMap = new int[colorMapLength];
                        double r = 0, g = 0, b = 0;
                        for (int i = 0; i < colorMapLength; i++)
                        {
                            int hi = (int)(Math.Floor(interpolatedHue[i] * 6));
                            double f = interpolatedHue[i] * 6 - (double)hi;
                            double v = interpolatedSaturation[i];
                            double s = interpolatedValue[i];
                            double p = v * (1 - s);
                            double q = v * (1 - f * s);
                            double t = v * (1 - (1 - f) * s);
                            switch (hi)
                            {
                                case 0:
                                    r = v; g = t; b = p; break;
                                case 1:
                                    r = q; g = v; b = p; break;
                                case 2:
                                    r = p; g = v; b = t; break;
                                case 3:
                                    r = p; g = q; b = v; break;
                                case 4:
                                    r = t; g = p; b = v; break;
                                case 5:
                                    r = v; g = p; b = q; break;
                            }
                            r *= 255; g *= 255; b *= 255;
                            colourMap[i, 0] = (byte)alphaValue;
                            colourMap[i, 1] = (byte)r;
                            colourMap[i, 2] = (byte)g;
                            colourMap[i, 3] = (byte)b;
                            intColourMap[i] = ((int)r << 16) // H
                                    | ((int)g << 8)     // S
                                    | ((int)b << 0);     // V
                        }
                        break;
                }
            }
        }

        public int[,] Spring()
        {
            int[,] cmap = new int[colorMapLength, 4];
            float[] spring = new float[colorMapLength];
            for (int i = 0; i < colorMapLength; i++)
            {
                spring[i] = 1.0f * i / (colorMapLength - 1);
                cmap[i, 0] = alphaValue;
                cmap[i, 1] = 255;
                cmap[i, 2] = (int)(255 * spring[i]);
                cmap[i, 3] = 255 - cmap[i, 1];
            }
            return cmap;
        }

        public int[,] Summer()
        {
            int[,] cmap = new int[colorMapLength, 4];
            float[] summer = new float[colorMapLength];
            for (int i = 0; i < colorMapLength; i++)
            {
                summer[i] = 1.0f * i / (colorMapLength - 1);
                cmap[i, 0] = alphaValue;
                cmap[i, 1] = (int)(255 * summer[i]);
                cmap[i, 2] = (int)(255 * 0.5f * (1 + summer[i]));
                cmap[i, 3] = (int)(255 * 0.4f);
            }
            return cmap;
        }

        public int[,] Autumn()
        {
            int[,] cmap = new int[colorMapLength, 4];
            float[] autumn = new float[colorMapLength];
            for (int i = 0; i < colorMapLength; i++)
            {
                autumn[i] = 1.0f * i / (colorMapLength - 1);
                cmap[i, 0] = alphaValue;
                cmap[i, 1] = 255;
                cmap[i, 2] = (int)(255 * autumn[i]);
                cmap[i, 3] = 0;
            }
            return cmap;
        }

        public int[,] Winter()
        {
            int[,] cmap = new int[colorMapLength, 4];
            float[] winter = new float[colorMapLength];
            for (int i = 0; i < colorMapLength; i++)
            {
                winter[i] = 1.0f * i / (colorMapLength - 1);
                cmap[i, 0] = alphaValue;
                cmap[i, 1] = 0;
                cmap[i, 2] = (int)(255 * winter[i]);
                cmap[i, 3] = (int)(255 * (1.0f - 0.5f * winter[i]));
            }
            return cmap;
        }

        public void Gray()
        {
            colourMapMode = ColourMapMode.RGB;
            interpPoints = new double[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 };
            red = new Curve(interpPoints, new double[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 });
            green = new Curve(interpPoints, new double[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 });
            blue = new Curve(interpPoints, new double[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 });
        }

        public void Jet()
        {
            // RGB format used
            // Assume that pixel color is colour at pixel centre
            // 0 Dark blue (0,0,0.5) to blue (0,0,1) 1/8
            // 1 Blue (0,0,1) to cyan (0,1,1) 2/8
            // 2 Cyan (0,1,1) to yellow (1,1,0) 2/8
            // 3 Yellow (1,1,0) to red (1,0,0) 2/8
            // 4 Red (1,0,0) to dark red (0.5,0,0) 1/8
            // Dark Blue, blue, cyan, yellow, red, dark red
            colourMapMode = ColourMapMode.RGB;
            interpPoints = new double[] { 0.0, 0.01, 0.125, 0.375, 0.625, 0.875, 0.99, 1.0 };
            red = new Curve(interpPoints, new double[] { 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.5, 0.5 });
            green = new Curve(interpPoints, new double[] { 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0 });
            blue = new Curve(interpPoints, new double[] { 0.5, 0.5, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0 });
        }

        public void HSV()
        {
            colourMapMode = ColourMapMode.HSV;
            interpPoints = new double[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
            hue = new Curve(interpPoints, new double[] { 0.0, 0.25, 0.5, 0.75, 1.0 });
            saturation = new Curve(interpPoints, new double[] { 1.0, 1.0, 1.0, 1.0, 1.0 });
            value = new Curve(interpPoints, new double[] { 1.0, 1.0, 1.0, 1.0, 1.0 });
        }

        public int[,] Hot()
        {
            int[,] cmap = new int[colorMapLength, 4];
            int n = 3 * colorMapLength / 8;
            float[] red = new float[colorMapLength];
            float[] green = new float[colorMapLength];
            float[] blue = new float[colorMapLength];
            for (int i = 0; i < colorMapLength; i++)
            {
                if (i < n)
                    red[i] = 1.0f * (i + 1) / n;
                else
                    red[i] = 1.0f;
                if (i < n)
                    green[i] = 0f;
                else if (i >= n && i < 2 * n)
                    green[i] = 1.0f * (i + 1 - n) / n;
                else
                    green[i] = 1f;
                if (i < 2 * n)
                    blue[i] = 0f;
                else
                    blue[i] = 1.0f * (i + 1 - 2 * n) / (colorMapLength - 2 * n);
                cmap[i, 0] = alphaValue;
                cmap[i, 1] = (int)(255 * red[i]);
                cmap[i, 2] = (int)(255 * green[i]);
                cmap[i, 3] = (int)(255 * blue[i]);
            }
            return cmap;
        }

        public int[,] Cool()
        {
            int[,] cmap = new int[colorMapLength, 4];
            float[] cool = new float[colorMapLength];
            for (int i = 0; i < colorMapLength; i++)
            {
                cool[i] = 1.0f * i / (colorMapLength - 1);
                cmap[i, 0] = alphaValue;
                cmap[i, 1] = (int)(255 * cool[i]);
                cmap[i, 2] = (int)(255 * (1 - cool[i]));
                cmap[i, 3] = 255;
            }
            return cmap;
        }

        public BitmapSource ToBitmapSource(ColorBarOrientation colorBarOrientation)
        {
            // Define parameters used to create the BitmapSource.
            int width, height;
            if (colorBarOrientation == ColorBarOrientation.Vertical)
            {
                width = 1;
                height = colorMapLength;
            }
            else 
            {
                width = colorMapLength;
                height = 1;
            }
            PixelFormat pf = PixelFormats.Bgr32;
            int bytes = (pf.BitsPerPixel + 7) / 8;
            int rawStride = (width * bytes);
            byte[] rawImage = new byte[rawStride * height];
            byte[,] cmap = ToByteArray();
            int index = 0;
            for (int c = colorMapLength - 1; c > 0; --c)
            {
                rawImage[index] = cmap[c, 3];
                rawImage[index + 1] = cmap[c, 2];
                rawImage[index + 2] = cmap[c, 1];
                rawImage[index + 3] = cmap[c, 0];
                index += bytes;
            }
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }

        public void UpdateWriteableBitmap(WriteableBitmap bitmap)
        {
            bitmap.Lock();
            unsafe
            {
                IntPtr backBuffer = bitmap.BackBuffer;
                int* pBackBuffer = (int*)backBuffer;
                PixelFormat pf = PixelFormats.Bgr32;
                int bytes = (pf.BitsPerPixel + 7) / 8;
                int[] cmap = ToIntArray();
                for (int c = colorMapLength - 1; c > 0; --c)
                {
                    *pBackBuffer = cmap[c];
                    pBackBuffer++;
                }
            }
            bitmap.AddDirtyRect(new Int32Rect(0,0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
        }
    }
}
