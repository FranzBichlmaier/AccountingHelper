using AccountingHelper.Core;
using AccountingHelper.Models;
using AccountingHelper.SupportClasses;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Telerik.Reporting;
using AccountingHelper.Events;
using Prism.Events;
using DateTimeFunctions;

namespace AccountingHelper.ViewModels
{
    public class SalaryOverViewModel: Qc_ViewModelBase  
    {
        private List<EmployeeSalaryOverview> overviews = new List<EmployeeSalaryOverview>();
        private DateDifferences dateFunctions = new DateDifferences();

        private DateTime periodFrom;
        public DateTime PeriodFrom
        {
            get { return periodFrom; }
            set { SetProperty(ref periodFrom, value); }
        }
        private DateTime periodTo;
        private readonly IEventAggregator eventAggregator;

        public DateTime PeriodTo
        {
            get { return periodTo; }
            set { SetProperty(ref periodTo, value); }
        }
        public ICommand StartOverviewCommand { get; private set; }
        private string selectedFilename;
        public string SelectedFilename
        {
            get { return selectedFilename; }
            set { SetProperty(ref selectedFilename, value); }
        }

        private bool sortByOffice;
        public bool SortByOffice
        {
            get { return sortByOffice; }
            set { SetProperty(ref sortByOffice, value); }
        }

        public SalaryOverViewModel(IEventAggregator eventAggregator)
        {
            StartOverviewCommand = new DelegateCommand(OnStartOverview);
            this.eventAggregator = eventAggregator;
        }

        private void OnStartOverview()
        {
            // set PeriodTo to end of Month

            PeriodTo = dateFunctions.MonthEnd(PeriodTo);
            CreateEmployeeSalaryOverview createOverview = new CreateEmployeeSalaryOverview(null, PeriodFrom.Date, PeriodTo.Date);
            overviews = createOverview.GetSalaryOverview();   

            if (SortByOffice)
            {
                overviews = overviews.OrderBy(s => s.OfficeLocation).ThenBy(s => s.EmployeeName).ToList();
            }
            


            // create JsonFile
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;

            string jsonContent = JsonConvert.SerializeObject(overviews, settings);

            string jsonfile = $"C:\\Users\\Public\\Documents\\Json.json";

            System.IO.File.WriteAllText(jsonfile,jsonContent);

            //TypeReportSource source = new TypeReportSource();
            //source.TypeName = typeof(AccountingHelper.Reporting.AnnualSalarySummary).AssemblyQualifiedName;
            //source.Parameters.Add("Source", jsonfile);
            //source.Parameters.Add("DataSelector", string.Empty);

            //ViewerParameter parameter = new ViewerParameter();
            //parameter.typeReportSource = source;

            //eventAggregator.GetEvent<ViewerParameterEvent>().Publish(parameter);

            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

            // set any deviceInfo settings if necessary
            var deviceInfo = new System.Collections.Hashtable();


            var reportSource = new Telerik.Reporting.TypeReportSource();

            // reportName is the Assembly Qualified Name of the report
            reportSource.TypeName = typeof(AccountingHelper.Reporting.AnnualSalarySummary).AssemblyQualifiedName;


            // Pass parameter value with the Report Source if necessary         
            reportSource.Parameters.Add("Source", jsonfile);
            reportSource.Parameters.Add("DataSelector", string.Empty);

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);
            jsonfile = jsonfile.Replace(".json", ".pdf");


            using (System.IO.FileStream fs = new System.IO.FileStream(jsonfile, System.IO.FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }            

            SelectedFilename = jsonfile;

            result = reportProcessor.RenderReport("XLSX", reportSource, deviceInfo);
            string excelFileName = $"C:\\Users\\Public\\Documents\\gehaltsuebersicht.xlsx";

            using (System.IO.FileStream fs = new System.IO.FileStream(excelFileName, System.IO.FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }


        }
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            // set PeriodFrom and PeriodTo to the current Year (if it is January use the previous year)

            DateTime helpDate = DateTime.Now;

           

            PeriodFrom = new DateTime(helpDate.Year, 1, 1);
            PeriodTo = new DateTime(helpDate.Year, 12, 31);
        }
    }
}
