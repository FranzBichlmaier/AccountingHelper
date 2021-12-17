using AccountingHelper.Core;
using AccountingHelper.DataAccess;
using AccountingHelper.Models;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace AccountingHelper.ViewModels
{


    public class EmployeeDocumentViewModel: Qc_ViewModelBase
    {
        ObservableCollection<EmployeesForTravelExpenses> employees = new ObservableCollection<EmployeesForTravelExpenses>();
        public ICollectionView Employees { get; set; }
        ObservableCollection<EmployeeDocument> employeeDocuments = new ObservableCollection<EmployeeDocument>();
        public ICollectionView ListOfDocuments { get; set; }
        private EmployeeDocument selectedDocument = null;
        public EmployeeDocument SelectedDocument
        {
            get { return selectedDocument; }
            set { SetProperty(ref selectedDocument, value); }
        }
        private string selectedFileName =  string.Empty;
        public string SelectedFileName
        {
            get { return selectedFileName; }
            set { SetProperty(ref selectedFileName, value); }
        }
        private bool canUserAddItems = false;
        public bool CanUserAddItems
        {
            get { return canUserAddItems; }
            set { SetProperty(ref canUserAddItems, value); }
        }

        public ICommand AddDocumentCommand { get; set; }
        public ICommand PrepareEmailCommand { get; set; }
        public ICommand SaveDocumentCommand { get; set; }
        public ICommand BrowseFilesCommand { get; set; }
        public ICommand RowEditEndedCommand { get; set; }

        DbAccess dbAccess = new DbAccess();
        EmployeesForTravelExpenses selectedEmployee = new EmployeesForTravelExpenses();
        string documentRoot = Path.Combine(Properties.Settings.Default.RootDirectory, Properties.Settings.Default.EmployeeDocuments);
        string employeeDirectory = string.Empty;
        string fileNameOpend = string.Empty;
        string lastDirectory = Properties.Settings.Default.RootDirectory;
        private bool hasBeenCalled = true;
        CreateDynamicParameters createDynamicParameters = new CreateDynamicParameters();
        public EmployeeDocumentViewModel()
        {
            AddDocumentCommand = new DelegateCommand(OnAddDocument).ObservesCanExecute(() => CanUserAddItems);
            PrepareEmailCommand = new DelegateCommand(OnPrepareEmail);
            SaveDocumentCommand = new DelegateCommand(OnSaveDocument);
            BrowseFilesCommand = new DelegateCommand(OnBrowseFiles);
            RowEditEndedCommand = new DelegateCommand(OnRowEditEnded);
        }


        private void OnRowEditEnded()
        {
            EmployeeDocument employeeDocument = ListOfDocuments.CurrentItem as EmployeeDocument;

            if (employeeDocument.Id ==0)
            {
                //insert
                employeeDocument = dbAccess.InsertEmployeeDocument(employeeDocument);
                employeeDocument.DocumentFileName = createFileName(employeeDocument.Id);
                dbAccess.UpdateEmployeeDocument(employeeDocument);
            }
            else
            {
                dbAccess.UpdateEmployeeDocument(employeeDocument);
            }
        }

        private string createFileName(int id)
        {
            if (selectedEmployee!= null)
            {    
                string name = $"{selectedEmployee.FullName} {id:000000}.pdf";
                File.Copy(SelectedFileName, Path.Combine(employeeDirectory, name), true);               
                return name;
            }
            return string.Empty;
        }

        private void OnBrowseFiles()
        {
            throw new NotImplementedException();
        }

        private void OnSaveDocument()
        {
            throw new NotImplementedException();
        }

        private void OnPrepareEmail()
        {
            throw new NotImplementedException();
        }

        private void OnAddDocument()
        {
            
            Telerik.Windows.Controls.RadOpenFileDialog fileDialog = new Telerik.Windows.Controls.RadOpenFileDialog();
            fileDialog.Owner = Application.Current.MainWindow;
            fileDialog.InitialDirectory = lastDirectory;
            fileDialog.RestoreDirectory = true;
            fileDialog.DefaultExt = "pdf";
            fileDialog.Filter = "pdf files (*.pdf)|*.pdf";
            bool? result = fileDialog.ShowDialog();
            if (result == null) return;
            if (result == false) return;

            // set the selected Directory as InitialDirectory for the next open
            FileInfo fileInfo = new FileInfo(fileDialog.FileName);
            lastDirectory = fileInfo.DirectoryName;

            // ReportFileName only contains filename relative to travelExpenseRoot directory
            SelectedFileName = fileDialog.FileName;
            
            employeeDocuments.Add(new EmployeeDocument           
            {
                DateValidUntil = null,
                DocumentContentDescription = string.Empty,
                DocumentType = DocumentTypes.Contract,
                DocumentFileName="",
                Employee = selectedEmployee,
                EmployeeId = selectedEmployee.Id
            });
            // Activate added item
            ListOfDocuments.MoveCurrentToLast();
            RaisePropertyChanged("ListOfDocuments");
            RaisePropertyChanged("SelectedDocument");
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            hasBeenCalled = true;
            CanUserAddItems = false;
            employees.Clear();
            employees = new ObservableCollection<EmployeesForTravelExpenses>( dbAccess.GetAllEmployees());
            Employees = CollectionViewSource.GetDefaultView(employees);
            Employees.CurrentChanged -= Employees_CurrentChanged;
            Employees.CurrentChanged += Employees_CurrentChanged;
            RaisePropertyChanged("Employees");
            hasBeenCalled = false;
        }

        private void Employees_CurrentChanged(object sender, EventArgs e)
        {
            if (hasBeenCalled) return;
            CanUserAddItems = true;
            selectedEmployee = Employees.CurrentItem as EmployeesForTravelExpenses;
            employeeDirectory = Path.Combine(documentRoot, selectedEmployee.FullName);
            Directory.CreateDirectory(employeeDirectory);
            employeeDocuments = new ObservableCollection<EmployeeDocument>(dbAccess.GetAllEmployeeDocumentsForEmployeeId(selectedEmployee.Id));
            ListOfDocuments = CollectionViewSource.GetDefaultView(employeeDocuments);
            ListOfDocuments.CurrentChanged -= ListOfDocuments_CurrentChanged;
            ListOfDocuments.CurrentChanged += ListOfDocuments_CurrentChanged;
            RaisePropertyChanged("ListOfDocuments");
            if (ListOfDocuments.CurrentItem != null)
            {
                SelectedDocument = ListOfDocuments.CurrentItem as EmployeeDocument;
                FileInfo fileInfo = new FileInfo(Path.Combine(employeeDirectory, SelectedDocument.DocumentFileName));
                if (fileInfo.Exists)
                {
                    SelectedFileName = fileInfo.FullName;
                }
                else
                {
                    NotificationRequest.Raise(new Notification
                    {
                        Title = "QuantCo Deutschland GmbH",
                        Content = $"Die Datei {fileInfo.FullName} wurde entweder gelöscht oder verschoben"
                    });
                    SelectedFileName = null;
                }
            }
                
            if (employeeDocuments.Count == 0) OnAddDocument();
            RaisePropertyChanged("SelectedFileName");
        }

        private void ListOfDocuments_CurrentChanged(object sender, EventArgs e)
        {
            EmployeeDocument document = ListOfDocuments.CurrentItem as EmployeeDocument;
            if (!string.IsNullOrEmpty(document.DocumentFileName))
            {
                FileInfo fileInfo = new FileInfo(Path.Combine(employeeDirectory, document.DocumentFileName));
                if (fileInfo.Exists)
                {
                    SelectedFileName = fileInfo.FullName;
                }
                else
                {
                    NotificationRequest.Raise(new Notification
                    {
                        Title = "QuantCo Deutschland GmbH",
                        Content = $"Die Datei {fileInfo.FullName} wurde entweder gelöscht oder verschoben"
                    });
                    SelectedFileName = null;
                }                
            }
            else
            {
                //SelectedFileName = "";
            }
        }
    }
}
