using AccountingHelper.Core;
using AccountingHelper.Views;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace AccountingHelper
{
    public class Bootstrapper: UnityBootstrapper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
                        
            Container.RegisterType<object, DatevTransactionView>(ViewNames.DatevTransactionView);
            Container.RegisterType<object, EmployeeView>(ViewNames.EmployeeView);
            Container.RegisterType<object, TravelExpenseList>(ViewNames.TravelExpenseList);
            Container.RegisterType<object, EditTravelExpense>(ViewNames.EditTravelExpense);
            Container.RegisterType<object, EmployeeDocument>(ViewNames.EmployeeDocument);
            Container.RegisterType<object, EmployeeSalaries>(ViewNames.EmployeeSalaries);
            Container.RegisterType<object, BranchesView>(ViewNames.QuantCoBranches);
            Container.RegisterType<object, EmployeeOffice>(ViewNames.EmployeeOffice);
            Container.RegisterType<object, SalaryOverView>(ViewNames.SalaryOverview);
            Container.RegisterType<object, PrepareForBDO>(ViewNames.PrepareForBDO);
            Container.RegisterType<object, EmployeeOverview>(ViewNames.EmployeeOverview);
            Container.RegisterType<object, CreateEmployeeContract>(ViewNames.CreateEmployeeContract);
        }
        protected override DependencyObject CreateShell()
        {
            log.Debug("Shell created");
            return new Shell();
           
        }
        protected override void InitializeShell()
        {
            base.InitializeShell();
            Application.Current.MainWindow.Show();
        }

    }
}
