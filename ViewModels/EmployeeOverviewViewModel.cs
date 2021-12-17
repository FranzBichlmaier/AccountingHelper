using AccountingHelper.Core;
using AccountingHelper.DataAccess;
using AccountingHelper.Models;
using AccountingHelper.SupportClasses;
using DocumentFormat.OpenXml.Bibliography;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Telerik.Windows.Documents.Fixed;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Fixed.Model.ColorSpaces;
using Telerik.Windows.Documents.Fixed.Model.Editing;
using Telerik.Windows.Documents.Fixed.Model.Editing.Tables;
using Telerik.Windows.Documents.Fixed.Model.Text;

namespace AccountingHelper.ViewModels
{
    public class EmployeeOverviewViewModel: Qc_ViewModelBase
    {
        private static readonly double defaultLeftIndent = 50;
        private static readonly double defaultLineHeight = 16;
        public ObservableCollection<EmployeesForTravelExpenses> Employees { get; set; }
        public ObservableCollection<EmployeeSalaryDetail> EmployeeSalaryDetails { get; set; }
        public ObservableCollection<EmployeeSalaryDetail> EmployeeAnnualDetails { get; set; }
        public ICommand GeneratePdfFilesCommand { get; set; }
        public ICommand ChartLoadedCommand { get; set; }


        private EmployeeSalaryInformation   salaryInformation;
        private Telerik.Windows.Controls.RadCartesianChart employeeChart;
        private string chartFileName = string.Empty;

        public string ChartFileName
        {
            get { return chartFileName; }
            set { SetProperty(ref chartFileName, value); }
        }

        private bool canGeneratePdfFiles;
        public bool CanGeneratePdfFiles
        {
            get { return canGeneratePdfFiles; }
            set { SetProperty(ref canGeneratePdfFiles, value); }
        }
        private Visibility   employeeDetailVisiblity;
        public Visibility   EmployeeDetailVisibility
        {
            get { return employeeDetailVisiblity; }
            set { SetProperty(ref employeeDetailVisiblity, value); }
        }
        private string formattedDateJoined;
        public string FormattedDateJoined
        {
            get { return formattedDateJoined; }
            set { SetProperty(ref formattedDateJoined, value); }
        }
        private string formattedDateLeft;
        public string FormattedDateLeft
        {
            get { return formattedDateLeft; }
            set { SetProperty(ref formattedDateLeft, value); }
        }

        private EmployeesForTravelExpenses selectedEmployee;
        public EmployeesForTravelExpenses SelectedEmployee
        {
            get { return selectedEmployee; }
            set { SetProperty(ref selectedEmployee, value); }
        }
        private ObservableCollection<EmployeesForTravelExpenses> selectedEmployees;
        public ObservableCollection<EmployeesForTravelExpenses> SelectedEmployees
        {
            get { return selectedEmployees; }
            set { SetProperty(ref selectedEmployees, value); }
        }

        private DbAccess dbAccess = new DbAccess();
        public EmployeeOverviewViewModel()
        {
            GeneratePdfFilesCommand = new DelegateCommand(OnGeneratePdfFile);
            ChartLoadedCommand = new DelegateCommand<object>(OnChartLoaded);
        }

        private void OnChartLoaded(object obj)
        {
            employeeChart = obj as Telerik.Windows.Controls.RadCartesianChart;
        }

        #region PdfCreation
        private void OnGeneratePdfFile()
        {
            double height = 0;
            ChartFileName = Path.Combine(Properties.Settings.Default.RootDirectory, SelectedEmployee.FullName);
            ChartFileName += ".png";

            Size chartSize = new Size(employeeChart.ActualWidth, employeeChart.ActualHeight);
          

            employeeChart.Measure(Size.Empty);
            employeeChart.Measure(chartSize);
            employeeChart.Arrange(new Rect(chartSize));            
           

            Task.Delay(100);

            using (Stream stream = File.Open(ChartFileName, FileMode.OpenOrCreate))
            {
                Telerik.Windows.Media.Imaging.ExportExtensions.ExportToImage(employeeChart, stream, new PngBitmapEncoder());
            }

            string fileName = Properties.Settings.Default.PdfVorlagePortait;
            RadFixedDocument document = GetDocumentFromTemplate(fileName);
            RadFixedPage page = document.Pages[0];
            FixedContentEditor editor = new FixedContentEditor(page);

            double currentTopOffset = 150;
            editor.Position.Translate(defaultLeftIndent, currentTopOffset);
            double maxWidth = page.Size.Width - defaultLeftIndent * 2;

            using (editor.SaveProperties())
            {
                height = AddEmployeeNameToPage(editor, maxWidth);
                currentTopOffset += height;
            }

            currentTopOffset += defaultLineHeight * 2;
            editor.Position.Translate(defaultLeftIndent, currentTopOffset);

            using (editor.SaveProperties())
            {
                height = AddEmploymentPeriod(editor, maxWidth);
                currentTopOffset += height;
            }

            currentTopOffset += defaultLineHeight * 2;
            editor.Position.Translate(defaultLeftIndent, currentTopOffset);

            using (editor.SaveProperties())
            {
                height = AddHeadlineToPage(editor, "Gehaltsentwicklung (max. 5 Jahre)",  maxWidth);
                currentTopOffset += height;
            }

            currentTopOffset += defaultLineHeight * 2;
            editor.Position.Translate(defaultLeftIndent, currentTopOffset);

            currentTopOffset += AddImageToPage(editor, ChartFileName, maxWidth);

            currentTopOffset += defaultLineHeight * 4;
            editor.Position.Translate(defaultLeftIndent, currentTopOffset);


            using (editor.SaveProperties())
            {
                height = AddAnnualOverview(editor, maxWidth);
                currentTopOffset += height;
            }



            string saveFileName = @"C:\Users\franz\OneDrive - Franz Bichlmaier Consulting\QuantCo\Uebersicht " + SelectedEmployee.FullName + ".pdf"; 
            SaveDocumentAsPdf(document, saveFileName);
            ChartFileName = string.Empty;
        }

