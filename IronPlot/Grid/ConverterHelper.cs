using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Media;

namespace IronPlot
{
    #region Converters

    public class SortDirectionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            switch ((ListSortDirection)value)
            {
                case ListSortDirection.Ascending:
                    return "Ascending";
                case ListSortDirection.Descending:
                    return "Descending";
                default:
                    break;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((string)value)
            {
                case "null":
                    return null;
                case "Ascending":
                    return ListSortDirection.Ascending;
                case "Descending":
                    return ListSortDirection.Descending;
                default:
                    break;
            }

            return null;
        }

        #endregion
    }

    #endregion Converters
}
