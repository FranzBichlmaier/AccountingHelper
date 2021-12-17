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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DateTimeFunctions;
using Telerik.Reporting;
using AccountingHelper.Events;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;
using AccountingHelper.SupportClasses;
using Newtonsoft.Json;

namespace AccountingHelper.ViewModels
{
    public class EmployeeSalariesViewModel: Qc_ViewModelBase
    {
        List<EmployeesForTravelExpenses> newEmployees = new List<EmployeesForTravelExpenses>();
        private DateDifferences timeFunctions = new DateDifferences();
        ObservableCollection<EmployeePaymentDetail> employeePaymentDetails = new ObservableCollection<EmployeePaymentDetail>();
        ObservableCollection<EmployeePayment> currentPayments = new ObservableCollection<EmployeePayment>();
        public ICollectionView ListOfCurrentPayments { get; set; }
        public ICollectionView ListOfPaymentDetails { get; set; }
        private EmployeePaymentDetail currentPaymentDetail;
        public EmployeePaymentDetail CurrentPaymentDetail
        {
            get { return currentPaymentDetail; }
            set { SetProperty(ref currentPaymentDetail, value); }
        }

        private Visibility noRecordView = Visibility.Collapsed;
        public Visibility NoRecordView
        {
            get { return noRecordView; }
            set { SetProperty(ref noRecordView, value); }
        }

        private Visibility currentSalaryView = Visibility.Collapsed;
        public Visibility CurrentSalaryView
        {
            get { return currentSalaryView; }
            set { SetProperty(ref currentSalaryView, value); }
        }
        private Visibility bonusView = Visibility.Collapsed;
        public Visibility BonusView
        {
            get { return bonusView; }
            set { SetProperty(ref bonusView, value); }
        }
        private Visibility salaryHistoryView = Visibility.Collapsed;
        public Visibility SalaryHistoryView
        {
            get { return salaryHistoryView; }
            set { SetProperty(ref salaryHistoryView, value); }
        }
        private Visibility finalPaymentDateView = Visibility.Collapsed;
        public Visibility FinalPaymentDateView
        {
            get { return finalPaymentDateView; }
            set { SetProperty(ref finalPaymentDateView, value); }
        }
        private double controlHeight;
        public double ControlHeight
        {
            get { return controlHeight; }
            set { SetProperty(ref controlHeight, value); }
        }
        private DateTime monthlySalaryDate = DateTime.Now;
        public DateTime MonthlySalaryDate
        {
            get { return monthlySalaryDate; }
            set { SetProperty(ref monthlySalaryDate
                , value); }
        }
        private DateTime timelinePeriodStart;
        public DateTime TimelinePeriodStart
        {
            get { return timelinePeriodStart; }
            set { SetProperty(ref timelinePeriodStart, value); }
        }
        private DateTime timelinePeriodEnd;
        public DateTime TimelinePeriodEnd
        {
            get { return timelinePeriodEnd; }
            set { SetProperty(ref timelinePeriodEnd, value); }
        }
        private string currentEmployeeFullname;
        public string CurrentEmployeeFullname
        {
            get { return currentEmployeeFullname; }
            set { SetProperty(ref currentEmployeeFullname, value); }
        }
        public ICommand EditCurrentSalaryCommand { get; set; }
        public ICommand AddBonusPaymentCommand { get; set; }
        public ICommand EditSalaryHistoryCommand { get; set; }
        public ICommand ShowDevelopmentCommand { get; set; }
        public ICommand SalaryOverviewCommand { get; set; }
        public ICommand PayrollCostCommand { get; set; }
        public ICommand SalaryDevelopmentCommand { get; set; }
        public ICommand SetFinalPaymentCommand { get; set; }
        public ICommand UserControlLoadedCommand { get; set; }
        public ICommand AddingNewDetailCommand { get; set; }
        public ICommand AddNewPaymentDetailCommand { get; set; }
        public ICommand RowDetailDeletingCommand { get; set; }
        public ICommand RowEditEndedCommand { get; set; }
        public ICommand CellValidatingCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand SalaryItemDeletingCommand { get; set; }
        public ICommand VacationLiabilityCommand { get; set; }

        DbAccess dbAccess = new DbAccess();
        private readonly IEventAggregator eventAggregator;

