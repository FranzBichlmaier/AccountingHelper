using AccountingHelper.Core;
using AccountingHelper.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccountingHelper.DataAccess;
using System.Collections.ObjectModel;
using AccountingHelper.Models;
using Telerik.Reporting;

namespace AccountingHelper.ViewModels
{
    public class ShellViewModel: Qc_ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly RegionManager regionManager;
        private readonly IEventAggregator eventAggregator;
        private string testVersionHinweis = string.Empty;

        public string TestVersionHinweis
        {
            get { return testVersionHinweis; }
            set { SetProperty(ref testVersionHinweis, value); }
        }

        public ICommand TransactionTypesCommand { get; private set; }
        public ICommand EmployeeCommand { get; private set; }
        public ICommand EmployeeDocumentsCommand { get; set; }
        public ICommand EmployeeSalaryCommand { get; set; }
        public ICommand InputTravelExpensesCommand { get; private set; }
        public ICommand ShellLoadedCommand { get; private set; }
        public ICommand UseTestDatabaseCommand { get; private set; }
        public ICommand UseProductionDatabaseCommand { get; private set; }
        public ICommand SalaryOverviewCommand { get; private set; }
        public ICommand EmployeeOverviewCommand { get; private set; }
        public ICommand BranchesCommand { get; private set; }
        public ICommand EmployeeOfficeCommand { get; private set; }
        public ICommand PrepareForBDOCommand { get; private set; }
        public ICommand NavigationViewItemClicked { get; private set; }
        public ICommand EmployeeContractCommand { get; private set; }
        public ICommand ContractBonusCommand { get; set; }
        private bool databaseToggleState =true;
        public bool DatabaseToggleState
        {
            get { return databaseToggleState; }
            set { SetProperty(ref databaseToggleState, value); }
        }
        //public ICommand CollectTaxableIncomeCommand { get; private set; }

        private ObservableCollection<NavigationItem> navigationItems;
        public ObservableCollection<NavigationItem> NavigationItems
        {
            get { return navigationItems; }
            set { SetProperty(ref navigationItems, value); }
        }
        private NavigationItem selectedNavigationItem;
        public NavigationItem SelectedNavigationItem
        {
            get { return selectedNavigationItem; }
            set { SetProperty(ref selectedNavigationItem, value); }
        }
        private string selectedNavigationValue;
        public string SelectedNavigationValue
        {
            get { return selectedNavigationValue; }
            set { SetProperty(ref selectedNavigationValue, value); }
        }

        public ShellViewModel(RegionManager regionManager, IEventAggregator eventAggregator)
        {
            log.Debug("this is my first log message");
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;
           
           
            TransactionTypesCommand = new DelegateCommand(OnTransactionTypes);
            EmployeeCommand = new DelegateCommand(OnEmployee);
            EmployeeDocumentsCommand = new DelegateCommand(OnEmployeeDocuments);
            EmployeeSalaryCommand = new DelegateCommand(OnEmployeeSalary);
            InputTravelExpensesCommand = new DelegateCommand(OnInputTravelExpenses);
            ShellLoadedCommand = new DelegateCommand(OnShellLoaded);
            BranchesCommand = new DelegateCommand(OnBranches);
            EmployeeOfficeCommand = new DelegateCommand(OnEmployeeOffice);
            UseProductionDatabaseCommand = new DelegateCommand(OnUseProductionDatabase);
            UseTestDatabaseCommand = new DelegateCommand(OnUseTestDatabase);
            SalaryOverviewCommand = new DelegateCommand(OnSalaryOverview);
            EmployeeOverviewCommand = new DelegateCommand(OnEmployeeOverview);
            PrepareForBDOCommand = new DelegateCommand(OnPrepareForBDO);
            ContractBonusCommand = new DelegateCommand(OnContractBonus);
            EmployeeContractCommand = new DelegateCommand(OnCreateEmployeeContract);

            NavigationViewItemClicked = new DelegateCommand<object>(OnNavigationViewItemClicked);
            //CollectTaxableIncomeCommand = new DelegateCommand(OnCollectTaxableIncome);

            eventAggregator.GetEvent<ViewerParameterEvent>().Subscribe(OnShowReportViewer);


            if (Properties.Settings.Default.IsTest)
            { OnUseTestDatabase(); }
            else
            {
                OnUseProductionDatabase();
            }
            DatabaseToggleState = Properties.Settings.Default.IsTest;

        }