        private double AddImageToPage(FixedContentEditor editor ,string fileName, double width)
        {
            Block block = new Block();

            using (Stream stream = File.OpenRead(fileName))
            {
                block.InsertImage(stream);
            }
            editor.DrawBlock(block, new Size(width, double.PositiveInfinity));
            Size size = block.Measure();
            return size.Height;
        }

        private double AddAnnualOverview(FixedContentEditor editor, double width)
        {
            Table table = new Table();

            // HeaderCells
            TableRow headerRow = table.Rows.AddTableRow();

            TableCell cell1 = headerRow.Cells.AddTableCell();
            cell1.PreferredWidth = 150;

            Block block1 = cell1.Blocks.AddBlock();

            block1.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Center;
            block1.GraphicProperties.FillColor = new RgbColor(0, 0, 255);
            block1.TextProperties.FontSize = 14;
            block1.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block1.InsertText("Kalenderjahr");

            TableCell cell2 = headerRow.Cells.AddTableCell();
            cell2.PreferredWidth = 150;

            Block block2 = cell2.Blocks.AddBlock();

            block2.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Right;
            block2.GraphicProperties.FillColor = new RgbColor(0, 0, 255);
            block2.TextProperties.FontSize = 14;
            block2.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block2.InsertText("Gehalt");

            TableCell cell3 = headerRow.Cells.AddTableCell();
            cell3.PreferredWidth = 150;

            Block block3 = cell3.Blocks.AddBlock();

            block3.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Right;
            block3.GraphicProperties.FillColor = new RgbColor(0, 0, 255);
            block3.TextProperties.FontSize = 14;
            block3.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block3.InsertText("Bonus");


            TableCell cell4 = headerRow.Cells.AddTableCell();
            cell4.PreferredWidth = 150;

            Block block4 = cell4.Blocks.AddBlock();

            block4.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Right;
            block4.GraphicProperties.FillColor = new RgbColor(0, 0, 255);
            block4.TextProperties.FontSize = 14;
            block4.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block4.InsertText("Gesamtvergütung");

            // foreach year one Row
            foreach (EmployeeSalaryDetail detail in EmployeeAnnualDetails)
            {
                TableRow annualRow = table.Rows.AddTableRow();
                AddRowContent(annualRow, detail);
            }

           
            editor.DrawTable(table);
            Size size = table.Measure();
            return size.Height;
        }

        private void AddRowContent(TableRow annualRow, EmployeeSalaryDetail detail)
        {
            TableCell cell1 = annualRow.Cells.AddTableCell();
            cell1.PreferredWidth = 150;
            

            Block block1 = cell1.Blocks.AddBlock();

            block1.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Center;
            block1.TextProperties.FontSize = 14;
            block1.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Normal);
            block1.InsertText(detail.Category);

            TableCell cell2 = annualRow.Cells.AddTableCell();
            cell2.PreferredWidth = 150;

            Block block2 = cell2.Blocks.AddBlock();

            block2.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Right;
            block2.TextProperties.FontSize = 14;
            block2.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Normal);
            block2.InsertText($"{detail.Salary:N2}");

            TableCell cell3 = annualRow.Cells.AddTableCell();
            cell3.PreferredWidth = 150;

            Block block3 = cell3.Blocks.AddBlock();