        public EmployeeSalariesViewModel( IEventAggregator eventAggregator)
        {
            EditCurrentSalaryCommand = new DelegateCommand(OnEditCurrentSalary);
            AddBonusPaymentCommand = new DelegateCommand(OnAddBonusPayment);
            EditSalaryHistoryCommand = new DelegateCommand(OnEditSalaryHistory);
            SetFinalPaymentCommand = new DelegateCommand(OnSetFinalPayment);
            SalaryOverviewCommand = new DelegateCommand(OnSalaryOverview);
            PayrollCostCommand = new DelegateCommand(OnPayrollCost);
            UserControlLoadedCommand = new DelegateCommand<object>(OnUserControlLoaded);
            AddingNewDetailCommand = new DelegateCommand<object>(OnAddingNewDetail);
            AddNewPaymentDetailCommand = new DelegateCommand(OnAddNewPaymentDetail);
            RowDetailDeletingCommand = new DelegateCommand(OnRowDetailDeleting);
            RowEditEndedCommand = new DelegateCommand(OnRowEditEnded);
            CellValidatingCommand = new DelegateCommand<object>(OnCellValidating);
            SaveCommand = new DelegateCommand<string>(OnSave);
            CancelCommand = new DelegateCommand<string>(OnCancel);
            SalaryItemDeletingCommand = new DelegateCommand<object>(OnSalaryItemDeleting);
            VacationLiabilityCommand = new DelegateCommand(OnVacationLiability);
            ConfirmationRequest = new InteractionRequest<IConfirmation>();
            this.eventAggregator = eventAggregator;

            NotificationRequest = new InteractionRequest<INotification>();
           
        }
/// <summary>
/// Send E-Mails to Colleagues regarding open vacation days
/// </summary>
        private void OnVacationLiability()
        {
            DateTime reportDate = timeFunctions.MonthEnd(timeFunctions.MonthEnd(MonthlySalaryDate));
            

            CreateEmployeeSalaryOverview salaryOverview = new CreateEmployeeSalaryOverview(null, new DateTime(reportDate.Year, 1, 1), reportDate);
            List<EmployeesMonthlyPayments> monthlyPayments = salaryOverview.GetMonthlyPayments(reportDate.Month);

            foreach (EmployeesMonthlyPayments payment in monthlyPayments)
            {
                EmployeesForTravelExpenses employee = dbAccess.FindEmployeeById(payment.EmployeeForTravelExpensesId);
                if (employee == null) continue;
                EmployeePaymentDetail salarydetail = dbAccess.GetLatestSalary(payment.EmployeeForTravelExpensesId);

                // do not send emails to people whos contract expire 
                if (salarydetail.LastPayment != null && salarydetail.LastPayment == reportDate) continue;

                // Send Email

                SendEmailClass sendEmail = new SendEmailClass();
                sendEmail.Subject = "Urlaub / Vacation ";
                sendEmail.ToAddress = employee.EmailAddress;

                StringBuilder germanText = new StringBuilder();
                germanText.Append($"Liebe(r) {employee.FirstName},{Environment.NewLine}{Environment.NewLine}");
                germanText.Append("Wie einmal im Jahr üblich, bitte ich dich mir mitzuteilen, wieviele Urlaubstage zum Jahresende noch offen sein werden. Falls du deinen kompletten Urlaub genommen hast, kannst du diese E-Mail ignorieren." + Environment.NewLine + Environment.NewLine);
                germanText.Append($"Solltest du im Laufe des Jahres {reportDate.Year} bei QuantCo angefangen haben, errechnen sich die Urlaubstage nach der Formel 28 / 12 * Anzahl Monate bei QuantCo (Ergebnis bitte aufrunden).");
                germanText.Append(Environment.NewLine + Environment.NewLine + "Vielen Dank für deine Unterstützung." + Environment.NewLine + Environment.NewLine);
                StringBuilder englishText = new StringBuilder();
                englishText.Append($"Dear {employee.FirstName},{Environment.NewLine}{Environment.NewLine}");
                englishText.Append("As usual once a year, I would like to ask you to tell me how many vacation days are left for this calendar year. Should you have taken all your vacation you can ignore this email." + Environment.NewLine + Environment.NewLine);
                englishText.Append($"In case you have joined QuantCo in {reportDate.Year} you can calculate the number of vacation days using the formula: 28 / 12 * number of months with QuantCo (result can be rounded up).");
                englishText.Append(Environment.NewLine + Environment.NewLine + "Thank you for your support." + Environment.NewLine + Environment.NewLine);

                sendEmail.Body = $"{germanText.ToString()}{englishText.ToString()}" + 
                    $"{System.Environment.NewLine} {System.Environment.NewLine} Mit freundlichen Grüßen / Best regards {System.Environment.NewLine} {System.Environment.NewLine} Franz";               

                bool success = true;

                if (employee.FirstName.Contains("Sabrina") || employee.FirstName.Contains("Franz"))
                {
                    success = sendEmail.SendEmailToServer();
                }

                if (!success)
                {
                    NotificationRequest.Raise(new Notification()
                    {
                        Title = "QuantCo Deutschland GmbH",
                        Content = $"Das Email an {sendEmail.ToAddress} konnte nicht gesendet werden"
                    });
                }


            }

        }