        private void OnCreateEmployeeContract()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.CreateEmployeeContract);
        }

        private void OnContractBonus()
        {
            TypeReportSource source = new TypeReportSource();
            source.TypeName = typeof(AccountingHelper.Reporting.BonusYear).AssemblyQualifiedName;           
            ViewerParameter parameter = new ViewerParameter()
            {
                typeReportSource = source
            };
            eventAggregator.GetEvent<ViewerParameterEvent>().Publish(parameter);
        }

        private void FillNavigationItems()
        {
            NavigationItems = new ObservableCollection<NavigationItem>();
            NavigationItem employeeItem = new NavigationItem()           
            {
                NavigationTitle = "Mitarbeiter",
                ViewName = ViewNames.EmployeeView, 
                IsSelectable=false
               
            };
            employeeItem.NavigationSubItems.Add(new NavigationItem()
            {
                NavigationTitle = "Mitarbeiter verwalten",
                ViewName = ViewNames.EmployeeOverview
            });
            employeeItem.NavigationSubItems.Add(new NavigationItem()
            {
                NavigationTitle = "Dokumente verwalten",
                ViewName = ViewNames.EmployeeDocument
            });
            employeeItem.NavigationSubItems.Add(new NavigationItem()
            {
                NavigationTitle = "Liste 1. Tätigkeitsstätte",
                ViewName = ViewNames.EmployeeOffice
            });
            NavigationItems.Add(employeeItem);

        }

        private void OnNavigationViewItemClicked(object obj)
        {
            System.Windows.Controls.SelectionChangedEventArgs e = obj as System.Windows.Controls.SelectionChangedEventArgs;
            if (e == null) return;
            if (e.AddedItems.Count == 0) return;

            SelectedNavigationItem = e.AddedItems[0] as NavigationItem;
            if (SelectedNavigationItem == null) return;

            regionManager.RequestNavigate(RegionNames.MainRegion, SelectedNavigationItem.ViewName);

        }

        private void OnEmployeeOverview()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.EmployeeOverview);
        }

        private void OnPrepareForBDO()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.PrepareForBDO);
        }

        private void OnSalaryOverview()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.SalaryOverview);
        }

        private void OnEmployeeOffice()
        {
            // start UserControl EmployeeOffice
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.EmployeeOffice);
        }

        private void OnBranches()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.QuantCoBranches);
        }

        private void OnEmployeeSalary()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.EmployeeSalaries);
        }

        private void OnEmployeeDocuments()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.EmployeeDocument);
        }

        private void OnUseTestDatabase()
        {
            ConnectionHelper.CNNTest();
            ConnectionHelper.SaveConnectionString();
            TestVersionHinweis = "Test-Datenbank";
            Properties.Settings.Default.IsTest = true;
            Properties.Settings.Default.Save();
        }

        private void OnUseProductionDatabase()
        {
            ConnectionHelper.CNNProduction();
            ConnectionHelper.SaveConnectionString();
            TestVersionHinweis = "Produktion";
            Properties.Settings.Default.IsTest = false;
            Properties.Settings.Default.Save();
        }

        //private void OnCollectTaxableIncome()
        //{
        //    regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.CollectTaxableIncome);
        //}

        private void OnShellLoaded()
        {
            FillNavigationItems();
            RaisePropertyChanged("NavigationItems");

            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.TravelExpenseList);
        }

        private void OnShowReportViewer(ViewerParameter obj)
        {
            ViewerParameter source = obj;

            if (source.reportBook != null)
            {
                Windows.Window_ReportViewer window = new Windows.Window_ReportViewer(source.reportBook);
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }

            else
            {
                Windows.Window_ReportViewer window = new Windows.Window_ReportViewer(source.typeReportSource);
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
        }

        private void OnInputTravelExpenses()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.TravelExpenseList);
        }

        private void OnEmployee()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.EmployeeView);
        }

        private void OnTransactionTypes()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.DatevTransactionView);
        }

       
    }
}
