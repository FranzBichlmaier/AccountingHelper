using AccountingHelper.DataAccess;
using AccountingHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;

namespace AccountingHelper.SupportClasses
{
    public class CreateEmployeeSalaryOverview
    {
        private readonly DateTime periodfrom;
        private readonly DateTime periodto;
        private DateTimeFunctions.DateDifferences dateFunctions = new DateTimeFunctions.DateDifferences();
        
        private List<EmployeeSalaryOverview> employeeSalaryOverviews = null;
        private List<EmployeeChangeLog> changeLogItems = null;
        private List<EmployeesMonthlyPayments> monthlyPayments = null;
        private List<EmployeesForTravelExpenses> employees;

        private DbAccess dbAccess = new DbAccess();

        /// <summary>
        /// creates a list of EmployeeSalaryOverview either for one Employee (employeeId != null) or for all Employees (employeeId == null)
        /// </summary>
        /// <param name="employeeId">null or employeeId</param>
        public CreateEmployeeSalaryOverview(int? employeeId, DateTime periodfrom, DateTime periodto)
        {
            employeeSalaryOverviews = new List<EmployeeSalaryOverview>();
            employees = new List<EmployeesForTravelExpenses>();
            this.periodfrom = periodfrom;
            this.periodto = periodto;
            if (employeeId == null)
            {
                employees = dbAccess.GetAllEmployees();
            }
            else
            {
                employees.Add(dbAccess.FindEmployeeById((int)employeeId));
            }

            foreach(EmployeesForTravelExpenses employee in employees)
            {
                if (string.IsNullOrEmpty(employee.FirstName)) employee.FirstName = string.Empty;  
  
                EmployeeSalaryOverview e = GetEmployeeSalaryOverview(employee.Id);
                if (e.TotalSalary >0) employeeSalaryOverviews.Add(e);
            }           
        }

        public List<EmployeeSalaryOverview> GetSalaryOverview()
        {
            return new List<EmployeeSalaryOverview>(employeeSalaryOverviews);
        }

        public List<EmployeesMonthlyPayments> GetMonthlyPayments(int month)
        {
            monthlyPayments = new List<EmployeesMonthlyPayments>();

            foreach (EmployeeSalaryOverview overviewItem in employeeSalaryOverviews)
            {
  
                if (!IsActiveEmployee(overviewItem, month)) continue;
                EmployeesForTravelExpenses employee = dbAccess.FindEmployeeById(overviewItem.EmployeeForTravelExpensesId);                      
                EmployeesMonthlyPayments item = new EmployeesMonthlyPayments();
                item.EmployeeForTravelExpensesId = employee.Id;
                item.MonthAndYear = $"{periodto:MMMM} {periodto:yyyy}";
                item.Fullname = employee.FullName;
                item.MonthlyPayment = SetMonthlySalary(overviewItem, month);
                item.BonusAmount = SetBonusAmount(overviewItem, month);
                if (item.MonthlyPayment >0)
                {
                    // calculate Social Security Premiim
                    item.SocialSecurity = (decimal)Math.Round((double)item.MonthlyPayment * 19.5 / 100, 2);
                    if (item.SocialSecurity > 1178m) item.SocialSecurity = 1178m; 
                }
                item.TotalCost = item.MonthlyPayment + item.BonusAmount + item.SocialSecurity;
                monthlyPayments.Add(item);
            }
            return monthlyPayments;
        }

        private decimal SetMonthlySalary(EmployeeSalaryOverview overviewItem, int month)
        {
            switch (month)
            {

                case 1:
                    return overviewItem.January;
                case 2:
                    return overviewItem.February;
                case 3:
                    return overviewItem.March;
                case 4:
                    return overviewItem.April;
                case 5:
                    return overviewItem.May;
                case 6:
                    return overviewItem.June;
                case 7:
                    return overviewItem.July;
                case 8:
                    return overviewItem.August;
                case 9:
                    return overviewItem.September;
                case 10:
                    return overviewItem.October;
                case 11:
                    return overviewItem.November;
                case 12:
                    return overviewItem.December;
                default:
                    return 0m;
            }
        }

