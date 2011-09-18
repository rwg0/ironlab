using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public partial class Title : ContentControl
    {
#if !SILVERLIGHT
        /// <summary>
        /// Initializes the static members of the Title class.
        /// </summary>
        static Title()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Title), new FrameworkPropertyMetadata(typeof(Title)));
        }

#endif
        /// <summary>
        /// Initializes a new instance of the Title class.
        /// </summary>
        public Title()
        {
#if SILVERLIGHT
            DefaultStyleKey = typeof(Title);
#endif
        }
    }
}
