using AccountingHelper.Core;
using AccountingHelper.DataAccess;
using AccountingHelper.Models;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using Telerik.Windows.Controls.GridView;
using Xceed;
using Xceed.Wpf.Toolkit;
using Prism.Interactivity.InteractionRequest;
using System.Windows;
using System.Diagnostics;
using Telerik.Reporting;
using AccountingHelper.Events;
using Prism.Events;

namespace AccountingHelper.ViewModels
{
    public class EditTravelExpenseViewModel: Qc_ViewModelBase
    {
        private TravelExpense travelExpense;
        public TravelExpense TravelExpense
        {
            get { return travelExpense; }
            set { SetProperty(ref travelExpense, value); }
        }
        public List<EmployeesForTravelExpenses> Employees { get; set; } = new List<EmployeesForTravelExpenses>();
        public ObservableCollection<DatevTransactionType> TransactionTypes { get; set; } = new ObservableCollection<DatevTransactionType>();
        private DatevTransactionType selectedTransactionType = new DatevTransactionType();

        private ObservableCollection<TravelExpenseItem> expenseItems = new ObservableCollection<TravelExpenseItem>();
        public ICollectionView ListOfTravelExpenseItems { get; set; }
        public ICollectionView ListOfTaxableItems { get; set; }
       

        private EmployeesForTravelExpenses selectedEmployee;

        public EmployeesForTravelExpenses SelectedEmployee
        {
            get { return selectedEmployee; }
            set { selectedEmployee = value;
                CanEditTravelExpenseItems = selectedEmployee != null ? true : false;
            }
        }

        private TravelExpenseItem currentTravelExpenseItem = null;
        private DateTime reportAsOf;
        public DateTime ReportAsOf
        {
            get { return reportAsOf; }
            set { SetProperty(ref reportAsOf, value); }
        }
        private bool receiptFileNameFound;
        public bool ReceiptFileNameFound
        {
            get { return receiptFileNameFound; }
            set { SetProperty(ref receiptFileNameFound, value); }
        }
        private bool acceptLowerTaxableAmount =false;
        public bool AcceptLowerTaxableAmount
        {
            get { return acceptLowerTaxableAmount; }
            set { SetProperty(ref acceptLowerTaxableAmount, value); }
        }
        private bool splitTotalAmount = true;
        public bool SplitTotalAmount
        {
            get { return splitTotalAmount; }
            set { SetProperty(ref splitTotalAmount, value); }

        }
        private bool canEditTaxableIncome;
        public bool CanEditTaxableIncome
        {
            get { return canEditTaxableIncome; }
            set { SetProperty(ref canEditTaxableIncome, value); }
        }
        private bool canSaveTaxableIncome;
        public bool CanSaveTaxableIncome
        {
            get { return canSaveTaxableIncome; }
            set { SetProperty(ref canSaveTaxableIncome, value); }
        }
        private bool canEditTravelExpenseItems;
        public bool CanEditTravelExpenseItems
        {
            get { return canEditTravelExpenseItems; }
            set { SetProperty(ref canEditTravelExpenseItems, value); }
        }
        private decimal amountToBeSplit;
        public decimal AmountToBeSplit
        {
            get { return amountToBeSplit; }
            set { SetProperty(ref amountToBeSplit, value); }
        }
        private int selectedEmployeeIndex;
        public int SelectedEmployeeIndex
        {
            get { return selectedEmployeeIndex; }
            set { SetProperty(ref selectedEmployeeIndex, value); }
        }
        private Visibility taxableIncomeWindowState = Visibility.Collapsed;
        public Visibility TaxableIncomeWindowState
        {
            get { return taxableIncomeWindowState; }
            set { SetProperty(ref taxableIncomeWindowState, value); }
        }

        private string travelExpenseRoot = Path.Combine(Properties.Settings.Default.RootDirectory, Properties.Settings.Default.TravelExpenseDirectory);
        private DbAccess dbAccess = new DbAccess();
        private readonly RegionManager regionManager;
        private readonly IEventAggregator eventAggregator;

