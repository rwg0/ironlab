// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.Integration;
using System.Windows.Forms.DataVisualization.Charting;

namespace ILab.Plot
{
    /// <summary>
    /// Control containing a Chart (Microsoft Chart Controls for .NET)
    /// http://www.microsoft.com/downloads/details.aspx?FamilyId=130F7986-BF49-4FE5-9CA8-910AE6EA442C
    /// </summary>
    public class MSChart : ContentControl
    {
        Form form;
        WindowsFormsHost host;
        Chart chart;

        /// <summary>
        /// Get Chart (Microsoft Chart Controls for .NET)
        /// </summary>
        public Chart Chart
        {
            get { return chart; }
        }

        public MSChart()
        {
            host = new WindowsFormsHost();
            chart = new Chart();
            host.Child = chart;
            Content = host;
        }
    }
}