            block3.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Right;
            block3.TextProperties.FontSize = 14;
            block3.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Normal);
            block3.InsertText($"{detail.Bonus:n2}");


            TableCell cell4 = annualRow.Cells.AddTableCell();
            cell4.PreferredWidth = 150;

            Block block4 = cell4.Blocks.AddBlock();

            block4.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Right;
            block4.TextProperties.FontSize = 14;
            block4.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Normal);
            block4.InsertText($"{detail.TotalCompensation:n2}");
        }

        private double AddEmploymentPeriod(FixedContentEditor editor, double width)
        {
           

            // this is a table with two rows
            // each row has two cells
            // the first cell contains a description
            // the second cel contains a value

            Table table = new Table();

            TableRow firstRow = table.Rows.AddTableRow();
            
            TableCell cell1 = firstRow.Cells.AddTableCell();
            cell1.PreferredWidth = 200;
            
            Block block1 = cell1.Blocks.AddBlock();
            
            block1.HorizontalAlignment = (Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment)HorizontalAlignment.Left;
            block1.TextProperties.FontSize = 20;
            block1.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block1.InsertText("Eintrittsdatum:");

            TableCell cell2 = firstRow.Cells.AddTableCell();
            cell2.PreferredWidth = 200;

            Block block2 = cell2.Blocks.AddBlock();

            block2.HorizontalAlignment = (Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment)HorizontalAlignment.Left;
            block2.TextProperties.FontSize = 20;
            block2.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block2.InsertText(FormattedDateJoined);

            TableRow secondRow = table.Rows.AddTableRow();

            TableCell cell3 = secondRow.Cells.AddTableCell();
            cell3.PreferredWidth = 200;

            Block block3 = cell3.Blocks.AddBlock();

            block3.HorizontalAlignment = (Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment)HorizontalAlignment.Left;
            block3.TextProperties.FontSize = 20;
            block3.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block3.InsertText("Austrittsdatum:");

            TableCell cell4 = secondRow.Cells.AddTableCell();
            cell4.PreferredWidth = 200;

            Block block4 = cell4.Blocks.AddBlock();

            block4.HorizontalAlignment = (Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment)HorizontalAlignment.Left;
            block4.TextProperties.FontSize = 20;
            block4.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block4.InsertText(FormattedDateLeft);

            editor.DrawTable(table);
            Size size = table.Measure();
            return size.Height;
        }

        private double AddHeadlineToPage(FixedContentEditor editor, string headerText, double width)
        {
            Block block = new Block();
            block.GraphicProperties.FillColor = new RgbColor(0, 0, 255);
            block.HorizontalAlignment = (Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment)HorizontalAlignment.Left;
            block.TextProperties.FontSize = 20;
            block.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Bold);
            block.InsertText(headerText);


            editor.DrawBlock(block, new Size(width, double.PositiveInfinity));
            Size size = block.Measure();
            return size.Height;
        }

        private double AddEmployeeNameToPage(FixedContentEditor editor, double width)
        {
            Block block = new Block();
            block.GraphicProperties.FillColor = new RgbColor(0, 0, 255);
            block.HorizontalAlignment = (Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment)HorizontalAlignment.Left;
            block.TextProperties.FontSize = 30;
            block.TextProperties.TrySetFont(new FontFamily("Calibri"), FontStyles.Italic, FontWeights.Bold);
            block.InsertText(SelectedEmployee.FullName);


            editor.DrawBlock(block, new Size(width, double.PositiveInfinity));
            Size size = block.Measure();
            return size.Height;
        }

        private RadFixedDocument GetDocumentFromTemplate(string fileName)
        {
            PdfFormatProvider provider = new PdfFormatProvider();
            RadFixedDocument document;
            using (Stream stream = File.OpenRead(fileName))
            {
                document = provider.Import(stream);
            }
            return document;
        }

        private void SaveDocumentAsPdf(RadFixedDocument document, string fileName)
        {
            PdfFormatProvider provider = new PdfFormatProvider();
            using (Stream output = File.OpenWrite(fileName))
            {
                provider.Export(document, output);
            }
        } 
        #endregion

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            Employees = new ObservableCollection<EmployeesForTravelExpenses>(dbAccess.GetAllEmployees());
            RaisePropertyChanged("Employees");
            SelectedEmployees = new ObservableCollection<EmployeesForTravelExpenses>();

            SelectedEmployees.CollectionChanged -= SelectedEmployees_CollectionChanged;
            SelectedEmployees.CollectionChanged += SelectedEmployees_CollectionChanged;

        }

        private void SelectedEmployees_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CanGeneratePdfFiles = SelectedEmployees.Count > 0;

            if (SelectedEmployees.Count ==0)
            {
                EmployeeDetailVisibility = Visibility.Hidden;
                SelectedEmployee = null;
                return;
            }
            SelectedEmployee = SelectedEmployees[0];
            EmployeeDetailVisibility = Visibility.Visible;

           
            ShowEmployeeDetails();

        }

        private void ShowEmployeeDetails()
        {
            salaryInformation = new EmployeeSalaryInformation(SelectedEmployee.Id);
            FormattedDateJoined = $"{salaryInformation.DateJoined:d}";
            if(salaryInformation.DateLeft == null)
            {
                FormattedDateLeft = "Vertrag ist unbefristet";
            }
            else
            {
                FormattedDateLeft = $"{(DateTime)salaryInformation.DateLeft:d}";
            }
            EmployeeSalaryDetails = new ObservableCollection<EmployeeSalaryDetail>(salaryInformation.GetMonthlySalaries());
            EmployeeAnnualDetails = new ObservableCollection<EmployeeSalaryDetail>(salaryInformation.GetAnnualSalaries());
            RaisePropertyChanged("EmployeeSalaryDetails");
            RaisePropertyChanged("EmployeeAnnualDetails");
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

    }
}
