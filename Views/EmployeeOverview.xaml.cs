using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AccountingHelper.Views
{
    /// <summary>
    /// Interaktionslogik für EmployeeOverview.xaml
    /// </summary>
    public partial class EmployeeOverview : UserControl
    {
        private bool isMeasured = false;
        public EmployeeOverview()
        {
            InitializeComponent();
        }

        private void EmployeeChart_UIUpdated(object sender, EventArgs e)
        {
    
        }

     
    }
}
