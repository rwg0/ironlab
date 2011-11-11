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
//using System.Windows.Xps.Packaging;
//using System.Windows.Xps.Serialization;
using System.Printing;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{    
    public class FalseColourImage : Plot2DItem
    {
        ColourBar colourBar = null;
        IEnumerable<double> underlyingData;

        int width;
        int height;

        UInt16[] indices;
        ColourMap colourMap;
        int[] updateColourMap;
        Path imageRectangle;
        ImageBrush imageBrush;
        WriteableBitmap writeableBitmap;
        // The bounds of the rectangle in graph coordinates
        volatile bool updateInProgress = false;
        DispatcherTimer colourMapUpdateTimer;
        IntPtr backBuffer;
        private delegate void AfterUpdateCallback();

        internal override void BeforeArrange()
        {
            // Ensure that transform is updated with the latest axes values. 
            imageRectangle.RenderTransform = Axis2D.GraphToCanvasLinear(xAxis, yAxis); 
        }

        protected override void OnHostChanged(PlotPanel host)
        {
            base.OnHostChanged(host);
            if (this.host != null)
            {
                try
                {
                    host.canvas.Children.Remove(Rectangle);
                }
                catch (Exception) 
                { 
                    // Just swallow any exception 
                }
            }
            this.host = host;
            //imageRectangle.RenderTransform = host.graphToCanvas;
            if (colourBar != null)
            {
                host.annotationsRight.Children.Add(colourBar);
                colourBar.VerticalAlignment = VerticalAlignment.Stretch;
                colourBar.ColourMapChanged += new RoutedEventHandler(OnColourMapChanged);
            }
            colourMapUpdateTimer.Tick += OnColourMapUpdateTimerElapsed;
            host.canvas.Children.Add(Rectangle);
        }
        
        // a FalseColourImage creates a UInt16[]
        // The UInt16[] contains indexed pixels that are mapped to colours 
        // via the colourMap
        bool useILArray = false;
#if ILNumerics
        ILArray<double> underlyingILArrayData;
#endif


        public Path Rectangle
        {
            get { return imageRectangle; }
        }

        public ColourMap ColourMap
        {
            get { return colourMap; }
            set { 
                colourMap = value;
                colourMapUpdateTimer.Start();    
            }
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register("BoundsProperty",
            typeof(Rect), typeof(FalseColourImage),
            new PropertyMetadata(new Rect(0, 0, 10, 10),
                OnBoundsChanged));

        public Rect Bounds
        {
            set
            {
                SetValue(BoundsProperty, (Rect)value);
            }
            get { return (Rect)GetValue(BoundsProperty); }
        }

        public override Rect TightBounds
        {
            get { return (Rect)GetValue(BoundsProperty); }
        }

        public override Rect PaddedBounds
        {
            get { return (Rect)GetValue(BoundsProperty); }
        }

        protected static void OnBoundsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Rect bounds = (Rect)e.NewValue;
            Geometry geometry = new RectangleGeometry(bounds);
            ((FalseColourImage)obj).imageRectangle.Data = geometry;
        }

        public FalseColourImage(double[,] underlyingData)
        {
            this.underlyingData = underlyingData.ArrayEnumerator(EnumerationOrder2D.RowMajor);
            width = underlyingData.GetLength(0);
            height = underlyingData.GetLength(1);
            Initialize(true);
        }

        public FalseColourImage(IEnumerable<object> underlyingData)
        {
            Array array = GeneralArray.ToDoubleArray(underlyingData);
            this.underlyingData = ((double[,])array).ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
            width = array.GetLength(0);
            height = array.GetLength(1);
            Initialize(true);
        }

        internal FalseColourImage(Rect bounds, double[,] underlyingData, bool newColourBar)
        {
            this.underlyingData = underlyingData.ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
            width = underlyingData.GetLength(0);
            height = underlyingData.GetLength(1);
            Initialize(newColourBar);
            Bounds = bounds;
        }

        internal FalseColourImage(Rect bounds, IEnumerable<object> underlyingData, bool newColourBar)
        {
            Array array = GeneralArray.ToDoubleArray(underlyingData);
            this.underlyingData = ((double[,])array).ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
            width = array.GetLength(0);
            height = array.GetLength(1);
            Initialize(newColourBar);
            Bounds = bounds;
        }
        
#if ILNumerics
        public FalseColourImage(ILArray<double> underlyingData)
        {
            this.underlyingILArrayData = underlyingData;
            width = underlyingData.Dimensions[0];
            height = underlyingData.Dimensions[1];
            useILArray = true;
            Initialize(true);
        }

        public FalseColourImage(Rect bounds, ILArray<double> underlyingData)
        {
            this.underlyingILArrayData = underlyingData;
            width = underlyingData.Dimensions[0];
            height = underlyingData.Dimensions[1];
            useILArray = true;
            Initialize(true);
            Bounds = bounds;
        }

        internal FalseColourImage(Rect bounds, ILArray<double> underlyingData, bool newColourBar)
        {
            this.underlyingILArrayData = underlyingData;
            width = underlyingData.Dimensions[0];
            height = underlyingData.Dimensions[1];
            useILArray = true;
            Initialize(newColourBar);
            Bounds = bounds;
        }

        public UInt16[] UnderlyingILArrayToIndexArray(int nIndices)
        {
            double max = underlyingILArrayData.MaxValue;
            double min = underlyingILArrayData.MinValue;
            double Scale = (nIndices - 1) / (max - min);
            int count = width * height;
            int index = 0;
            UInt16[] indices = new UInt16[count];
            foreach (double value in underlyingILArrayData)
            {
                indices[index] = (UInt16)((value - min) * Scale);
                index++;
            }
            return indices;
        }
#endif

        protected void Initialize(bool newColourBar)
        {
            colourMapUpdateTimer = new DispatcherTimer();
            colourMapUpdateTimer.Interval = new TimeSpan(2000); // 2/10 s
            colourMap = new ColourMap(ColourMapType.Jet, 256);
            imageRectangle = new Path();
            Geometry geometry = new RectangleGeometry(bounds);
            imageRectangle.Data = geometry;
            RenderOptions.SetBitmapScalingMode(imageRectangle, BitmapScalingMode.NearestNeighbor);
#if ILNumerics
            if (useILArray)
            {
                indices = UnderlyingILArrayToIndexArray(colourMap.Length);
            }
#endif
            if (!useILArray) indices = UnderlyingToIndexArray(colourMap.Length);
            writeableBitmap = new WriteableBitmap(IndexArrayToBitmapSource());
            imageBrush = new ImageBrush(writeableBitmap);
            imageRectangle.Fill = imageBrush;
            Bounds = new Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
            if (newColourBar)
            {
                colourBar = new ColourBar(ColourMap);
#if ILNumerics
                if (useILArray)
                {
                    colourBar.Min = underlyingILArrayData.MinValue;
                    colourBar.Max = underlyingILArrayData.MaxValue;
                }
#endif
                if (!useILArray)
                {
                    colourBar.Min = underlyingData.Min();
                    colourBar.Max = underlyingData.Max();
                }
            }
        }

        public static UInt16[] IEnumerableToIndexArray(IEnumerable<double> data, int width, int height, int nIndices)
        {
            double max = data.Max();
            double min = data.Min();
            double Scale = (nIndices - 1) / (max - min);
            int count = width * height;
            int index = 0;
            UInt16[] indices = new UInt16[count];
            foreach (double value in data)
            {
                indices[index] = (UInt16)((value - min) * Scale);
                index++;
            }
            return indices;
        }

        public UInt16[] UnderlyingToIndexArray(int nIndices)
        {
            double max = underlyingData.Max();
            double min = underlyingData.Min();
            double Scale = (nIndices - 1) / (max - min);
            int count = width * height; 
            int index = 0;
            UInt16[] indices = new UInt16[count];
            foreach (double value in underlyingData)
            {
                indices[index] = (UInt16)((value - min) * Scale);
                index++;
            }
            return indices;
        }

#if ILNumerics 
        public static BitmapSource ILArrayToBitmapSource(ILArray<double> surface, ColourMap colourMap)
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int width = surface.Dimensions[0];
            int height = surface.Dimensions[1];
            int bytes = (pf.BitsPerPixel + 7) / 8;
            int rawStride = (width * bytes);
            byte[] rawImage = new byte[rawStride * height];
            int index = 0;
            byte[,] cmap = colourMap.ToByteArray();
            int colourMapLength = colourMap.Length;
            double min = surface.MinValue;
            double range = surface.MaxValue - min;
            int magnitude = 0;
            ILArray<int> scaled = (ILArray<int>)ILMath.convert(NumericType.Int32, ILMath.floor((surface - min) * (double)((colourMapLength - 1) / range)));
            ILIterator<int> iterator = scaled.CreateIterator();
            magnitude = iterator.Value;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    rawImage[index] = cmap[magnitude, 3];
                    rawImage[index + 1] = cmap[magnitude, 2];
                    rawImage[index + 2] = cmap[magnitude, 1];
                    rawImage[index + 3] = cmap[magnitude, 0];
                    index += bytes;
                    magnitude = iterator.Increment();
                }
            }
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }

        public static BitmapSource ILArrayToBitmapSourceReversed(ILArray<double> surface, ColourMap colourMap)
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int width = surface.Dimensions[0];
            int height = surface.Dimensions[1];
            int bytes = (pf.BitsPerPixel + 7) / 8;
            int rawStride = (width * bytes);
            byte[] rawImage = new byte[rawStride * height];
            int index = 0;
            byte[,] cmap = colourMap.ToByteArray();
            int colourMapLength = colourMap.Length;
            double range = surface.MaxValue - surface.MinValue;
            double min = surface.MinValue;
            int magnitude = 0;
            ILArray<int> scaled = (ILArray<int>)ILMath.convert(NumericType.Int32, ILMath.floor((surface - min) * (double)(colourMapLength - 1) / range));
            ILIterator<int> iterator = scaled.CreateIterator();
            magnitude = iterator.Value;
            for (int y = height - 1; y >= 0; --y)
            {
                index = y * rawStride; 
                for (int x = 0; x < width; ++x)
                {
                    rawImage[index] = cmap[magnitude, 3];
                    rawImage[index + 1] = cmap[magnitude, 2];
                    rawImage[index + 2] = cmap[magnitude, 1];
                    rawImage[index + 3] = cmap[magnitude, 0];
                    index += bytes;
                    magnitude = iterator.Increment();
                }
            }
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);
            return bitmap;
        }