        public List<EmployeeChangeLog> GetChangeLogItems(int month)
        {
            changeLogItems = new List<EmployeeChangeLog>();

            foreach(EmployeeSalaryOverview overviewItem in employeeSalaryOverviews)
            {
                if (!IsActiveEmployee(overviewItem, month)) continue;

                // do not show employees located in Bulgaria as they do not affect German pay roll
                if (overviewItem.GermanPayroll == false) continue;

                EmployeesForTravelExpenses employee = dbAccess.FindEmployeeById(overviewItem.EmployeeForTravelExpensesId);
                EmployeePaymentDetail payment = dbAccess.GetLatestSalary(employee.Id);
                List<TaxableIncomeItem> taxableItems = dbAccess.GetOpenTaxableItemsByEmployeeId(overviewItem.EmployeeForTravelExpensesId);
                EmployeeChangeLog item = new EmployeeChangeLog();
                item.FirstName = employee.FirstName;
                item.LastName = employee.ShortName;
                item.BonusAmount = SetBonusAmount(overviewItem, month);
                if (payment.FirstPayment.Year == periodfrom.Year && payment.FirstPayment.Month == month)        // nur füllen, wenn eine Gehaltsänderung vorgenommen wurde 
                {
                    item.NewSalary = payment.MonthlyAmount;
                    if (employee.EntitledToSurplus) item.NewSalary -= 300;  // Das Gehalt wird ohne den Essenszuschuss an BDO geliefert.
                }
                // loop through taxableItems
                // if Description contains("VMA") set item.vma
                if (taxableItems.Count>0)
                {
                    item.VmaTaxable = taxableItems.Where(t => t.Description.ToUpper().Contains("VMA")).Sum(s => s.TaxableAmount);
                    
                    item.TaxableIncome = taxableItems.Where(t => !t.Description.ToUpper().Contains("VMA") && t.AdjustGrossIncome==false).Sum(s => s.TaxableAmount);
                    item.TaxableIncomeWithNetAdjustment = taxableItems.Where(t => !t.Description.ToUpper().Contains("VMA") && t.AdjustGrossIncome ==true).Sum(s => s.TaxableAmount);
                }
                changeLogItems.Add(item);
            }
            return changeLogItems;
            
        }

        private bool IsActiveEmployee(EmployeeSalaryOverview overviewItem, int month)
        {
            decimal totalamount = 0m;
            switch (month)
            {

                case 1:
                    totalamount = overviewItem.January + overviewItem.JanuaryBonus;
                    break;
                case 2:
                    totalamount = overviewItem.February + overviewItem.FebruaryBonus;
                    break;
                case 3:
                    totalamount = overviewItem.March + overviewItem.MarchBonus;
                    break;
                case 4:
                    totalamount = overviewItem.April + overviewItem.AprilBonus;
                    break;
                case 5:
                    totalamount = overviewItem.May + overviewItem.MayBonus;
                    break;
                case 6:
                    totalamount = overviewItem.June + overviewItem.JuneBonus;
                    break;
                case 7:
                    totalamount = overviewItem.July + overviewItem.JulyBonus;
                    break;
                case 8:
                    totalamount = overviewItem.August + overviewItem.AugustBonus;
                    break;
                case 9:
                    totalamount = overviewItem.September + overviewItem.SeptemberBonus;
                    break;
                case 10:
                    totalamount = overviewItem.October + overviewItem.OctoberBonus;
                    break;
                case 11:
                    totalamount = overviewItem.November + overviewItem.NovemberBonus;
                    break;
                case 12:
                    totalamount = overviewItem.December + overviewItem.DecemberBonus;
                    break;
                default:
                    totalamount = 0m;
                    break;
            }
            return totalamount > 0;
        }

        private decimal SetBonusAmount(EmployeeSalaryOverview overviewItem, int month)
        {
            switch (month)
                {

                case 1:
                    return overviewItem.JanuaryBonus;
                case 2:
                    return overviewItem.FebruaryBonus;
                case 3:
                    return overviewItem.MarchBonus;
                case 4:
                    return overviewItem.AprilBonus;
                case 5:
                    return overviewItem.MayBonus;
                case 6:
                    return overviewItem.JuneBonus;
                case 7:
                    return overviewItem.JulyBonus;
                case 8:
                    return overviewItem.AugustBonus;
                case 9:
                    return overviewItem.SeptemberBonus;
                case 10:
                    return overviewItem.OctoberBonus;
                case 11:
                    return overviewItem.NovemberBonus;
                case 12:
                    return overviewItem.DecemberBonus;
                default:
                    return 0m;
            }
        }

        private EmployeeSalaryOverview GetEmployeeSalaryOverview(int id)
        {
            EmployeesForTravelExpenses employee = dbAccess.FindEmployeeById(id);
            List<EmployeePaymentDetail> details = dbAccess.GetAllPaymentDetails(id);
            QuantCoBranch branch = dbAccess.GetBranchById(employee.QuantCoBranchId);
            EmployeeSalaryOverview overview = new EmployeeSalaryOverview();

            if (details.Count == 0) return overview;

            overview.EmployeeForTravelExpensesId = id;
            overview.EmployeeName = employee.FullName;
            if (branch != null)
            {
                overview.OfficeLocation = branch.LocationName;
                overview.GermanPayroll = branch.GermanPayroll;
            }
               
            overview.CalenderYear = periodfrom.Year.ToString();

  
            foreach(EmployeePaymentDetail detail in details)
            {
                if (detail.PaymentType == PaymentType.Bonus && detail.LastPayment == null) detail.LastPayment = detail.FirstPayment; 
                else 
                    if(detail.LastPayment == null) detail.LastPayment = periodto;  //set LastPayment 
                if (detail.LastPayment < periodfrom) continue;  // payment was before reporting period
                if (detail.FirstPayment > periodto) continue;   // payment is after reporting period

                DateTime periodStart = periodfrom;

              
                    if (detail.PaymentType == PaymentType.Bonus)
                    {
                        AddBonusPayment(overview, detail.FirstPayment, detail.MonthlyAmount);
                    }
                    else if (detail.PaymentType == PaymentType.Salary)
                    {
                        AddSalary(overview, detail.FirstPayment, detail.LastPayment, detail.MonthlyAmount);
                    }
                    else if (detail.PaymentType == PaymentType.CurrentSalary)
                    {
                        AddSalary(overview, detail.FirstPayment, detail.LastPayment, detail.MonthlyAmount);
                    }
                    else
                    {

                    }
            }

            return overview;
        }