        public ICommand SaveTravelExpenseCommand { get; set; }
        public ICommand AddingNewRowCommand { get; set; }
        public ICommand RowEditEndedCommand { get; set; }
        public ICommand CellValidatedCommand { get; set; }
        public ICommand EditTaxableIncomeCommand { get; set; }
        public ICommand SaveTaxableIncomeCommand { get; set; }
        public ICommand CancelTaxableIncomeCommand { get; set; }
        public ICommand AddingNewTaxableIncomeItemCommand { get; set; }
        public ICommand TaxableIncomeRowEditEndedCommand { get; set; }
        public ICommand BackToListCommand { get; set; }
        public ICommand SelectReceiptFileNameCommand { get; set; }
        public ICommand ViewReceiptsCommand { get; set; }
        public ICommand DeletedCommand { get; set; }
        public ICommand TaxableIncomeRowDeletedCommand { get; set; }
        public ICommand PrintExpenseCommand { get; set; }




        public EditTravelExpenseViewModel(RegionManager regionManager, IEventAggregator eventAggregator)
        {
            SaveTravelExpenseCommand = new DelegateCommand(OnSaveTravelExpense);
            AddingNewRowCommand = new DelegateCommand<object>(OnAddingNewRowCommand);
            CellValidatedCommand = new DelegateCommand(OnCellValidated);
            RowEditEndedCommand = new DelegateCommand(OnRowEditEnded);
            EditTaxableIncomeCommand = new DelegateCommand(OnEditTaxableIncome).ObservesCanExecute(() => CanEditTaxableIncome);
            SaveTaxableIncomeCommand = new DelegateCommand(OnSaveTaxableIncome).ObservesCanExecute(() => CanSaveTaxableIncome);
            CancelTaxableIncomeCommand = new DelegateCommand(OnCancelTaxableIncome);
            AddingNewTaxableIncomeItemCommand = new DelegateCommand<object>(OnAddingNewTaxableIncomeItem);
            TaxableIncomeRowEditEndedCommand = new DelegateCommand(OnTaxableIncomeRowEditEnded);
            TaxableIncomeRowDeletedCommand = new DelegateCommand(OnTaxableIncomeRowDeleted);
            ViewReceiptsCommand = new DelegateCommand(OnViewReceipts).ObservesCanExecute(() => ReceiptFileNameFound);
            PrintExpenseCommand = new DelegateCommand(OnPrintExpense);
            BackToListCommand = new DelegateCommand(OnBackToList);
            SelectReceiptFileNameCommand = new DelegateCommand(OnSelectReceiptFileName);
            DeletedCommand = new DelegateCommand(OnDeleted);
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;
            NotificationRequest = new InteractionRequest<INotification>();
            ConfirmationRequest = new InteractionRequest<IConfirmation>();
        }

        private void OnPrintExpense()
        {
            if (TravelExpense.Id ==0)
            {
                NotificationRequest.Raise(new Notification()
                {
                    Title = "QuantCo Deutschland GmbH",
                    Content = "Die Reisekosten müssen vor dem Druck erst gespeichert werden"
                });
                return;
            }

            NotificationRequest.Raise(new Notification()
            {
                Title = "QuantCo Deutschland GmbH",
                Content = "Nicht gespeicherte Änderungen werden nicht ausgedruckt"
            });

            List<int> idList = new List<int>();
            idList.Add(TravelExpense.Id);           
        
                TypeReportSource source = new TypeReportSource();
                source.TypeName = typeof(AccountingHelper.Reporting.TravelExpenses).AssemblyQualifiedName;
                source.Parameters.Add("TravelExpenseIds", idList);
                ViewerParameter parameter = new ViewerParameter()
                {
                    typeReportSource = source
                };
                eventAggregator.GetEvent<ViewerParameterEvent>().Publish(parameter);
          
        }

