using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Media;
using System.Data;

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

    public class GridHelpers
    {
        public static DataView ArrayToView(double[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            DataTable table = new DataTable();
            for (int j = 0; j < cols; ++j)
                table.Columns.Add(new DataColumn(j.ToString(), typeof(Double)));

            for (int i = 0; i < rows; ++i)
            {
                object[] rowData = new object[cols];
                for (int j = 0; j < cols; ++j) rowData[j] = (object)(array[i, j]);
                DataRow row = table.LoadDataRow(rowData, false);
            }
            DataView view = table.DefaultView;
            view.AllowNew = false;
            return view;
        }
    }
}
