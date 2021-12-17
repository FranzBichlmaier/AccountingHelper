using AccountingHelper.Core;
using AccountingHelper.DataAccess;
using AccountingHelper.Models;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
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
    public class BranchesViewModel: Qc_ViewModelBase
    {
        private ObservableCollection<QuantCoBranch> QuantCoBranches;
        private DbAccess dataAccess = new DbAccess();
        private QuantCoBranch selectedItem = new QuantCoBranch();


        private ObservableCollection<QuantCoBranch> quantCoBranches = new ObservableCollection<QuantCoBranch>();
       
        public ICollectionView ListOfBranches { get; set; }
        public ICommand AddedNewRowCommand { get; private set; }
        public ICommand RowDeletingCommand { get; private set; }
        public ICommand RowEditEndedCommand { get; private set; }

        public BranchesViewModel()
        {
            AddedNewRowCommand = new DelegateCommand<object>(OnAddedNewRow);
            RowDeletingCommand = new DelegateCommand<object>(OnRowDeleting);
            RowEditEndedCommand = new DelegateCommand(OnRowEditEnded);

            NotificationRequest = new InteractionRequest<INotification>();
        }

        private void OnRowEditEnded()
        {
            if (selectedItem.Id == 0)
            {
                QuantCoBranch e = dataAccess.InsertQuantCoBranch(selectedItem);
                if (e == null)
                {
                    // Fehler
                }
                else
                {
                    selectedItem.Id = e.Id;
                    return;
                }
            }
            else
            {
                QuantCoBranch e = dataAccess.UpdateQuantCoBranch(selectedItem);
                if (e == null)
                {
                    // Fehler
                }
            }
        }

        private void OnRowDeleting(object obj)
        {
            var e = (Telerik.Windows.Controls.GridViewDeletingEventArgs)obj;

            if (e == null) return;

            if (!dataAccess.IsQuantCoBranchUsed(selectedItem.Id))
            {
                if (dataAccess.RemoveQuantCoBranch(selectedItem)) return;
            }
            else
            {
                NotificationRequest.Raise(new Notification()
                {
                    Title = "QuantCo Deutschland GmbH",
                    Content = "Die Niederlassung kann nicht gelöscht werden. Sie ist noch mind. einem Mitarbeiter zugeordnet"
                });
                // e.Cancel = true ensures the record is not cancelled
                e.Cancel = true;
                return;
            }
            
        }

        private void OnAddedNewRow(object obj)
        {
            GridViewAddingNewEventArgs e = obj as GridViewAddingNewEventArgs;
            if (e == null) return;
            QuantCoBranch branch = new QuantCoBranch();
            branch.GermanPayroll = true;
            e.NewObject = branch;
        }
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            quantCoBranches = new ObservableCollection<QuantCoBranch>(dataAccess.GetAllQuantCoBranches());
            if (quantCoBranches.Count == 0) quantCoBranches.Add(new QuantCoBranch());

            ListOfBranches = CollectionViewSource.GetDefaultView(quantCoBranches);
            ListOfBranches.CurrentChanged -= ListOfBranches_CurrentChanged;
            ListOfBranches.CurrentChanged += ListOfBranches_CurrentChanged;
            selectedItem = ListOfBranches.CurrentItem as QuantCoBranch;
         
            RaisePropertyChanged("ListOfBranches");
        }

        private void ListOfBranches_CurrentChanged(object sender, EventArgs e)
        {
            if (ListOfBranches.CurrentItem != null) selectedItem = ListOfBranches.CurrentItem as QuantCoBranch;
        }
    }
}
