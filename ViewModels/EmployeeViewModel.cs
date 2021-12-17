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
using System.Windows.Data;
using System.Windows.Input;
using Telerik.Windows.Controls.GridView;

namespace AccountingHelper.ViewModels
{
    public class EmployeeViewModel: Qc_ViewModelBase
    {
        private ObservableCollection<EmployeesForTravelExpenses> employees;
        private ObservableCollection<EmployeeBranchRelation> listOfBranches;
        private DbAccess dataAccess = new DbAccess();
        private EmployeesForTravelExpenses selectedItem = new EmployeesForTravelExpenses();
        private EmployeeBranchRelation selectedBranch = new EmployeeBranchRelation();
        private EmployeeBranchEdit previousBranch = new EmployeeBranchEdit();


        private ObservableCollection<QuantCoBranch> quantCoBranches = new ObservableCollection<QuantCoBranch>();
        public ObservableCollection<QuantCoBranch> QuantCoBranches
        {
            get { return quantCoBranches; }
            set { SetProperty(ref quantCoBranches, value); }
        }
        private bool canSelectBranch = false;
        public bool CanSelectBranch
        {
            get { return canSelectBranch; }
            set { SetProperty(ref canSelectBranch, value); }
        }
        public ICollectionView ListOfEmployees { get; set; }
        public ICollectionView EmployeeBranches { get; set; }
        public ICommand AddedNewRowCommand { get; private set; }
        public ICommand RowDeletingCommand { get; private set; }
        public ICommand RowEditEndedCommand { get; private set; }
        public ICommand AddedNewBranchCommand { get; private set; }
        public ICommand BranchDeletingCommand { get; private set; }
        public ICommand BranchEditEndedCommand { get; private set; }

        public EmployeeViewModel()
        {
            AddedNewRowCommand = new DelegateCommand<object>(OnAddedNewRow);
            RowDeletingCommand = new DelegateCommand(OnRowDeleting);
            RowEditEndedCommand = new DelegateCommand(OnRowEditEnded);
            AddedNewBranchCommand = new DelegateCommand<object>(OnAddedNewBranch);
            BranchDeletingCommand = new DelegateCommand(OnBranchDeleting);
            BranchEditEndedCommand = new DelegateCommand(OnBranchEditEnded);
        }

        private void OnBranchEditEnded()
        {
            if (selectedBranch.Id == 0)
            {
                EmployeeBranchRelation e = dataAccess.InserEmployeeBranchRelation(selectedBranch);
                if (e == null)
                {
                    // Fehler
                }
                else
                {
                    selectedBranch.Id = e.Id;
                    if (selectedBranch.QuantCoBranchId != selectedItem.QuantCoBranchId)
                    {
                        selectedItem.QuantCoBranchId = selectedBranch.QuantCoBranchId;
                        dataAccess.UpdateEmployee(selectedItem);
                    }
                    UpdatePreviousBranch();
                    return;
                }
            }
            else
            {
                EmployeeBranchRelation e = dataAccess.UpdateEmployeeBranch(selectedBranch);
                if (e == null)
                {
                    // Fehler
                }
                if (selectedBranch.QuantCoBranchId != selectedItem.QuantCoBranchId)
                {
                    selectedItem.QuantCoBranchId = selectedBranch.QuantCoBranchId;
                    dataAccess.UpdateEmployee(selectedItem);
                }
            }
        }

        private void OnBranchDeleting()
        {
            if (dataAccess.RemoveEmployeeBranch(selectedBranch)) return;
        }

        private void OnAddedNewBranch(object obj)
        {
            GridViewAddingNewEventArgs e = obj as GridViewAddingNewEventArgs;
            if (e == null) return;

            EmployeeBranches.MoveCurrentToFirst();
            previousBranch = EmployeeBranches.CurrentItem as EmployeeBranchEdit;

            e.NewObject = new EmployeeBranchEdit
            {
                EmployeeId = selectedItem.Id,
                QuantCoBranchId = 1,
                ValidFrom = new DateTime(DateTime.Now.Year,DateTime.Now.Month,1),
                IsReadOnly=false
            };
        }