        private void OnTaxableIncomeRowDeleted()
        {
            decimal itemAmount = AmountToBeSplit;
            CanSaveTaxableIncome = false;
            // 
            if (SplitTotalAmount)
            {
                int nrOfItems = currentTravelExpenseItem.TaxableIncomeItems.Where(t => t.Status != DBStatus.Removed).Count();
                decimal equalAmount = Math.Round(itemAmount / nrOfItems, 2);
                decimal roundingDifference = itemAmount - (equalAmount * nrOfItems);
                foreach (TaxableIncomeItem item in currentTravelExpenseItem.TaxableIncomeItems)
                {
                    item.TaxableAmount = equalAmount;
                }
                // add roundingDifference to first item
                currentTravelExpenseItem.TaxableIncomeItems[0].TaxableAmount += roundingDifference;
            }

            foreach (TaxableIncomeItem item in currentTravelExpenseItem.TaxableIncomeItems)
            {
                if (item.Status == DBStatus.Removed) continue;
                itemAmount -= item.TaxableAmount;
            }

            if (itemAmount != 0)
            {
                if (AcceptLowerTaxableAmount) CanSaveTaxableIncome = true;
            }
            else
            {
                CanSaveTaxableIncome = true;
            }
        }

        private void OnDeleted()
        {
            decimal amount = 0;
            foreach(TravelExpenseItem expenseItem in TravelExpense.TravelExpenseItems)
            {
                amount += expenseItem.TotalAmount;
            }
            TravelExpense.TotalReimbursement = amount;
        }

        private void OnViewReceipts()
        {
            string fileName = Path.Combine(travelExpenseRoot, TravelExpense.ReportFileName);
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            p.Start();
        }

        private void OnSelectReceiptFileName()
        {
            Telerik.Windows.Controls.RadOpenFileDialog fileDialog = new Telerik.Windows.Controls.RadOpenFileDialog();
            fileDialog.Owner = Application.Current.MainWindow;
            fileDialog.InitialDirectory = travelExpenseRoot;
            fileDialog.DefaultExt = "pdf";
            fileDialog.Filter= "pdf files (*.pdf)|*.pdf";
            bool? result = fileDialog.ShowDialog();
            if (result == null) return;
            if (result == false) return;

            // ReportFileName only contains filename relative to travelExpenseRoot directory
            TravelExpense.ReportFileName = fileDialog.FileName.Replace(travelExpenseRoot+'\\', "");
            ReceiptFileNameFound = true;
        }

