using AccountingHelper.Core;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingHelper.ViewModels
{
    public class EmployeeOfficeViewModel: Qc_ViewModelBase
    {
        private string selectedFileName;
        public string SelectedFileName
        {
            get { return selectedFileName; }
            set { SetProperty(ref selectedFileName, value); }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            SelectedFileName = string.Empty;

            // render report as pdf

            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

            // set any deviceInfo settings if necessary
            var deviceInfo = new System.Collections.Hashtable();


            var reportSource = new Telerik.Reporting.TypeReportSource();

            // reportName is the Assembly Qualified Name of the report
            reportSource.TypeName = typeof(AccountingHelper.Reporting.EmployeeOffices).AssemblyQualifiedName;


            // Pass parameter value with the Report Source if necessary         
            // no parameters required

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);


            using (System.IO.FileStream fs = new System.IO.FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mitarbeiter-Büro-Zuordnung.pdf"), System.IO.FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }

            SelectedFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mitarbeiter-Büro-Zuordnung.pdf");
        }
    }
}
