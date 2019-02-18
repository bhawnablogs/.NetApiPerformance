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
    public partial class GridView : Form
    {
        public GridView()
        {
            InitializeComponent();
        }

        private void GridView_Load(object sender, EventArgs e)
        {
            PerformanceData.FetchPerformanceData(PerformanceData.DataType.ASCIIDouble);
            dataGridView1.DataSource = PerformanceData.GraphData;
            dataGridView1.Columns[0].Name = "Data";
            dataGridView1.Columns[0].HeaderText = "Data";
            dataGridView1.Columns[0].DataPropertyName = "CustomerID";

            dataGridView1.Columns[1].HeaderText = "Contact Name";
            dataGridView1.Columns[1].Name = "Name";
            dataGridView1.Columns[1].DataPropertyName = "ContactName";

            dataGridView1.Columns[2].Name = "Country";
            dataGridView1.Columns[2].HeaderText = "Country";
            dataGridView1.Columns[2].DataPropertyName = "Country";
        }

        
    }
}
