using System;
using System.Collections.Generic;
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
    /// Interaktionslogik für EmployeeDocument.xaml
    /// </summary>
    public partial class EmployeeDocument : UserControl
    {
        public EmployeeDocument()
        {
            InitializeComponent();
        }

        private void RadDataForm_AutoGeneratingField(object sender, Telerik.Windows.Controls.Data.DataForm.AutoGeneratingFieldEventArgs e)
        {
            if (e.PropertyName.Equals("Id")) e.Cancel = true;
            if (e.PropertyName.Equals("EmployeeId")) e.Cancel = true;
            if (e.PropertyName.Equals("Employee")) e.DataField.IsReadOnly = true;
            if (e.PropertyName.Equals("DocumentFileName")) e.DataField.IsReadOnly = true;
        }
    }
}
