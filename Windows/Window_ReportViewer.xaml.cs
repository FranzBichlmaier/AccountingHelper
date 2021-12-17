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
using System.Windows.Shapes;
using Telerik.Reporting;

namespace AccountingHelper.Windows
{
    /// <summary>
    /// Interaktionslogik für Window_ReportViewer.xaml
    /// </summary>
    public partial class Window_ReportViewer : Window
    {
        TypeReportSource typeReportSource = new TypeReportSource();        
        private InstanceReportSource instanceReportSource = new InstanceReportSource();
        bool isBook = false;
        
        public Window_ReportViewer()
        {
            InitializeComponent();
        }

        public Window_ReportViewer(TypeReportSource reportSource)
        {
            InitializeComponent();
            typeReportSource = reportSource;
        }
        public Window_ReportViewer(ReportBook reportBook)
        {
            InitializeComponent();
            isBook = true;
            instanceReportSource.ReportDocument = reportBook;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (isBook)
            {
                ReportViewer.ReportSource = instanceReportSource;
                ReportViewer.RefreshReport();
            }
            else
            {
                ReportViewer.ReportSource = typeReportSource;
                ReportViewer.RefreshReport();
            }

        }
    }
}