        private void OnRowEditEnded()
        {
            if (selectedItem.Id == 0)
            {
                EmployeesForTravelExpenses e = dataAccess.InsertEmployee(selectedItem);
                if (e == null)
                {
                    // Fehler
                }
                else
                {
                    selectedItem.Id = e.Id;

                    if (selectedItem.QuantCoBranchId >0)
                    {
                        EmployeeBranchRelation relation= new EmployeeBranchRelation
                        {
                            EmployeeId = selectedItem.Id,
                            QuantCoBranchId = selectedItem.QuantCoBranchId,
                            ValidFrom = new DateTime(DateTime.Now.Year, 1, 1)
                        };
                        dataAccess.InserEmployeeBranchRelation(relation);
                    }
                    CanSelectBranch = false;
                    
                    return;
                }
            }
            else
            {
                if(selectedBranch.QuantCoBranchId>0) selectedItem.QuantCoBranchId = selectedBranch.QuantCoBranchId;
                EmployeesForTravelExpenses e = dataAccess.UpdateEmployee(selectedItem);
                if (e == null)
                {
                    // Fehler
                }
            }
           
        }

        private void UpdatePreviousBranch()
        {
            if (previousBranch == null) return;

            previousBranch.ValidUntil = selectedBranch.ValidFrom.AddDays(-1);
            previousBranch.IsReadOnly = true;
            dataAccess.UpdateEmployeeBranch(previousBranch);
            previousBranch = null;
        }

        private void OnRowDeleting()
        {
            if (dataAccess.RemoveEmployee(selectedItem)) return;
            // Fehler
        }

        private void OnAddedNewRow(object obj)
        {
            GridViewAddingNewEventArgs e = obj as GridViewAddingNewEventArgs;
            if (e == null) return;
            e.NewObject = new EmployeesForTravelExpenses();
            CanSelectBranch = true;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            employees = new ObservableCollection<EmployeesForTravelExpenses>(dataAccess.GetAllEmployees());
            if (employees.Count == 0) employees.Add(new EmployeesForTravelExpenses());

            ListOfEmployees = CollectionViewSource.GetDefaultView(employees);
            ListOfEmployees.CurrentChanged -= ListOfEmployees_CurrentChanged;
            ListOfEmployees.CurrentChanged += ListOfEmployees_CurrentChanged;
            selectedItem = ListOfEmployees.CurrentItem as EmployeesForTravelExpenses;

            QuantCoBranches = new ObservableCollection<QuantCoBranch>(dataAccess.GetAllQuantCoBranches());


            RaisePropertyChanged("QuantCoBranches");
            RaisePropertyChanged("ListOfEmployees");
            UpdateEmployeeBranches();
            ReadEmployeeBranches();

        }

        /// <summary>
        /// This Method adds records into EmployeeBranches if the databasetable is empty
        /// Foreach Employee a record is added if there is an entry in QuantCoBranchId
        /// </summary>
        private void UpdateEmployeeBranches()
        {
            List<EmployeeBranchRelation> branches = new List<EmployeeBranchRelation>();
            branches = dataAccess.GetAllEmployeeBranchesForEmployeeId(selectedItem.Id);
            if (branches.Count > 0) return;
            foreach (EmployeesForTravelExpenses employee in employees)
            {
                branches = new List<EmployeeBranchRelation>(dataAccess.GetAllEmployeeBranchesForEmployeeId(selectedItem.Id));
                if (branches.Count == 0 && employee.QuantCoBranchId > 0)
                {
                    // there is no record ==> insert record
                    EmployeeBranchRelation relation = new EmployeeBranchRelation
                    {
                        EmployeeId = selectedItem.Id,
                        QuantCoBranchId = selectedItem.QuantCoBranchId,
                        ValidFrom = new DateTime(DateTime.Now.Year, 1, 1),
                        ValidUntil = null
                    };
                    dataAccess.InserEmployeeBranchRelation(relation);                    
                }               
            }
        }