        private void AddSalary(EmployeeSalaryOverview overview, DateTime firstPayment, DateTime? lastPayment, decimal monthlyAmount)
        {
            if (overview.EmployeeName.Contains("Achelrod") )
            {

            }
            decimal salaryAmount = 0m;
            DateTime paymentDate = firstPayment;
            if (paymentDate < periodfrom) paymentDate = periodfrom;
            DateTime endDate = periodto;
            if(lastPayment == null)
            {
                endDate = periodto;
            }
            else
            {
                if ((DateTime)lastPayment < endDate) endDate = (DateTime)lastPayment;
            }

            DateTime compareDate = dateFunctions.MonthEnd(endDate);

            bool isemployed = true;
            do
            {
                
                salaryAmount = monthlyAmount;
                if (paymentDate.Month == endDate.Month)
                {
                    // das Gehalt muss eventuell gekürzt werden, wenn der Austritt / Unterbrechnung nicht am Monatsende erfolgt.                 
                    if (lastPayment != null && lastPayment <=endDate)
                    {
                        DateTime monthEnd = dateFunctions.MonthEnd(new DateTime(paymentDate.Year, paymentDate.Month, 1));
                        DateTime helperDate = (DateTime)lastPayment;
                        if (monthEnd.Day != helperDate.Day)
                        {
                            salaryAmount = Math.Round(monthlyAmount / 30 * helperDate.Day, 0);
                        }                     
                    }
                }
                if (paymentDate == firstPayment && paymentDate.Day != 1)
                {
                    // das erste Gehalt muss gekürzt werden, wenn der Starttermin nicht auf den 1. eines Monats fällt
                    int numberofDays = DateTime.DaysInMonth(firstPayment.Year, firstPayment.Month) - firstPayment.Day;
                    salaryAmount = Math.Round(monthlyAmount / 30 * numberofDays);
                }
                overview.TotalSalary += salaryAmount;
                switch (paymentDate.Month)
                {
                    case 1:
                        {
                            overview.January += salaryAmount;
                            break;
                        }
                    case 2:
                        {
                            overview.February += salaryAmount;
                            break;
                        }
                    case 3:
                        {
                            overview.March += salaryAmount;
                            break;
                        }
                    case 4:
                        {
                            overview.April += salaryAmount;
                            break;
                        }
                    case 5:
                        {
                            overview.May += salaryAmount;
                            break;
                        }
                    case 6:
                        {
                            overview.June += salaryAmount;
                            break;
                        }
                    case 7:
                        {
                            overview.July += salaryAmount;
                            break;
                        }
                    case 8:
                        {
                            overview.August += salaryAmount;
                            break;
                        }
                    case 9:
                        {
                            overview.September += salaryAmount;
                            break;
                        }
                    case 10:
                        {
                            overview.October += salaryAmount;
                            break;
                        }
                    case 11:
                        {
                            overview.November += salaryAmount;
                            break;
                        }
                    case 12:
                        {
                            overview.December += salaryAmount;
                            break;
                        }
                }
                paymentDate = paymentDate.AddMonths(1);
                if (paymentDate > compareDate) isemployed = false;       

                } while (isemployed);           
        }

        private void AddBonusPayment(EmployeeSalaryOverview overview, DateTime firstPayment, decimal monthlyAmount)
        {
            overview.TotalBonus += monthlyAmount;
            switch (firstPayment.Month)
            {
                case 1:
                    {
                        overview.JanuaryBonus += monthlyAmount;
                        break;
                    }
                case 2:
                    {
                        overview.FebruaryBonus += monthlyAmount;
                        break;
                    }
                case 3:
                    {
                        overview.MarchBonus += monthlyAmount;
                        break;
                    }
                case 4:
                    {
                        overview.AprilBonus += monthlyAmount;
                        break;
                    }
                case 5:
                    {
                        overview.MayBonus += monthlyAmount;
                        break;
                    }
                case 6:
                    {
                        overview.JuneBonus += monthlyAmount;
                        break;
                    }
                case 7:
                    {
                        overview.JulyBonus += monthlyAmount;
                        break;
                    }
                case 8:
                    {
                        overview.AugustBonus += monthlyAmount;
                        break;
                    }
                case 9:
                    {
                        overview.SeptemberBonus += monthlyAmount;
                        break;
                    }
                case 10:
                    {
                        overview.OctoberBonus += monthlyAmount;
                        break;
                    }
                case 11:
                    {
                        overview.NovemberBonus += monthlyAmount;
                        break;
                    }
                case 12:
                    {
                        overview.DecemberBonus += monthlyAmount;
                        break;
                    }

            }
        }
    }
}
