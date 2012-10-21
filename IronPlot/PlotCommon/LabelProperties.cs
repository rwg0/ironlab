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
using System.Reflection;
namespace IronPlot
{
    public class LabelProperties : DependencyObject
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background",
            typeof(Brush), typeof(LabelProperties),
            new PropertyMetadata(Brushes.Transparent));

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground",
            typeof(Brush), typeof(LabelProperties),
            new PropertyMetadata(Brushes.Black));

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily",
            typeof(FontFamily), typeof(LabelProperties),
            new PropertyMetadata(new FontFamily("Tahoma")));

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize",
            typeof(double), typeof(LabelProperties),
            new PropertyMetadata());

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register("FontStyle",
            typeof(FontStyle), typeof(LabelProperties),
            new PropertyMetadata(FontStyles.Normal));

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register("FontWeight",
            typeof(FontWeight), typeof(LabelProperties),
            new PropertyMetadata(FontWeights.Normal));

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text",
            typeof(string), typeof(LabelProperties),
            new PropertyMetadata("Label"));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        //public static readonly DependencyProperty TypographyProperty =
        //    DependencyProperty.Register("Typography",
        //    typeof(Typography), typeof(LabelProperties),
        //    new PropertyMetadata());

        public LabelProperties()
        {
        }

        /// <summary>
        /// Constructor offering the ability to bind the new object to a parent.
        /// </summary>
        /// <param name="parentLabelProperties">Parent set of properties.</param>
        public LabelProperties(LabelProperties parentLabelProperties) : base()
        {
            SetParent(parentLabelProperties);
        }

        internal void SetParent(LabelProperties parentLabelProperties)
        {
            FieldInfo[] fields = this.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                DependencyProperty dp = (DependencyProperty)field.GetValue(this);
                DependencyProperty dpTextblock = (DependencyProperty)(parentLabelProperties.GetType().GetField(string.Concat(dp.Name, "Property")).GetValue(parentLabelProperties));
                Binding bindingTransform = new Binding(dp.Name);
                bindingTransform.Source = parentLabelProperties;
                bindingTransform.Mode = BindingMode.OneWay;
                BindingOperations.SetBinding(this, dpTextblock, bindingTransform);
            }
        }

        internal void BindTextBlock(TextBlock textblock)
        {
            FieldInfo[] fields = this.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                DependencyProperty dp = (DependencyProperty)field.GetValue(this);
                DependencyProperty dpTextblock = (DependencyProperty)(textblock.GetType().GetField(string.Concat(dp.Name, "Property")).GetValue(textblock));
                Binding bindingTransform = new Binding(dp.Name);
                bindingTransform.Source = this;
                bindingTransform.Mode = BindingMode.OneWay;
                BindingOperations.SetBinding(textblock, dpTextblock, bindingTransform);
            }
        }
    }
}