        private void OnSalaryItemDeleting(object obj)
        {
           
                     
            EmployeePaymentDetail detail = ListOfPaymentDetails.CurrentItem as EmployeePaymentDetail;
            if (detail == null) return;
            Telerik.Windows.Controls.GridViewDeletingEventArgs e = obj as Telerik.Windows.Controls.GridViewDeletingEventArgs;
            if (e == null) return;
            ConfirmationRequest.Raise(new Confirmation()
            {
                Title = "QauntCo Deutschland GmbH",
                Content = $"Wollen Sie den Eintrag wirklich löschen?"
            }, respone =>
            {
                if (respone.Confirmed)
                {
                    dbAccess.RemoveEmployeePaymentDetail(detail);
                }
                else
                {
                    e.Cancel = true;
                }
            });

        }

        private void OnPayrollCost()
        {
            //TypeReportSource source = new TypeReportSource();
            //source.TypeName = typeof(AccountingHelper.Reporting.MonthlySalary).AssemblyQualifiedName;

            //source.Parameters.Add("EndDate", reportDate);
            //source.Parameters.Add("StartDate", new DateTime(reportDate.Year, reportDate.Month, 1));
            //source.Parameters.Add("MonthYear", $"{reportDate: MMMM yyyy}");


            // new Version of MonthlySalary
            DateTime reportDate = timeFunctions.MonthEnd(timeFunctions.MonthEnd(MonthlySalaryDate));

            CreateEmployeeSalaryOverview salaryOverview = new CreateEmployeeSalaryOverview(null, new DateTime(reportDate.Year, 1, 1), reportDate);
            List<EmployeesMonthlyPayments> monthlyPayments = salaryOverview.GetMonthlyPayments(reportDate.Month);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;

            string jsonContent = JsonConvert.SerializeObject(monthlyPayments, settings);

            string jsonfile = $"C:\\Users\\Public\\Documents\\MonthlyOverview.json";

            System.IO.File.WriteAllText(jsonfile, jsonContent);
            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

            // set any deviceInfo settings if necessary
            var deviceInfo = new System.Collections.Hashtable();


            var reportSource = new Telerik.Reporting.TypeReportSource();

            // reportName is the Assembly Qualified Name of the report
            reportSource.TypeName = typeof(AccountingHelper.Reporting.MonthlySalaryOverview).AssemblyQualifiedName;


            // Pass parameter value with the Report Source if necessary         
            reportSource.Parameters.Add("Source", jsonfile);
            reportSource.Parameters.Add("DataSelector", string.Empty);           

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("XLSX", reportSource, deviceInfo);

            string xlsxFile = jsonfile.Replace("json", "xlsx");
            using (System.IO.FileStream fs = new System.IO.FileStream(xlsxFile, System.IO.FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }

            ConfirmationRequest.Raise(new Confirmation()
            {
                Content = $"Es wurde eine Excel-Datei ({xlsxFile}) erstellt" + Environment.NewLine + Environment.NewLine + "Soll die Excel-Datei als E-Mail verschickt werden?",
                Title = "QuantCo Deutschland GmbH"
            }, response =>
            {
                if (response.Confirmed)
                {
                    // send E-mail to Property 'CEOto'
                    SendEmailToCEO(xlsxFile);
                }
            });

            ViewerParameter parameter = new ViewerParameter()
            {
                typeReportSource = reportSource
            };
            eventAggregator.GetEvent<ViewerParameterEvent>().Publish(parameter);

        }

        private void SendEmailToCEO(string xlsxFile)
        {
            string test = Properties.Settings.Default.CEOTo;
            SendEmailClass sendEmail = new SendEmailClass();
            sendEmail.Subject = $"Gehaltsübersicht ({monthlySalaryDate:MMMM yyyy})";
            //sendEmail.ToAddress = Properties.Settings.Default.CEOTo;
            sendEmail.ToAddress = "bichlmaier@quantco.com";

            sendEmail.Attachments.Add(xlsxFile);
            
            sendEmail.Body = $"Hallo Johann, {Environment.NewLine}{Environment.NewLine} anbei die Auswertung wie besprochen." +
                $"{System.Environment.NewLine} {System.Environment.NewLine} Mit freundlichen Grüßen {System.Environment.NewLine} {System.Environment.NewLine} QuantCo Deutschland GmbH";

            bool success = sendEmail.SendEmailToServer();

            if (!success)
            {
                NotificationRequest.Raise(new Notification()
                {
                    Title = "QuantCo Deutschland GmbH",
                    Content = $"Das Email an {sendEmail.ToAddress} konnte nicht gesendet werden"
                });
            }
        }

        private void OnSalaryOverview()
        {
            TypeReportSource source = new TypeReportSource();
            source.TypeName = typeof(AccountingHelper.Reporting.SalaryOverviewReport).AssemblyQualifiedName;
            DateTime reportDate = timeFunctions.MonthEnd(DateTime.Now);
            source.Parameters.Add("ReportDate", reportDate );
            ViewerParameter parameter = new ViewerParameter()
            {
                typeReportSource = source
            };
            eventAggregator.GetEvent<ViewerParameterEvent>().Publish(parameter);
        }

        private void OnCellValidating(object obj)
        {
            Telerik.Windows.Controls.GridViewCellValidatingEventArgs e = obj as Telerik.Windows.Controls.GridViewCellValidatingEventArgs;
            if (e == null) return;
            // validate PaymentType
            if (!e.Cell.Column.UniqueName.Equals("PaymentType")) return;
            if (CurrentPaymentDetail.Id >0 && e.NewValue.Equals(e.OldValue)) return;
            PaymentType newType = (PaymentType)e.NewValue;
            if (newType == PaymentType.CurrentSalary)
            {
                e.IsValid = false;
                e.ErrorMessage = "Die Auswahl 'CurrentSalary' ist nicht zulässig";
            }
        }

        private void OnRowEditEnded()
        {
            EmployeePaymentDetail detail = ListOfPaymentDetails.CurrentItem as EmployeePaymentDetail;
            if (detail == null) return;
            EmployeePayment payment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            // if Bonuspayment: set LastPayment = FirstPayment
            if (detail.PaymentType == PaymentType.Bonus) detail.LastPayment = detail.FirstPayment;            
            if (detail.Id == 0)
            {           
                detail = dbAccess.InsertEmployeePaymentDetail(detail);
            }
            else
            {
                dbAccess.UpdateEmployeePaymentDetail(detail);
                
                if (detail.PaymentType==PaymentType.CurrentSalary && payment.FirstPayment.Month == detail.FirstPayment.Month)
                {
                    // update payment
                    payment.LastPayment = detail.LastPayment;
                    payment.MonthlySalary = detail.MonthlyAmount;
                    payment.SocialSecurityPremium = detail.SocialSecurityPremium;
                    dbAccess.UpdateEmployeePayment(payment);
                }
            }
            RefreshCurrentPayment(payment);
        }

        private void OnRowDetailDeleting()
        {
            EmployeePaymentDetail detail = ListOfPaymentDetails.CurrentItem as EmployeePaymentDetail;
            if (detail == null) return;

            dbAccess.RemoveEmployeePaymentDetail(detail);
        }

        private void OnAddingNewDetail(object obj)
        {
            Telerik.Windows.Controls.GridView.GridViewAddingNewEventArgs e = obj as Telerik.Windows.Controls.GridView.GridViewAddingNewEventArgs;
            if (e == null) return;
            EmployeePaymentDetail newDetail = new EmployeePaymentDetail();
            EmployeePayment currentPayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            newDetail.EmployeeId = currentPayment.EmployeeId;
            e.NewObject = newDetail;
        }

        private void OnAddNewPaymentDetail()
        {
            EmployeePaymentDetail newDetail = new EmployeePaymentDetail();
            newDetail.PaymentType = PaymentType.Salary;
            EmployeePayment currentPayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            newDetail.EmployeeId = currentPayment.EmployeeId;
            employeePaymentDetails.Add(newDetail);
            ListOfPaymentDetails.MoveCurrentToLast();
        }

        private void OnUserControlLoaded(object obj)
        {
            RoutedEventArgs e = obj as RoutedEventArgs;
            UserControl userControl = e.Source as UserControl;
            ControlHeight = userControl.ActualHeight;

        }

        private void OnSetFinalPayment()
        {
            EmployeePayment employeePayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            CurrentPaymentDetail = dbAccess.GetLatestSalary(employeePayment.EmployeeId);
            if (CurrentPaymentDetail == null) return;
            NoRecordView = Visibility.Collapsed;
            CurrentSalaryView = Visibility.Collapsed;
            BonusView = Visibility.Collapsed;
            SalaryHistoryView = Visibility.Collapsed;
            FinalPaymentDateView = Visibility.Visible;
        }

        private void OnCancel(string obj)
        {
            EmployeePayment employeePayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            switch (obj)
            {
                case "Detail":
                    {
                        if (CurrentPaymentDetail.Id ==0)
                        {
                            employeePaymentDetails.Remove(CurrentPaymentDetail);
                            RefreshCurrentPayment(employeePayment);
                        }
                        break;
                    }
            }
        }

        private void OnSave(string obj)
        {
            EmployeePayment employeePayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            switch (obj)
            {
                case "CurrentSalary":
                    {
                        EmployeePaymentDetail existingCurrentSalary = dbAccess.GetCurrentPayment(CurrentPaymentDetail.EmployeeId);
                        if (existingCurrentSalary == null)
                        {
                            //there is no existing CurrentSalary ==> insert record and update EmployeePayment
                            CurrentPaymentDetail = dbAccess.SetCurrentPayment(CurrentPaymentDetail);
                            employeePaymentDetails.Add(CurrentPaymentDetail);                            
                            employeePayment.FirstPayment = CurrentPaymentDetail.FirstPayment;
                            employeePayment.LastPayment = CurrentPaymentDetail.LastPayment;
                            employeePayment.PaymentDescription = CurrentPaymentDetail.PaymentDescription;
                            employeePayment.MonthlySalary = CurrentPaymentDetail.MonthlyAmount;
                            employeePayment.SocialSecurityPremium = CurrentPaymentDetail.SocialSecurityPremium;
                        }
                        else
                        {
                            if (existingCurrentSalary.FirstPayment.Month == CurrentPaymentDetail.FirstPayment.Month && 
                                existingCurrentSalary.FirstPayment.Year == CurrentPaymentDetail.FirstPayment.Year)
                            {
                                //update existingCurrentSalary with new data
                                //update EmployeePayment
                                existingCurrentSalary.LastPayment = CurrentPaymentDetail.LastPayment;
                                existingCurrentSalary.PaymentDescription = CurrentPaymentDetail.PaymentDescription;
                                existingCurrentSalary.Sequence = CurrentPaymentDetail.Sequence;
                                existingCurrentSalary.SocialSecurityPremium = CurrentPaymentDetail.SocialSecurityPremium;
                                existingCurrentSalary.MonthlyAmount = CurrentPaymentDetail.MonthlyAmount;
                                dbAccess.UpdateEmployeePaymentDetail(existingCurrentSalary);

                                employeePayment.MonthlySalary = existingCurrentSalary.MonthlyAmount;
                                employeePayment.LastPayment = existingCurrentSalary.LastPayment;
                                employeePayment.SocialSecurityPremium = existingCurrentSalary.SocialSecurityPremium;
                                employeePayment.PaymentDescription = existingCurrentSalary.PaymentDescription;
                                dbAccess.UpdateEmployeePayment(employeePayment);
                            }
                            else
                            {
                                //set PaymentType of existingCurrentSalary To Salary and set LastPaymentDate
                                //insert record and update EmploymentPayment
                                existingCurrentSalary.PaymentType = PaymentType.Salary;
                                existingCurrentSalary.LastPayment = timeFunctions.MonthEnd(CurrentPaymentDetail.FirstPayment.AddMonths(-1));
                                dbAccess.UpdateEmployeePaymentDetail(existingCurrentSalary);

                                CurrentPaymentDetail = dbAccess.SetCurrentPayment(CurrentPaymentDetail);
                                employeePaymentDetails.Add(CurrentPaymentDetail);
                                employeePayment.FirstPayment = CurrentPaymentDetail.FirstPayment;
                                employeePayment.LastPayment = CurrentPaymentDetail.LastPayment;
                                employeePayment.PaymentDescription = CurrentPaymentDetail.PaymentDescription;
                                employeePayment.MonthlySalary = CurrentPaymentDetail.MonthlyAmount;
                                employeePayment.SocialSecurityPremium = CurrentPaymentDetail.SocialSecurityPremium;
                            }
                        }
                        
                        break;
                    }
                case "BonusPayment":
                    {
                        CurrentPaymentDetail = dbAccess.AddBonusPayment(CurrentPaymentDetail);
                        employeePaymentDetails.Add(CurrentPaymentDetail);
                        break;
                    }
                case "LastPayment":
                    {
                        dbAccess.SetFinalPayment(CurrentPaymentDetail);
                        employeePaymentDetails.Add(CurrentPaymentDetail);   
                        employeePayment.LastPayment = CurrentPaymentDetail.LastPayment;
                        employeePayment.PaymentDescription = CurrentPaymentDetail.PaymentDescription;
                        break;
                    }
                case "Detail":
                    {
                        
                        if (CurrentPaymentDetail == null) return;
                        EmployeePayment payment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
                        // if Bonuspayment: set LastPayment = FirstPayment
                        if (CurrentPaymentDetail.PaymentType == PaymentType.Bonus) CurrentPaymentDetail.LastPayment = CurrentPaymentDetail.FirstPayment;
                        if (CurrentPaymentDetail.Id == 0)
                        {
                            CurrentPaymentDetail = dbAccess.InsertEmployeePaymentDetail(CurrentPaymentDetail);
                        }
                        else
                        {
                            dbAccess.UpdateEmployeePaymentDetail(CurrentPaymentDetail);

                            if (CurrentPaymentDetail.PaymentType.Equals(PaymentType.CurrentSalary))
                            {
                                // update payment
                                payment.FirstPayment = CurrentPaymentDetail.FirstPayment;
                                payment.LastPayment = CurrentPaymentDetail.LastPayment;
                                payment.MonthlySalary = CurrentPaymentDetail.MonthlyAmount;
                                payment.SocialSecurityPremium = CurrentPaymentDetail.SocialSecurityPremium;
                                dbAccess.UpdateEmployeePayment(payment);
                            }
                        }
                        RefreshCurrentPayment(payment);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            RefreshCurrentPayment(employeePayment);
        }

        private void OnEditSalaryHistory()
        {
            NoRecordView = Visibility.Collapsed;
            CurrentSalaryView = Visibility.Collapsed;
            BonusView = Visibility.Collapsed;
            SalaryHistoryView = Visibility.Visible;
            FinalPaymentDateView = Visibility.Collapsed;
        }

        private void OnAddBonusPayment()
        {
            NoRecordView = Visibility.Collapsed;
            CurrentSalaryView = Visibility.Collapsed;
            BonusView = Visibility.Visible;
            SalaryHistoryView = Visibility.Collapsed;
            FinalPaymentDateView = Visibility.Collapsed;
            CurrentPaymentDetail = new EmployeePaymentDetail();
            CurrentPaymentDetail.PaymentType = PaymentType.Bonus;
            EmployeePayment employeePayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            CurrentPaymentDetail.EmployeeId = employeePayment.EmployeeId;
        }

        private void OnEditCurrentSalary()
        {
            NoRecordView = Visibility.Collapsed;
            CurrentSalaryView = Visibility.Visible;
            BonusView = Visibility.Collapsed;
            SalaryHistoryView = Visibility.Collapsed;
            FinalPaymentDateView = Visibility.Collapsed;
            CurrentPaymentDetail = new EmployeePaymentDetail();
            CurrentPaymentDetail.PaymentType = PaymentType.CurrentSalary;
            EmployeePayment employeePayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            CurrentPaymentDetail.EmployeeId = employeePayment.EmployeeId;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            newEmployees = dbAccess.GetNewEmployeesForEmployeePayments();
            if (newEmployees.Count>0)
            {
                foreach(EmployeesForTravelExpenses employee in newEmployees)
                {
                    EmployeePayment payment = new EmployeePayment();
                    payment.EmployeeId = employee.Id;
                    payment = dbAccess.InsertEmployeePayment(payment);
                }
            }

            currentPayments = new ObservableCollection<EmployeePayment>(dbAccess.GetAllCurrentPayments());
           

            ListOfCurrentPayments = CollectionViewSource.GetDefaultView(currentPayments);
            ListOfCurrentPayments.CurrentChanged -= ListOfCurrentPayments_CurrentChanged;
            ListOfCurrentPayments.CurrentChanged += ListOfCurrentPayments_CurrentChanged;

            if (ListOfCurrentPayments.CurrentItem != null)
            {
                EmployeePayment currentPayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
                RefreshCurrentPayment(currentPayment);
            }

            RaisePropertyChanged("ListOfCurrentPayments");
  
        }

        private void ListOfCurrentPayments_CurrentChanged(object sender, EventArgs e)
        {
            EmployeePayment currentPayment = ListOfCurrentPayments.CurrentItem as EmployeePayment;
            RefreshCurrentPayment(currentPayment);
        }

        private void RefreshCurrentPayment( EmployeePayment currentPayment)
        {
            employeePaymentDetails = new ObservableCollection<EmployeePaymentDetail>(dbAccess.GetAllPaymentDetails(currentPayment.EmployeeId));
            CurrentEmployeeFullname = currentPayment.Employee.FullName;

            ListOfPaymentDetails = CollectionViewSource.GetDefaultView(employeePaymentDetails);
            ListOfPaymentDetails.CurrentChanged -= ListOfPaymentDetails_CurrentChanged;
            ListOfPaymentDetails.CurrentChanged += ListOfPaymentDetails_CurrentChanged;
            if (employeePaymentDetails.Count == 0)
            {
                NoRecordView = Visibility.Visible;
                CurrentSalaryView = Visibility.Collapsed;
                BonusView = Visibility.Collapsed;
                SalaryHistoryView = Visibility.Collapsed;
                FinalPaymentDateView = Visibility.Collapsed;
            }
            else
            {
                NoRecordView = Visibility.Collapsed;
                CurrentSalaryView = Visibility.Collapsed;
                BonusView = Visibility.Collapsed;
                SalaryHistoryView = Visibility.Visible;
                FinalPaymentDateView = Visibility.Collapsed;
                // set Duration and Tooltip
                SetDurationAndTooltips();
            }
            // set  sort criteria
            ListOfPaymentDetails.SortDescriptions.Clear();

            ListOfPaymentDetails.SortDescriptions.Add(new SortDescription
            {
                Direction = ListSortDirection.Ascending,
                PropertyName = "FirstPayment"
            });
            ListOfPaymentDetails.SortDescriptions.Add(new SortDescription
            {
                Direction = ListSortDirection.Ascending,
                PropertyName = "PaymentType"
            });
            RaisePropertyChanged("ListOfPaymentDetails");
        }

        private void SetDurationAndTooltips()
        {
            TimelinePeriodStart = DateTime.MaxValue;
            TimelinePeriodEnd = timeFunctions.MonthEnd(DateTime.Now);
            foreach(EmployeePaymentDetail detail in ListOfPaymentDetails)
            {
                if (detail.FirstPayment < TimelinePeriodStart) TimelinePeriodStart = detail.FirstPayment.Date;
                if (detail.PaymentType == PaymentType.Bonus)
                {
                    detail.Duration = new TimeSpan();
                    detail.Tooltip = $"Bonus: {detail.MonthlyAmount:N0}";
                }
                else
                {
                    detail.Tooltip = $"Monatsgehalt: {detail.MonthlyAmount:n0}";
                    if (detail.LastPayment == null)
                    {
                        DateTime lastDate = timeFunctions.MonthEnd(DateTime.Now);
                        detail.Duration = lastDate.Subtract(detail.FirstPayment);                        
                    }
                    else
                    {
                        DateTime lastDate = timeFunctions.MonthEnd((DateTime)detail.LastPayment);
                        detail.Duration = lastDate.Subtract(detail.FirstPayment);
                    }
                }
            }
        }

        private void ListOfPaymentDetails_CurrentChanged(object sender, EventArgs e)
        {
            CurrentPaymentDetail = ListOfPaymentDetails.CurrentItem as EmployeePaymentDetail;
            RaisePropertyChanged("CurrentPaymentDetail");
        }
    }
}
