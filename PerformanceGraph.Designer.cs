namespace ParserPerformance
{
    partial class PerformanceGraph
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.performanceChartASCIIString = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.performanceChartASCIIString)).BeginInit();
            this.SuspendLayout();
            // 
            // performanceChartASCIIString
            // 
            chartArea1.AxisX.IntervalOffset = 16000D;
            chartArea1.AxisX.IntervalOffsetType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            chartArea1.AxisX.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            chartArea1.AxisX.Maximum = 96000D;
            chartArea1.AxisX.Minimum = 0D;
            chartArea1.AxisX.Title = "Data Values(bytes)";
            chartArea1.AxisY.Title = "Time Taken(ms)";
            chartArea1.Name = "ChartArea1";
            this.performanceChartASCIIString.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.performanceChartASCIIString.Legends.Add(legend1);
            this.performanceChartASCIIString.Location = new System.Drawing.Point(178, 59);
            this.performanceChartASCIIString.Name = "performanceChartASCIIString";
            this.performanceChartASCIIString.Size = new System.Drawing.Size(973, 348);
            this.performanceChartASCIIString.TabIndex = 1;
            this.performanceChartASCIIString.Text = "chart1";
            title1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            title1.Name = "Title1";
            title1.Text = "ASCII DATA Of Type String";
            this.performanceChartASCIIString.Titles.Add(title1);
            // 
            // PerformanceGraph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1454, 787);
            this.Controls.Add(this.performanceChartASCIIString);
            this.Name = "PerformanceGraph";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.PerformanceGraph_Load);
            ((System.ComponentModel.ISupportInitialize)(this.performanceChartASCIIString)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart performanceChartASCIIString;
    }
}