        private void OnBackToList()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.TravelExpenseList);
        }

        private void OnTaxableIncomeRowEditEnded()
        {
            decimal itemAmount = AmountToBeSplit;
            DatevTransactionType transactionType = dbAccess.GetTransactionById(currentTravelExpenseItem.DatevTransactionTypeId);
            TaxableIncomeItem currentTaxableIncomeItem = ListOfTaxableItems.CurrentItem as TaxableIncomeItem;
            if (currentTaxableIncomeItem == null) return;
            // if transactionTye.IsTaxableIncome AdjustGrossIncome must be false;
            if (transactionType.IsTaxableIncome) currentTaxableIncomeItem.AdjustGrossIncome = false;

            CanSaveTaxableIncome = false;
            // 
            if (SplitTotalAmount)
            {
                int nrOfItems = currentTravelExpenseItem.TaxableIncomeItems.Where(t => t.Status != DBStatus.Removed).Count();
                decimal equalAmount = Math.Round(itemAmount / nrOfItems, 2);
                decimal roundingDifference = itemAmount - (equalAmount * nrOfItems);
                foreach(TaxableIncomeItem item in currentTravelExpenseItem.TaxableIncomeItems)
                {
                    item.TaxableAmount = equalAmount;
                }
                // add roundingDifference to first item
                currentTravelExpenseItem.TaxableIncomeItems[0].TaxableAmount += roundingDifference;
            }

            foreach(TaxableIncomeItem item in currentTravelExpenseItem.TaxableIncomeItems)
            {
                if (item.Status == DBStatus.Removed) continue;
                itemAmount -= item.TaxableAmount;
               
            }

            if(itemAmount != 0)
            {
                if (AcceptLowerTaxableAmount) CanSaveTaxableIncome = true;
            }
            else
            {
                CanSaveTaxableIncome = true;
            }
        }

        private void OnAddingNewTaxableIncomeItem(object args)
        {
            GridViewAddingNewEventArgs e = args as GridViewAddingNewEventArgs;
            if (e == null) return;
            TaxableIncomeItem item = new TaxableIncomeItem();
            item.TravelExpenseItemId = currentTravelExpenseItem.Id;
            item.Description = currentTravelExpenseItem.Description;
            item.MonthAndYear= $"{ReportAsOf.ToString("MMMM yyyy")}";
            e.NewObject = item;
            CanSaveTaxableIncome = false;
        }

        private void OnCancelTaxableIncome()
        {
            TaxableIncomeWindowState = Visibility.Collapsed;
        }

        private void OnSaveTaxableIncome()
        {
            TaxableIncomeWindowState = Visibility.Collapsed;
        }

        private void OnEditTaxableIncome()
        {
            TravelExpenseItem item = ListOfTravelExpenseItems.CurrentItem as TravelExpenseItem;
            if (item == null) return;
            AmountToBeSplit = item.TotalAmount;
            ListOfTaxableItems = CollectionViewSource.GetDefaultView(item.TaxableIncomeItems);
            ListOfTaxableItems.CurrentChanged -= ListOfTaxableItems_CurrentChanged;
            ListOfTaxableItems.CurrentChanged += ListOfTaxableItems_CurrentChanged;
            RaisePropertyChanged("ListOfTaxableItems");
            CanSaveTaxableIncome = true;
            TaxableIncomeWindowState = Visibility.Visible;
            if (item.TaxableIncomeItems.Count>0)
            {
                AmountToBeSplit = 0;
                foreach(TaxableIncomeItem ti in item.TaxableIncomeItems)
                {
                    AmountToBeSplit += ti.TaxableAmount;
                }
            }

        }

        private void ListOfTaxableItems_CurrentChanged1(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnRowEditEnded()
        {
            TravelExpenseItem item = ListOfTravelExpenseItems.CurrentItem as TravelExpenseItem;
        
            if (item.IsTaxableIncome)
            {
                if (item.TaxableIncomeItems.Count == 0)
                {
                    item.TaxableIncomeItems.Add(new TaxableIncomeItem()
                    {
                        TravelExpenseItemId = item.Id,
                        Description = item.Description,
                        MonthAndYear = $"{ReportAsOf.ToString("MMMM yyyy")}",
                        EmployeeForTravelExpenseId = SelectedEmployee.Id,
                        TaxableAmount = item.TotalAmount, 
                        Status=DBStatus.Added
                    });
                }
            }
            else
            {
                item.TaxableIncomeItems.Clear();
            }            

            TravelExpense.TotalReimbursement = 0;
            foreach(TravelExpenseItem te in expenseItems)
            {
                TravelExpense.TotalReimbursement += te.TotalAmount;
            }
            if (item.TotalAmount == 0) return;
            if (string.IsNullOrEmpty(item.Description)) item.Description = selectedTransactionType.TransactionType;
        }

        private void OnCellValidated()
        {
  
            TravelExpenseItem item = ListOfTravelExpenseItems.CurrentItem as TravelExpenseItem;
            if (item == null) return;
            if (selectedTransactionType == null)
            {
                if (item.DatevTransactionTypeId>0) selectedTransactionType = dbAccess.GetTransactionById(item.DatevTransactionTypeId);
                if (selectedTransactionType != null) item.IsTaxableIncome = selectedTransactionType.IsTaxableIncome;
            }
            else
            {
                if (item.DatevTransactionTypeId > 0 && selectedTransactionType.Id != item.DatevTransactionTypeId)
                {
                    selectedTransactionType = dbAccess.GetTransactionById(item.DatevTransactionTypeId);
                    item.IsTaxableIncome = selectedTransactionType.IsTaxableIncome;

                }
            }
            if (string.IsNullOrEmpty(item.Description) && selectedTransactionType != null) item.Description = selectedTransactionType.TransactionType;
            item.TotalAmount = item.Amount00 + item.Amount07 + item.Amount19;
            if (item.Status == DBStatus.Unchanged) item.Status = DBStatus.Updated;
            CanEditTaxableIncome = item.TotalAmount != 0 ? item.IsTaxableIncome : false;
        }

        private string GenerateReceiptFileName(TravelExpense travelExpense)
        {
            string formattedDate = $"{ReportAsOf:yyyy}{ReportAsOf:MM}";
            string fileName = $"{formattedDate} {SelectedEmployee.FullName} Belege.pdf";
            string dir = $"Reisekosten {ReportAsOf.Year.ToString()}";
            string path = Path.Combine(Properties.Settings.Default.RootDirectory, Properties.Settings.Default.TravelExpenseDirectory, dir, fileName);
            return path;
        }

        private void OnAddingNewRowCommand(object obj)
        {
            GridViewAddingNewEventArgs e = obj as GridViewAddingNewEventArgs;
            if (e == null) return;

            TravelExpenseItem item = new TravelExpenseItem();
            item.Status = DBStatus.Added;
            item.TravelExpenseId = TravelExpense.Id;
            e.NewObject = item;
            //this will ensure selectedTransactionType is newly set in OnCellValidated
            selectedTransactionType = null;
        }

        public void OnSaveTravelExpense()
        {
            // remove travelexpenseitem in case of amount00 and amount07 and amount19 are o

            for(int i =0; i<TravelExpense.TravelExpenseItems.Count;i++)
            {
                TravelExpenseItem item = travelExpense.TravelExpenseItems.ElementAt(i);
                if (item.Amount00 ==0 && item.Amount07 ==0 && item.Amount19==0)
                {
                    TravelExpense.TravelExpenseItems.Remove(item);
                }
            }
            
            TravelExpense.EmployeeForTravelExpensesId = SelectedEmployee.Id;
            TravelExpense.EmployeeName = SelectedEmployee.FullName;
            TravelExpense.MonthAndYear = $"{ReportAsOf.ToString("MMMM yyyy")}";
            TravelExpense.TravelExpenseItems = expenseItems.ToList();
            TravelExpense.ExpenseDate = new DateTime(ReportAsOf.Year, ReportAsOf.Month, 1);
            if (TravelExpense.Id ==0)
            {
                TravelExpense = dbAccess.InsertTravelExpense(TravelExpense);
            }
            else
            {
                TravelExpense updated = dbAccess.UpdateTravelExpense(TravelExpense);
                if (updated == null)
                {
                    NotificationRequest.Raise(new Notification()
                    {
                        Title = "QuantCo Deutschland GmbH",
                        Content = "Fehler beim Ändern der Reisekosten"
                    });
                }
            }
           
        }

        public override void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
           if (!ReceiptFileNameFound)
            {
                ConfirmationRequest.Raise(new Confirmation()
                {
                    Title = "QuantCo Deutschland GmbH",
                    Content = "Es wurde keine gültige Beleg-Datei angegeben. Wollen Sie darauf verzichten?"
                }, response =>
                {
                    if (!response.Confirmed)
                        continuationCallback(false);
                });
            }
            continuationCallback(true);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            SelectedEmployeeIndex = -1;
           
                Employees = dbAccess.GetAllEmployees();
                Employees = Employees.OrderBy(e => e.FullName).ToList();
                RaisePropertyChanged("Employees");
          
                TransactionTypes = new ObservableCollection<DatevTransactionType>(dbAccess.GetAllTransactionTypes());
                RaisePropertyChanged("TransactionTypes");
           
            TravelExpense t = navigationContext.Parameters["TravelExpense"] as TravelExpense;
            if (t.Id>0)
            {
                TravelExpense = dbAccess.GetTravelExpenseById(t.Id);
                if (TravelExpense != null)
                {
                    expenseItems = new ObservableCollection<TravelExpenseItem>(TravelExpense.TravelExpenseItems);
                    if (expenseItems.Count > 0)
                    {
                        currentTravelExpenseItem = expenseItems.ElementAt(0);
                        CanEditTaxableIncome = currentTravelExpenseItem.IsTaxableIncome;
                    }
                    else
                    { currentTravelExpenseItem = null; }
                    
                    ReportAsOf = TravelExpense.ExpenseDate;
                    SelectedEmployee = Employees.FirstOrDefault(e => e.Id == TravelExpense.EmployeeForTravelExpensesId);
                    SelectedEmployeeIndex =Employees.IndexOf(SelectedEmployee);
                    RaisePropertyChanged("SelectedEmployee");
                    // check, whether ReceiptFile is still available
                    if(!string.IsNullOrEmpty(TravelExpense.ReportFileName))
                    {
                        if (File.Exists(Path.Combine(travelExpenseRoot,TravelExpense.ReportFileName))) ReceiptFileNameFound = true; else ReceiptFileNameFound = false;
                        if (!ReceiptFileNameFound)
                        {
                           
                                    NotificationRequest.Raise(new Notification()
                                    {
                                        Title = "QuantCo Deutschland GmbH",
                                        Content = "Die angegebene Datei existiert nicht. Sie wurde entweder gelöscht oder verschoben"
                                    });                             
                        }
                    }
                    else
                    {
                        ReceiptFileNameFound = false;
                       
                       
                                    NotificationRequest.Raise(new Notification()
                                    {
                                        Title = "QuantCo Deutschland GmbH",
                                        Content = "Es wurde keine Beleg-Datei angegeben"
                                    });
                     
                    }    
                }
            }
            else
            {
                TravelExpense = new TravelExpense();
                expenseItems = new ObservableCollection<TravelExpenseItem>();
                currentTravelExpenseItem = null;
                ReceiptFileNameFound = false;
                ReportAsOf = DateTime.Now.Date;
                ReportAsOf = ReportAsOf.AddMonths(-1);
            }

            ListOfTravelExpenseItems = CollectionViewSource.GetDefaultView(expenseItems);
            ListOfTravelExpenseItems.CurrentChanged -= ListOfTravelExpenseItems_CurrentChanged;
            ListOfTravelExpenseItems.CurrentChanged += ListOfTravelExpenseItems_CurrentChanged;

            // set Filter: do not show deletedItems
            ListOfTravelExpenseItems.Filter = item => 
                { TravelExpenseItem te = item as TravelExpenseItem;
                    if (te == null) return true;
                    return te.Status != DBStatus.Removed;
                };
            RaisePropertyChanged("ListOfTravelExpenseItems");          
        }

       
     
        private void ListOfTravelExpenseItems_CurrentChanged(object sender, EventArgs e)
        {
            currentTravelExpenseItem = ListOfTravelExpenseItems.CurrentItem as TravelExpenseItem;
            if (currentTravelExpenseItem == null) return;
            if (currentTravelExpenseItem.TravelExpenseId !=0)  selectedTransactionType = dbAccess.GetTransactionById(currentTravelExpenseItem.DatevTransactionTypeId);           
            CanEditTaxableIncome = currentTravelExpenseItem.IsTaxableIncome;

            // set new ListOfTaxableItems
            ListOfTaxableItems = CollectionViewSource.GetDefaultView(currentTravelExpenseItem.TaxableIncomeItems);
            ListOfTaxableItems.CurrentChanged -= ListOfTaxableItems_CurrentChanged;
            ListOfTaxableItems.CurrentChanged += ListOfTaxableItems_CurrentChanged;
            RaisePropertyChanged("ListOfTaxableItems");
        }

        private void ListOfTaxableItems_CurrentChanged(object sender, EventArgs e)
        {
            decimal amount = Math.Round(currentTravelExpenseItem.TotalAmount / currentTravelExpenseItem.TaxableIncomeItems.Count, 2);
        }
    }
}