#endif  
       
        public void OnColourMapChanged(object sender, RoutedEventArgs e)
        {
            colourMapUpdateTimer.Start();           
        }
        
        private void OnColourMapUpdateTimerElapsed(object sender, EventArgs e)
        {
            if (updateInProgress)
            {
                colourMapUpdateTimer.Start();
                return;
            }
            updateColourMap = colourMap.ToIntArray();
            colourMapUpdateTimer.Stop();
            object state = new object();
            writeableBitmap.Lock();
            backBuffer = writeableBitmap.BackBuffer;
            updateInProgress = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateWriteableBitmap), (object)state);
        }

        private BitmapSource IndexArrayToBitmapSource()
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int bytes = (pf.BitsPerPixel + 7) / 8;
            int rawStride = (width * bytes);
            byte[] rawImage = new byte[rawStride * height];
            int byteIndex = 0;
            byte[,] cmap = colourMap.ToByteArray();
            foreach (UInt16 magnitude in indices)
            {
                rawImage[byteIndex] = cmap[magnitude, 3];
                rawImage[byteIndex + 1] = cmap[magnitude, 2];
                rawImage[byteIndex + 2] = cmap[magnitude, 1];
                rawImage[byteIndex + 3] = cmap[magnitude, 0];
                byteIndex += bytes;
            }
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }

        private void UpdateWriteableBitmap(Object state)
        {
            unsafe
            {
                int* pBackBuffer = (int*)backBuffer;
                PixelFormat pf = PixelFormats.Bgr32;
                int bytes = (pf.BitsPerPixel + 7) / 8;
                for (int i = 0; i < indices.Length; ++i)
                {
                    *pBackBuffer = updateColourMap[indices[i]];
                    pBackBuffer++;
                }
            }
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new AfterUpdateCallback(AfterUpdateWriteableBitmap));
        }

        private void AfterUpdateWriteableBitmap()
        {
            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            writeableBitmap.Unlock();
            updateInProgress = false;
        }
    }
}
