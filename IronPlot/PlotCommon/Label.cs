using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;

namespace IronPlot
{
    public class Label : TextBlock
    {
        public Label()
            : base()
        {
            this.Visibility = Visibility.Collapsed;
            DependencyPropertyDescriptor descriptor =
                DependencyPropertyDescriptor.FromProperty(
                TextBlock.TextProperty, typeof(TextBlock));
            descriptor.AddValueChanged(this, OnTextChanged);
        }

        private void OnTextChanged(object sender, EventArgs args)
        {
            if (Text == String.Empty && Text == "") this.Visibility = Visibility.Collapsed;
            else this.Visibility = Visibility.Visible;
        }
    }
}
