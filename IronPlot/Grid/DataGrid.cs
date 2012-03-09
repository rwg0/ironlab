using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;
using DataGrid = IronPlot.DataGrid;

namespace IronPlot
{
    public class DataGrid : System.Windows.Controls.DataGrid
    {
        static DataGrid()
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(DataGrid),
                new CommandBinding(ApplicationCommands.Paste,
                    new ExecutedRoutedEventHandler(OnExecutedPaste),
                    new CanExecuteRoutedEventHandler(OnCanExecutePaste)));
        }

        #region Clipboard Paste

        private static void OnCanExecutePaste(object target, CanExecuteRoutedEventArgs args)
        {
            ((DataGrid)target).OnCanExecutePaste(args);
        }

        /// <summary>
        /// This virtual method is called when ApplicationCommands.Paste command query its state.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCanExecutePaste(CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = CurrentCell != null;
            args.Handled = true;
        }

        private static void OnExecutedPaste(object target, ExecutedRoutedEventArgs args)
        {
            ((DataGrid)target).OnExecutedPaste(args);
        }

        /// <summary> 
        /// This virtual method is called when ApplicationCommands.Paste command is executed. 
        /// </summary> 
        /// <param name="args"></param> 
        protected virtual void OnExecutedPaste(ExecutedRoutedEventArgs args)
        {
            Debug.WriteLine("OnExecutedPaste begin");

            // parse the clipboard data             
            List<string[]> rowData = ClipboardHelper.ParseClipboardData();

            // call OnPastingCellClipboardContent for each cell 
            //int nSelectedCells = 
            int minRowIndex = Items.IndexOf(CurrentItem);
            int maxRowIndex = Items.Count - 1;
            int minColumnDisplayIndex = (SelectionUnit != DataGridSelectionUnit.FullRow) ? Columns.IndexOf(CurrentColumn) : 0;
            int maxColumnDisplayIndex = Columns.Count - 1;
            if (SelectedCells.Count > 1) GetSelectionBounds(ref minRowIndex, ref maxRowIndex, ref minColumnDisplayIndex, ref maxColumnDisplayIndex);
            int rowDataIndex = 0;
            for (int i = minRowIndex; i <= maxRowIndex && rowDataIndex < rowData.Count; i++, rowDataIndex++)
            {
                int columnDataIndex = 0;
                for (int j = minColumnDisplayIndex; j <= maxColumnDisplayIndex && columnDataIndex < rowData[rowDataIndex].Length; j++, columnDataIndex++)
                {
                    DataGridColumn column = ColumnFromDisplayIndex(j);
                    column.OnPastingCellClipboardContent(Items[i], rowData[rowDataIndex][columnDataIndex]);
                }
            }
        }

        private void GetSelectionBounds(ref int minRow, ref int maxRow, ref int minColumn, ref int maxColumn)
        {
            int row, column;
            foreach (DataGridCellInfo cell in SelectedCells)
            {
                row = Items.IndexOf(cell.Item);
                column = Columns.IndexOf(cell.Column);
                if (row < minRow) minRow = row;
                if (row > maxRow) maxRow = row;
                if (column < minColumn) minColumn = column;
                if (column > maxColumn) maxColumn = column;
            }
        } 
        
        #endregion Clipboard Paste

    }
}
