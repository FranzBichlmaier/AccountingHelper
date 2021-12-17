using AccountingHelper.Core;
using AccountingHelper.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Documents.Flow.Model;
using Telerik.Windows.Documents.Flow.Model.Editing;
using DateTimeFunctions;

namespace AccountingHelper.ViewModels
{
    public class CreateEmployeeContractViewModel : Qc_ViewModelBase
    {
        private string selectedFileName;
        public string SelectedFileName
        {
            get { return selectedFileName; }
            set { SetProperty(ref selectedFileName, value); }
        }
        private bool canCreateContract = false;
        public bool CanCreateContract
        {
            get { return canCreateContract; }
            set { SetProperty(ref canCreateContract, value); }
        }

        public InternshipContractModel InternshipData { get; set; }
        public string SelectedTemplate { get; set; } = string.Empty;
        public RadFlowDocument document;

        private DateDifferences dateFunctions = new DateDifferences();
        public ICommand CreateContractCommand { get; set; }
        public ICommand ResetInformationCommand { get; set; }
        public ICommand BrowseCommand { get; set; }
        public CreateEmployeeContractViewModel()
        {
            CreateContractCommand = new DelegateCommand(OnCreateContract).ObservesCanExecute(() =>CanCreateContract);
            ResetInformationCommand = new DelegateCommand(OnResetInformation);
            BrowseCommand = new DelegateCommand(OnSelectTemplate);
        }

        private void OnSelectTemplate()
        {
            Telerik.Windows.Controls.RadOpenFileDialog fileDialog = new Telerik.Windows.Controls.RadOpenFileDialog();
            fileDialog.Owner = Application.Current.MainWindow;
            fileDialog.InitialDirectory = Properties.Settings.Default.ContractTemplates;
            //fileDialog.InitialDirectory = @"C:\Users\franz\source\repos\ContractCreator\Files";
            fileDialog.DefaultExt = "docx";
            fileDialog.Filter = "Word-Documents (*.docx)|*.DOCX";
            bool? result = fileDialog.ShowDialog();
            if (result == null)
            {
                CanCreateContract = false;
                return;
            }
            if (result == false)
            {
                CanCreateContract = false;
                return;
            }

            // ReportFileName only contains filename relative to travelExpenseRoot directory
            SelectedTemplate = fileDialog.FileName;
            RaisePropertyChanged("SelectedTemplate");
            CanCreateContract = true;            
        }

        private void OnResetInformation()
        {
            InternshipData = new InternshipContractModel();
            InternshipData.StartDate = DateTime.Now;
            InternshipData.EndDate = DateTime.Now.AddMonths(2);
            RaisePropertyChanged("InternshipData");
        }

        private void OnCreateContract()
        {
            SetFormattedFields();
            
            Telerik.Windows.Documents.Flow.FormatProviders.Docx.DocxFormatProvider provider = new Telerik.Windows.Documents.Flow.FormatProviders.Docx.DocxFormatProvider();
            using (Stream input = File.OpenRead(SelectedTemplate))
            {
                document = provider.Import(input);
            }
          
            // perform Mailmerge
            
            List<InternshipContractModel> interns = new List<InternshipContractModel>();
            interns.Add(InternshipData);
            RadFlowDocument resultDocument = document.MailMerge(interns);

            // create pdf Filename

            FileInfo fileInfo = new FileInfo(SelectedTemplate);
            string fileName = $"Offerletter for {InternshipData.FirstName} {InternshipData.LastName}.pdf";
            string outputFile = Path.Combine(fileInfo.DirectoryName, fileName);

            Telerik.Windows.Documents.Flow.FormatProviders.Pdf.PdfFormatProvider pdfprovider = new Telerik.Windows.Documents.Flow.FormatProviders.Pdf.PdfFormatProvider();
            using (Stream output = File.OpenWrite(outputFile))
            {               
                pdfprovider.Export(resultDocument, output);
            }

            SelectedFileName = outputFile;


        }

        private void SetFormattedFields()
        {
            CultureInfo currentInfo = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            InternshipData.FormattedBonus = $"{InternshipData.MaxBonusPerMonth:n2}";
            InternshipData.FormattedSalary = $"{InternshipData.MonthlySalary:n2}";

            // calculate total Bonus

            double month = dateFunctions.NumberOfMonths(InternshipData.StartDate, InternshipData.EndDate);
            decimal totalBonus = (decimal)Math.Round(month * (double)InternshipData.MaxBonusPerMonth, 0);
            // round to full 100
            totalBonus = Math.Ceiling(Math.Round(totalBonus / 100, 1)) * 100;
            InternshipData.VacationDays = (int)Math.Ceiling((double)28 / (double)12 * month);
            InternshipData.FormattedTotalBonus = $"{totalBonus:n2}";
            InternshipData.FormattedStart = $"{InternshipData.StartDate:MMMM dd, yyyy}";
            InternshipData.FormattedEnd = $"{InternshipData.EndDate:MMMM dd, yyyy}";
            InternshipData.Today = $"{DateTime.Now:MMMM dd, yyyy}";

            if (InternshipData.HasVacation)
            {
                InternshipData.VacationText = $"During your internship, you will have {InternshipData.VacationDays} days of vacation.";
            }
            else
            {
                InternshipData.VacationText = string.Empty;
            }

            CultureInfo.CurrentCulture = currentInfo;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            InternshipData = new InternshipContractModel();
            InternshipData.StartDate = DateTime.Now;
            InternshipData.EndDate = DateTime.Now.AddMonths(2);
            RaisePropertyChanged("InternshipData");
        }
    }
}
