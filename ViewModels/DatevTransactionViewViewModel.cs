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
    public class DatevTransactionViewModel: Qc_ViewModelBase
    {
        ObservableCollection<DatevTransactionType> transactionTypes = new ObservableCollection<DatevTransactionType>();
        DbAccess dbAccess = new DbAccess();
        DatevTransactionType selectedTransaction = null;
        public ICollectionView ListOfDatevTransactionTypes { get; set; }

        public DelegateCommand<object>  NewRowAddedCommand { get; set; }
       
        public ICommand RowDeletingCommand { get; set; }
        public ICommand RowEditEndedCommand { get; set; }
        

        public DatevTransactionViewModel()
        {
            NewRowAddedCommand = new DelegateCommand<object>(OnNewRowAdded);
          
            RowDeletingCommand = new DelegateCommand(OnRowDeleted);
            RowEditEndedCommand = new DelegateCommand(OnRowEditEnded);  
        }

        private void OnNewRowAdded(object obj)
        {
            GridViewAddingNewEventArgs e = obj as GridViewAddingNewEventArgs;
            if (e == null) return;
            e.NewObject = new DatevTransactionType();
        }

        private void OnRowEditEnded()
        {
           if (selectedTransaction.Id ==0)
            {
                DatevTransactionType returnValue = dbAccess.InsertTransactionType(selectedTransaction);
                if (returnValue == null)
                {
                    // Fehler
                }
                selectedTransaction.Id = returnValue.Id;
            }
           else
            {
                DatevTransactionType returnValue = dbAccess.UpdateTransactionType(selectedTransaction);
                if (returnValue == null)
                {
                    // Fehler
                }
            }
        }

        private void OnRowDeleted()
        {
            bool returnValue = dbAccess.RevomeTransactionType(selectedTransaction);
            if (returnValue == false)
            {
                // Fehler
            }
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            //base.OnNavigatedTo(navigationContext);
            transactionTypes = new ObservableCollection<DatevTransactionType>(dbAccess.GetAllTransactionTypes());

            // insert a new item to transactionTypes if the Collection is empty
            if (transactionTypes.Count == 0) transactionTypes.Add(new DatevTransactionType());

            ListOfDatevTransactionTypes = CollectionViewSource.GetDefaultView(transactionTypes);
            ListOfDatevTransactionTypes.CurrentChanged -= ListOfDatevTransactionTypes_CurrentChanged;
            ListOfDatevTransactionTypes.CurrentChanged += ListOfDatevTransactionTypes_CurrentChanged;
            selectedTransaction = ListOfDatevTransactionTypes.CurrentItem as DatevTransactionType;

            RaisePropertyChanged("ListOfDatevTransactionTypes");
            
         
        }

        private void ListOfDatevTransactionTypes_CurrentChanged(object sender, EventArgs e)
        {
            selectedTransaction = ListOfDatevTransactionTypes.CurrentItem as DatevTransactionType;
        }
    }
}
