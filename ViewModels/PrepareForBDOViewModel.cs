using AccountingHelper.Core;
using AccountingHelper.Models;
using AccountingHelper.SupportClasses;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccountingHelper.ViewModels
{
    public class PrepareForBDOViewModel: Qc_ViewModelBase
    {
        private int reportMonth = DateTime.Now.Month;
        private int selectedMonth;
        public int SelectedMonth
        {
            get { return selectedMonth; }
            set { SetProperty(ref selectedMonth, value); }
        }
        private CreateEmployeeSalaryOverview createOverView;
        
        private string selectedFilename = null;
        public string SelectedFilename
        {
            get { return selectedFilename; }
            set { SetProperty(ref selectedFilename, value); }
        }

        private string startButtonContent = "Datei erstellen";
        public string StartButtonContent
        {
            get { return startButtonContent; }
            set { SetProperty(ref startButtonContent, value); }
        }

        public ICommand StartCalculationCommand { get; set; }


        public PrepareForBDOViewModel()
        {
            StartCalculationCommand = new DelegateCommand(OnStartCalculation);
            SelectedMonth = reportMonth - 1;
        }

        private void OnStartCalculation()
        {
            List<EmployeeChangeLog> bdoInformation = createOverView.GetChangeLogItems(SelectedMonth+1);
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;

            string jsonContent = JsonConvert.SerializeObject(bdoInformation, settings);

            string jsonfile = $"C:\\Users\\Public\\Documents\\BdoInformation.json";

            System.IO.File.WriteAllText(jsonfile, jsonContent);

            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

            // set any deviceInfo settings if necessary
            var deviceInfo = new System.Collections.Hashtable();


            var reportSource = new Telerik.Reporting.TypeReportSource();

            // reportName is the Assembly Qualified Name of the report
            reportSource.TypeName = typeof(AccountingHelper.Reporting.BDOInformation).AssemblyQualifiedName;


            // Pass parameter value with the Report Source if necessary         
            reportSource.Parameters.Add("Source", jsonfile);
            reportSource.Parameters.Add("DataSelector", string.Empty);

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

            string pdfFile = jsonfile.Replace("json", "pdf");
            using (System.IO.FileStream fs = new System.IO.FileStream(pdfFile, System.IO.FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }

            SelectedFilename = pdfFile;
            RaisePropertyChanged("SelectedFilename");

            result = reportProcessor.RenderReport("XLSX", reportSource, deviceInfo);

            string xlsxFile = jsonfile.Replace("json", "xlsx");
            using (System.IO.FileStream fs = new System.IO.FileStream(xlsxFile, System.IO.FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }

        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // run CreateEmployeeSalaryOverview
            DateTime startDate = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime endDate = new DateTime(DateTime.Now.Year, 12, 31);
            createOverView = new CreateEmployeeSalaryOverview(null, startDate, endDate);

            StartButtonContent = $"Datei erstellen";
        }
    }
}
