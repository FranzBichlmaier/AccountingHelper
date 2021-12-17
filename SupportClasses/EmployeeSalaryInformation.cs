using AccountingHelper.DataAccess;
using AccountingHelper.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;

namespace AccountingHelper.SupportClasses
{
    public class EmployeeSalaryInformation: BindableBase
    {
        private EmployeesForTravelExpenses employee = null;
        private List<EmployeeSalaryDetail> employeeSalaryDetails = null;
        private List<EmployeeSalaryDetail> employeeAnnualSalaries = null;
        private List<EmployeePaymentDetail> paymentDetails;
        private DbAccess dbAccess = new DbAccess();
        private DateTime dateJoined;
        public DateTime DateJoined
        {
            get { return dateJoined; }
            set { SetProperty(ref dateJoined, value); }
        }
        private DateTime? dateLeft;
        public DateTime? DateLeft
        {
            get { return dateLeft; }
            set { SetProperty(ref dateLeft, value); }
        }
        private DateTime myDateLeft;
        public EmployeeSalaryInformation(int employeeId)
        {
            paymentDetails = dbAccess.GetAllPaymentDetails(employeeId);
            if (paymentDetails.Count == 0) return;
            DateJoined = paymentDetails.OrderBy(d => d.FirstPayment).Where(d => d.PaymentType == PaymentType.CurrentSalary || d.PaymentType == PaymentType.Salary).FirstOrDefault().FirstPayment;
            DateLeft = paymentDetails.OrderByDescending(d => d.FirstPayment).Where(d => d.PaymentType == PaymentType.CurrentSalary || d.PaymentType == PaymentType.Salary).FirstOrDefault().LastPayment;
            if (DateLeft == null) myDateLeft = new DateTime(DateTime.Now.Year, 12, 31);

            CreateMonthlyPaymentInformation();
            CreateAnnualPaymentInformtion();
        }

        private void CreateMonthlyPaymentInformation()
        {
            DateTime firstDate = new DateTime(DateJoined.Year, DateJoined.Month, 1);
            DateTime lastDate = new DateTime(DateTime.Now.Year, 12, 31);
            decimal lastSalary = 0m;
           
            employeeSalaryDetails = new List<EmployeeSalaryDetail>();
            DateTime begin = firstDate;
            do
            {
                employeeSalaryDetails.Add(new EmployeeSalaryDetail()
                {
                    Category = $"{begin:MMM yyyy}",
                    SalaryMonth = begin,
                    Salary=0m,
                    Bonus=0m
                });
                begin = begin.AddMonths(1);
            } while (begin < lastDate);

            foreach (EmployeeSalaryDetail detail in employeeSalaryDetails)
            {
                foreach(EmployeePaymentDetail payment in paymentDetails)
                {
                    if (payment.PaymentType == PaymentType.CurrentSalary || payment.PaymentType == PaymentType.Salary)
                    {
                        if (payment.LastPayment != null && payment.LastPayment < detail.SalaryMonth) continue;
                        if (payment.FirstPayment > detail.SalaryMonth) continue;
                        detail.Salary = payment.MonthlyAmount;
                        if (detail.Salary != lastSalary)
                        {
                            lastSalary = detail.Salary;
                            detail.SalaryLabel = $"Monatsgehalt: {detail.Salary:N0}";
                        }
                    }
                    if (payment.PaymentType == PaymentType.Bonus)
                    {
                        if (payment.FirstPayment.Year != detail.SalaryMonth.Year) continue;
                        if (payment.FirstPayment.Month != detail.SalaryMonth.Month) continue;
                        detail.Bonus = payment.MonthlyAmount;
                        detail.BonusLabel = $"{detail.Bonus:n0}";
                    }
                }
                detail.TotalCompensation = detail.Salary + detail.Bonus;
            }
        }

        private void CreateAnnualPaymentInformtion()
        {
            employeeAnnualSalaries = new List<EmployeeSalaryDetail>();
            EmployeeSalaryDetail monthItem = new EmployeeSalaryDetail();
            int startYear = 0;
            foreach(EmployeeSalaryDetail detail in employeeSalaryDetails)
            {
                if (startYear != detail.SalaryMonth.Year)
                {
                    startYear = detail.SalaryMonth.Year;
                    monthItem = new EmployeeSalaryDetail()
                    {
                        Category = startYear.ToString(),
                        SalaryMonth = new DateTime(startYear, 1, 1)
                    };
                    employeeAnnualSalaries.Add(monthItem);
                }
                monthItem.Salary += detail.Salary;
                monthItem.Bonus += detail.Bonus;
                monthItem.TotalCompensation = monthItem.Salary + monthItem.Bonus;
            }
        }

        public List<EmployeeSalaryDetail> GetMonthlySalaries()
        {
            return employeeSalaryDetails;
        }
        public List<EmployeeSalaryDetail> GetAnnualSalaries()
        {
            return employeeAnnualSalaries;
        }
    }
}