        private void ListOfEmployees_CurrentChanged(object sender, EventArgs e)
        {
            selectedItem = ListOfEmployees.CurrentItem as EmployeesForTravelExpenses;

            // read history of branch relations
            // if there is no record insert record

            ReadEmployeeBranches();

        }

        private List<EmployeeBranchEdit> ConvertToEditList(ObservableCollection<EmployeeBranchRelation> relationList)
        {
            List<EmployeeBranchEdit> returnList = new List<EmployeeBranchEdit>();
            bool isReadOnly = false;
            foreach(EmployeeBranchRelation item in relationList)
            {
                returnList.Add(new EmployeeBranchEdit
                {
                    Id = item.Id,
                    EmployeeId = item.EmployeeId,
                    QuantCoBranchId = item.QuantCoBranchId,
                    ValidFrom = item.ValidFrom,
                    ValidUntil = item.ValidUntil,
                    IsReadOnly = isReadOnly
                });
                isReadOnly = true;
            }
            return returnList;
        }

        private void ReadEmployeeBranches()
        {
            if (selectedItem == null)
            {
                listOfBranches = new ObservableCollection<EmployeeBranchRelation>();
                EmployeeBranches = CollectionViewSource.GetDefaultView(ConvertToEditList(listOfBranches));
                RaisePropertyChanged("EmployeeBranches");
                return;
            }
            listOfBranches = new ObservableCollection<EmployeeBranchRelation>(dataAccess.GetAllEmployeeBranchesForEmployeeId(selectedItem.Id));
            if (listOfBranches.Count == 0 && selectedItem.QuantCoBranchId > 0)
            {
                // there is no record ==> insert record
                EmployeeBranchRelation relation = new EmployeeBranchRelation
                {
                    EmployeeId = selectedItem.Id,
                    QuantCoBranchId = selectedItem.QuantCoBranchId,
                    ValidFrom = new DateTime(DateTime.Now.Year, 1, 1),
                    ValidUntil = null
                };
                dataAccess.InserEmployeeBranchRelation(relation);
                listOfBranches.Add(relation);
            }
            if (listOfBranches.Count == 0 && selectedItem.QuantCoBranchId == 0)
            {
                // there is no record ==> insert record
                EmployeeBranchRelation relation = new EmployeeBranchRelation
                {
                    EmployeeId = selectedItem.Id,
                    QuantCoBranchId = 1,
                    ValidFrom = new DateTime(DateTime.Now.Year, 1, 1),
                    ValidUntil = null
                };
                dataAccess.InserEmployeeBranchRelation(relation);
                listOfBranches.Add(relation);
            }
            EmployeeBranches = CollectionViewSource.GetDefaultView(ConvertToEditList( listOfBranches));
            // Sort EmployeeBranches
            EmployeeBranches.SortDescriptions.Add(new SortDescription
            {
                Direction = ListSortDirection.Descending,
                PropertyName = "ValidFrom"
            });
            EmployeeBranches.CurrentChanged -= EmployeeBranches_CurrentChanged;
            EmployeeBranches.CurrentChanged += EmployeeBranches_CurrentChanged;

            selectedBranch = EmployeeBranches.CurrentItem as EmployeeBranchEdit;
           
            selectedItem.QuantCoBranchId = selectedBranch.QuantCoBranchId;
            RaisePropertyChanged("EmployeeBranches");
        }

        private void EmployeeBranches_CurrentChanged(object sender, EventArgs e)
        {
            selectedBranch = EmployeeBranches.CurrentItem as EmployeeBranchRelation;
        }
    }
}
