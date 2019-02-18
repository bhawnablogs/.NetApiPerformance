using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ParserPerformance
{
    public partial class PerformanceGraph : Form
    {
        public PerformanceGraph()
        {
            InitializeComponent();
        }

        private void PerformanceGraph_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.DataVisualization.Charting.Chart performanceChart = null;
            foreach (PerformanceData.DataType dataType in Enum.GetValues(typeof(PerformanceData.DataType)))
            {
                switch (dataType)
                {
                    
                    case PerformanceData.DataType.ASCIIString:
                        performanceChart = performanceChartASCIIString;
                        break;

                }

                PopulateChartData(performanceChart, dataType);
            }
        }

        private void PopulateChartData(System.Windows.Forms.DataVisualization.Charting.Chart performanceChart, PerformanceData.DataType dataType)
        {
            PerformanceData.FetchPerformanceData(dataType);
            foreach (string address in PerformanceData.Addresses)
            {
                System.Windows.Forms.DataVisualization.Charting.Series series = new System.Windows.Forms.DataVisualization.Charting.Series();
                series.ChartArea = "ChartArea1";
                series.IsValueShownAsLabel = true;
                series.Legend = "Legend1";
                series.Name = address;
                series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                performanceChart.Series.Add(series);
            }
            for (int index = 0; index < PerformanceData.DataRanges.Length; index++)
            {
                int iXPoint = PerformanceData.DataRanges[index];
                PerformanceData.TimeTakenForData[] tymTaken = null;
                PerformanceData.GraphData.TryGetValue(iXPoint, out tymTaken);
                foreach(PerformanceData.TimeTakenForData tmtaken in tymTaken)
                {
                    performanceChart.Series[tmtaken.Address].Points.AddXY(iXPoint, tmtaken.TimeTaken);
                }
            }
            performanceChart.DataBind();
        }       
    }
}
