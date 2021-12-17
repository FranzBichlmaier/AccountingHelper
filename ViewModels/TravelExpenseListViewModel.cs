using AccountingHelper.Core;
using AccountingHelper.Events;
using AccountingHelper.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Telerik.Reporting;
using LINQtoCSV;
using Xceed.Wpf.Toolkit;
using System.IO;
using Prism.Interactivity.InteractionRequest;

using System.Windows;
using AccountingHelper.SupportClasses;

namespace AccountingHelper.ViewModels
{
    public class TravelExpenseListViewModel: Qc_ViewModelBase
    {
        public ICommand NewTravelExpenseCommand { get; set; }
        public ICommand ShowTravelExpenseCommand { get; set; }
        public ICommand PrepareForDatevCommand { get; set; }
        public ICommand PrepareTaxableExpensesCommand { get; set; }
        public ICommand SelectionChangedCommand { get; set; }
        public ICommand TravelExpenseItemsSelectionChangedCommand { get; set; }
        public ICommand DeletingTravelExpenseCommand { get; set; }      
        public ICommand StartMovingExpensesCommand { get; set; }
        public ICommand CancelMovingExpensesCommand { get; set; }
        public ICommand DropDownItemSelectedCommand { get; set; }

        private ObservableCollection<TravelExpense> travelExpenses = new ObservableCollection<TravelExpense>();
        public ICollectionView ListOfTravelExpenses { get; set; }
        private TravelExpense selectedItem = new TravelExpense();
        private ObservableCollection<TravelExpense> selectedTravelExpenses = new ObservableCollection<TravelExpense>();
        private List<DatevTransaction> csvDatevTransactions = new List<DatevTransaction>();
        private string[] subDirectoriesForDatev = { "Ausgang", "Eingang", "Buchungen" };
        private string belegeFolder = string.Empty;
        private string csvFolder = string.Empty;
        private string datevFolder = string.Empty;
        private string travelExpenseRoot = Path.Combine(Properties.Settings.Default.RootDirectory, Properties.Settings.Default.TravelExpenseDirectory);

        public ObservableCollection<TravelExpense> SelectedTravelExpenses
        {
            get { return selectedTravelExpenses; }
            set { SetProperty(ref selectedTravelExpenses, value); }
        }

        private readonly RegionManager regionManager;
        private readonly IEventAggregator eventaggregator;
        private DataAccess.DbAccess dbAccess = new DataAccess.DbAccess();
        private bool canPrepareForDatev;
        public bool CanPrepareForDatev
        {
            get { return canPrepareForDatev; }
            set { SetProperty(ref canPrepareForDatev, value); }
        }
        private bool canStartMoving;
        public bool CanStartMoving
        {
            get { return canStartMoving; }
            set { SetProperty(ref canStartMoving, value); }
        }
        private bool showReport = false;
        public bool ShowReport
        {
            get { return showReport; }
            set { SetProperty(ref showReport, value); }
        }
        private bool createFiles =true;
        public bool CreateFiles
        {
            get { return createFiles; }
            set { SetProperty(ref createFiles, value); }
        }
        private Visibility datevWindowState = Visibility.Collapsed;
        public Visibility DatevWindowState
        {
            get { return datevWindowState; }
            set { SetProperty(ref datevWindowState, value); }
        }
        private int bankStatementNumber;
        public int BankStatementNumber
        {
            get { return bankStatementNumber; }
            set
            {
                SetProperty(ref bankStatementNumber, value);
                if (string.IsNullOrEmpty(DatevDirectoryName))
                {
                    DatevDirectoryName = $"{DateTime.Now:yyyy} {BankStatementNumber:00}";                    
                }
            }
        }
        private string datevDirectoryName;
        public string DatevDirectoryName
        {
            get { return datevDirectoryName; }
            set { SetProperty(ref datevDirectoryName, value); if (!string.IsNullOrEmpty(DatevDirectoryName)) CanStartMoving = true; else CanStartMoving = false; }
        }



        public TravelExpenseListViewModel(RegionManager regionManager, IEventAggregator eventaggregator)
        {
            NewTravelExpenseCommand = new DelegateCommand(OnNewTravelExpense);
            ShowTravelExpenseCommand = new DelegateCommand(OnShowTravelExpense);
            PrepareForDatevCommand = new DelegateCommand(OnPrepareForDatev);
            PrepareTaxableExpensesCommand = new DelegateCommand(OnPrepareTaxableExpenses);
            SelectionChangedCommand = new DelegateCommand(OnSelectionChanged);
            DeletingTravelExpenseCommand = new DelegateCommand<object>(OnDeletingTravelExpense);
            StartMovingExpensesCommand = new DelegateCommand(OnStartMovingExpenses).ObservesCanExecute(() => CanStartMoving);
            CancelMovingExpensesCommand = new DelegateCommand(OnCancelMovingExpenses);
            TravelExpenseItemsSelectionChangedCommand = new DelegateCommand<object>(OnTravelExpenseItemsSelectionChanged);
            DropDownItemSelectedCommand = new DelegateCommand<object>(OnDropDownItemSelected);
            this.regionManager = regionManager;
            this.eventaggregator = eventaggregator;
            ConfirmationRequest = new Prism.Interactivity.InteractionRequest.InteractionRequest<Prism.Interactivity.InteractionRequest.IConfirmation>();
            NotificationRequest = new InteractionRequest<INotification>();

            // set datevFolder
            string root = Properties.Settings.Default.RootDirectory;
            string datev = Properties.Settings.Default.DatevDirectory;
            datevFolder = Path.Combine(root, datev);
        }

   

        private void OnCancelMovingExpenses()
        {
            DatevWindowState = Visibility.Collapsed;
        }

        private void OnDropDownItemSelected(object obj)
        {
           
        }

        private void OnStartMovingExpenses()
        {
            // Directories in Datev erstellen
            CreateNewDatevFolder(DatevDirectoryName);

            List<TravelExpense> travelExpensesToBeMoved = new List<TravelExpense>(SelectedTravelExpenses);

            // BelegDatein nach Datev-Ordner kopieren
            if (CreateFiles)
            {
                foreach(TravelExpense te in travelExpensesToBeMoved)
                {
                    if (string.IsNullOrEmpty(te.ReportFileName)) continue;
                    string receiptFilename = Path.Combine(travelExpenseRoot, te.ReportFileName);                    
                    File.Copy(receiptFilename, Path.Combine(belegeFolder, Path.GetFileName(receiptFilename)), true);
                }
                PrepareCsvFile(travelExpensesToBeMoved);
            }

            //csv Datei erstellen und nach Datev/Buchungen sichern

           
            PrintProtocolls(travelExpensesToBeMoved);
            if (CreateFiles)
            {
                ExportCsvFile();
                SendEmail();
            }

            // Bestätigung anzeigen
            NotificationRequest.Raise(new Notification()
            {
                Title = "QuantCo Deutschland GmbH",
                Content = $"Es wurde(n) {travelExpensesToBeMoved.Count} Reisekosten verarbeitet"
            });

            DatevWindowState = Visibility.Collapsed;
        }

        private void SendEmail()
        {
            SendEmailClass sendEmail = new SendEmailClass();
            sendEmail.Subject = "Protokoll und Buchungen der Reisekosten ";
            sendEmail.ToAddress = Properties.Settings.Default.BdoTo;
            sendEmail.CcAddress = Properties.Settings.Default.BdoCc;

            string[] filenames = Directory.GetFiles(csvFolder);

            // add further Attachments 

            foreach (string fn in filenames)
            {
                sendEmail.Attachments.Add(fn);
            }

            sendEmail.Body = $"Sehr geehrte Damen und Herren,{System.Environment.NewLine} {System.Environment.NewLine} als Anlage erhalten Sie eine Zusammenfassung der Reisekosten sowie der dazugehörenden Buchungen. " +
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


            //MailItem mailItem = outlookApp.CreateItem(OlItemType.olMailItem);

            //mailItem.BodyFormat = OlBodyFormat.olFormatHTML;
            //mailItem.SendUsingAccount = outlookUsingAccount;
            //mailItem.Subject = "Protokoll und Buchungen der Reisekosten ";

            //string bdoTo = Properties.Settings.Default.BdoTo;
            //string bdoCc = Properties.Settings.Default.BdoCc;
            //string[] emails = bdoTo.Split(';');
            //string[] emailscc = bdoCc.Split(';');
            ////string[] emails = new string[] { "franz.bichlmaier@gmail.com", "uta.bichlmaier@gmail.com" };
            ////string[] emailscc = new string[] { "franzrudolf.bichlmaier@outlook.de" };

            //foreach (string emailAddress in emails)
            //{
            //    Recipient recipTo = mailItem.Recipients.Add(emailAddress);
            //    recipTo.Type = (int)OlMailRecipientType.olTo;
            //}
            //foreach (string emailAddress in emailscc)
            //{
            //    Recipient recipCc = mailItem.Recipients.Add(emailAddress);
            //    recipCc.Type = (int)OlMailRecipientType.olCC;
            //}

            //if (!mailItem.Recipients.ResolveAll())
            //{
            //    NotificationRequest.Raise(new Notification()
            //    {
            //        Title = "QuantCo Deutschland GmbH",
            //        Content = "Eine der E-Mail-Adressen ist falsch"
            //    });
            //    return;
            //}

            //mailItem.Body = $"Sehr geehrte Damen und Herren,{System.Environment.NewLine} {System.Environment.NewLine} als Anlage erhalten Sie eine Zusammenfassung der Reisekosten sowie der dazugehörenden Buchungen. " +
            //    $"{System.Environment.NewLine} {System.Environment.NewLine} Mit freundlichen Grüßen {System.Environment.NewLine} {System.Environment.NewLine} QuantCo Deutschland GmbH";

            ////
            //// add all files which are in directory buchungen
            ////
            //string[] filenames = Directory.GetFiles(csvFolder);

            //// add further Attachments 

            //foreach (string fn in filenames)
            //{
            //    mailItem.Attachments.Add(fn, OlAttachmentType.olByValue);
            //}

            //mailItem.Send();
        }

        private void OnDeletingTravelExpense(object obj)
        {
            TravelExpense te = ListOfTravelExpenses.CurrentItem as TravelExpense;
            if (te == null) return;
            Telerik.Windows.Controls.GridViewDeletingEventArgs e = obj as Telerik.Windows.Controls.GridViewDeletingEventArgs;
            if (e == null) return;
            ConfirmationRequest.Raise(new Confirmation()
            {
                Title = "QauntCo Deutschland GmbH",
                Content = $"Wollen Sie die ausgewählten Reisekosten ({te.EmployeeName}) wirklich löschen?"
            }, respone =>
            {
                if (respone.Confirmed)
                {
                    dbAccess.RemoveTravelExpense(te);
                }
                else
                {
                    e.Cancel = true;
                }
            });
        }

        private void CreateNewDatevFolder(string dirName)
        {            
            string fulldir = Path.Combine(datevFolder, dirName);
            belegeFolder = fulldir;
            Directory.CreateDirectory(fulldir);
            for (int i =0; i<subDirectoriesForDatev.Length; i++)
            {
                string subdir = Path.Combine(fulldir, subDirectoriesForDatev[i]);
                Directory.CreateDirectory(subdir);
                if (subDirectoriesForDatev[i].StartsWith("Buch")) csvFolder = subdir;
            }
        }

        private void OnPrepareTaxableExpenses()
        {
            List<int> employeeList = dbAccess.GetEmployeeIdsForOpenTaxableIncome();

            if (employeeList.Count==0)
            {
                NotificationRequest.Raise(new Notification()
                {
                    Title = "QuantCo Deutschland GmbH",
                    Content = "Es sind keine Datensätze vorhanden, die noch nicht versendet wurden"
                });
                return;
            }
            TypeReportSource source = new TypeReportSource();
            source.TypeName = typeof(AccountingHelper.Reporting.TaxableIncomeReport).AssemblyQualifiedName;           
            ViewerParameter parameter = new ViewerParameter()
            {
                typeReportSource = source
            };
            eventaggregator.GetEvent<ViewerParameterEvent>().Publish(parameter);

            #region Save Report as File 
            // pdf Datei erzeugen und in Datev speichern

            // specify foldername  .../Datev/TaxableIncomeReports/Report yyyyMMdd

            string foldername = Path.Combine(Properties.Settings.Default.RootDirectory, Properties.Settings.Default.DatevDirectory, "TaxableIncomeReports");
            Directory.CreateDirectory(foldername);
            string filename = $"Report {DateTime.Now.Date:yyyyMMdd}.pdf";
            filename = Path.Combine(foldername, filename);

            // create Report as pdf file
            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

            // set any deviceInfo settings if necessary
            var deviceInfo = new System.Collections.Hashtable();


            var reportSource = new Telerik.Reporting.TypeReportSource();

            // reportName is the Assembly Qualified Name of the report
            reportSource.TypeName = typeof(AccountingHelper.Reporting.TaxableIncomeReport).AssemblyQualifiedName;


            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);


            using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }
            #endregion

            #region Send File To Taxadvisor
            // send file to taxAdvisor

            // prepare Email 

            SendEmailClass sendEmail = new SendEmailClass();
            sendEmail.Subject = "Besteuerung von geldwerten Vorteilen ";
            sendEmail.ToAddress = Properties.Settings.Default.BdoHr;
            sendEmail.Attachments.Add(filename);
            sendEmail.Body = $"Sehr geehrte Damen und Herren,{System.Environment.NewLine} {System.Environment.NewLine} als Anlage erhalten Sie eine Aufstellung über die geldwerten Vorteile aus den Reisekostenabrechnungen. " +
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


            //MailItem mailItem = outlookApp.CreateItem(OlItemType.olMailItem);

            //mailItem.BodyFormat = OlBodyFormat.olFormatHTML;
            //mailItem.SendUsingAccount = outlookUsingAccount;
            //mailItem.Subject = "Besteuerung von geldwerten Vorteilen ";

            //string BdoHr = Properties.Settings.Default.BdoHr;

            //string[] emails = BdoHr.Split(';');

            //foreach (string email in emails)
            //{
            //    Recipient recipTo = mailItem.Recipients.Add(email);
            //    recipTo.Type = (int)OlMailRecipientType.olTo;
            //}


            //if (!mailItem.Recipients.ResolveAll())
            //{
            //    NotificationRequest.Raise(new Notification()
            //    {
            //        Title = "QuantCo Deutschland GmbH",
            //        Content = $"Die E-Mail-Adressen '{BdoHr}' enthalten mindestens einen falschen Eintrag"
            //    });

            //}
            //else
            //{
            //    mailItem.Body = $"Sehr geehrte Damen und Herren,{System.Environment.NewLine} {System.Environment.NewLine} als Anlage erhalten Sie eine Aufstellung über die geldwerten Vorteile aus den Reisekostenabrechnungen. " +
            //  $"{System.Environment.NewLine} {System.Environment.NewLine} Mit freundlichen Grüßen {System.Environment.NewLine} {System.Environment.NewLine} QuantCo Deutschland GmbH";


            //    mailItem.Attachments.Add(filename, OlAttachmentType.olByValue);
            //    mailItem.Save();

            //} 
            #endregion

            ConfirmationRequest.Raise(new Confirmation()
            {
                Title = "QuantCo Deutschland GmbH",
                Content = "Sollen die Einträge als 'gesendet' gekennzeichnet werden? "
            }, response =>
            {
                if (response.Confirmed)
                {
                    // send Emails to Employees
                    TaxableIncomeSendEmailsToEmployees();

                    // mark records as sent
                    int nrOfAmendments = dbAccess.SetProvidedToTaxableIncome();                    
                     
                    NotificationRequest.Raise(new Notification()
                    {
                        Title = "QuantCo Deutschland GmbH",
                        Content = $"Es wurden {nrOfAmendments} Datensätze geändert."
                    });
                }
            });
        }

        private void TaxableIncomeSendEmailsToEmployees()
        {
            List<int> employeeList = dbAccess.GetEmployeeIdsForOpenTaxableIncome();
            foreach(int employeeId in employeeList)
            {
                EmployeesForTravelExpenses employee = dbAccess.FindEmployeeById(employeeId);
                if (employee == null) continue;
                if (string.IsNullOrEmpty(employee.EmailAddress)) continue;
                // Render Report
                var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

                // set any deviceInfo settings if necessary
                var deviceInfo = new System.Collections.Hashtable();


                var reportSource = new Telerik.Reporting.TypeReportSource();

                // reportName is the Assembly Qualified Name of the report
                reportSource.TypeName = typeof(AccountingHelper.Reporting.TaxableIncomeByEmployee).AssemblyQualifiedName;


                // Pass parameter value with the Report Source if necessary         
                reportSource.Parameters.Add("EmployeeId", employeeId);

                Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);


                // set Directory to Root/Datev/TaxableIncomeReports/ and Filename: Fullname + "Taxable Income" + Date
                string taxableIncomeFileName = Path.Combine(Properties.Settings.Default.RootDirectory, Properties.Settings.Default.DatevDirectory, "TaxableIncomeReports", $"{employee.FullName} TaxableIncome {DateTime.Now:d}.pdf");


                using (System.IO.FileStream fs = new System.IO.FileStream(taxableIncomeFileName, System.IO.FileMode.Create))
                {
                    fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
                }

                // prepare Email 
                SendEmailClass sendEmail = new SendEmailClass();
                sendEmail.Subject = "Besteuerung von geldwerten Vorteilen ";
                sendEmail.ToAddress = employee.EmailAddress;
                sendEmail.Attachments.Add(taxableIncomeFileName);
                sendEmail.Body = $"Lieber {employee.FullName},{System.Environment.NewLine} {System.Environment.NewLine} als Anlage erhältst Du eine Liste der geldwerten Vorteile, die in Kürze versteuert werden. " +
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

                //MailItem mailItem = outlookApp.CreateItem(OlItemType.olMailItem);

                //mailItem.BodyFormat = OlBodyFormat.olFormatHTML;
                //mailItem.SendUsingAccount = outlookUsingAccount;
                //mailItem.Subject = "Besteuerung von geldwerten Vorteilen ";               
            
                //Recipient recipTo = mailItem.Recipients.Add(employee.EmailAddress);
                //recipTo.Type = (int)OlMailRecipientType.olTo;
                
         

                //if (!mailItem.Recipients.ResolveAll())
                //{
                //    NotificationRequest.Raise(new Notification()
                //    {
                //        Title = "QuantCo Deutschland GmbH",
                //        Content = $"Die E-Mail Adresse für {employee.FullName} ist ungültig"
                //    });
                //    continue;
                //}

                //mailItem.Body = $"Lieber {employee.FullName},{System.Environment.NewLine} {System.Environment.NewLine} als Anlage erhältst Du eine Liste der geldwerten Vorteile, die in Kürze versteuert werden. " +
                //    $"{System.Environment.NewLine} {System.Environment.NewLine} Mit freundlichen Grüßen {System.Environment.NewLine} {System.Environment.NewLine} QuantCo Deutschland GmbH";

               
                //    mailItem.Attachments.Add(taxableIncomeFileName, OlAttachmentType.olByValue);
             

                //mailItem.Save();


            }
        }

        private void OnTravelExpenseItemsSelectionChanged(object obj)
        {
            Telerik.Windows.Controls.SelectionChangeEventArgs e = obj as Telerik.Windows.Controls.SelectionChangeEventArgs;
            if (e == null) return;
            foreach(TravelExpense item in e.RemovedItems)
            {
                SelectedTravelExpenses.Remove(item);
            }
            foreach (TravelExpense item in e.AddedItems)
            {
                SelectedTravelExpenses.Add(item);
            }
        }

        private void OnSelectionChanged()
        {
            //CanPrepareForDatev = SelectedTravelExpenses.Count > 0 ? true : false;
        }

        private void OnPrepareForDatev()
        {
            if (SelectedTravelExpenses == null) return;
            if (SelectedTravelExpenses.Count == 0) return;

            DatevWindowState = Visibility.Visible;
           
        }

        private void ExportCsvFile()
        {
            CsvFileDescription csvFileDescription = new CsvFileDescription
            {
                SeparatorChar = ';',
                FirstLineHasColumnNames = true,
                FileCultureName = "de-DE"
            };
            CsvContext csvContext = new CsvContext();
            csvContext.Write(csvDatevTransactions, Path.Combine(csvFolder, "Reisekostenbuchungen.csv"), csvFileDescription);
        }

        private void PrintProtocolls(List<TravelExpense> travelExpensesToBeMoved)
        {
            List<int> idList = new List<int>();
            foreach(TravelExpense item in travelExpensesToBeMoved)
            {
                idList.Add(item.Id);

                if (CreateFiles)
                {
                    TravelExpense newItem = dbAccess.SetProvidedToDatev(item);
                    if (item == null)
                    {
                        NotificationRequest.Raise(new Notification()
                        {
                            Title = "QuantCo Deutschland GmbH",
                            Content = $"Beim Setzen des Datums 'ProvidedToDatev' ({item.Id} ist ein Fehler aufgetreten"
                        });
                    }
                    else
                    {
                        // reload travelexpenses to show the updates made in SetProvidedToDatev

                        travelExpenses.Clear();
                        travelExpenses = new ObservableCollection<TravelExpense>(dbAccess.GetAllTravelExpenses());

                        ListOfTravelExpenses = CollectionViewSource.GetDefaultView(travelExpenses);

                        ListOfTravelExpenses.CurrentChanged -= ListOfTravelExpenses_CurrentChanged;
                        ListOfTravelExpenses.CurrentChanged += ListOfTravelExpenses_CurrentChanged;
                        RaisePropertyChanged("ListOfTravelExpenses");
                        if (travelExpenses.Count > 0)
                        {
                            selectedItem = travelExpenses[0];
                        }
                    }
                }
            }
            if (ShowReport)
            {
                TypeReportSource source = new TypeReportSource();
                source.TypeName = typeof(AccountingHelper.Reporting.TravelExpenses).AssemblyQualifiedName;
                source.Parameters.Add("TravelExpenseIds", idList);
                ViewerParameter parameter = new ViewerParameter()
                {
                    typeReportSource = source
                };
                eventaggregator.GetEvent<ViewerParameterEvent>().Publish(parameter);
            }


            if (CreateFiles)
            {
                // create Report as pdf file
                var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

                // set any deviceInfo settings if necessary
                var deviceInfo = new System.Collections.Hashtable();


                var reportSource = new Telerik.Reporting.TypeReportSource();

                // reportName is the Assembly Qualified Name of the report
                reportSource.TypeName = typeof(AccountingHelper.Reporting.TravelExpenses).AssemblyQualifiedName;


                // Pass parameter value with the Report Source if necessary         
                reportSource.Parameters.Add("TravelExpenseIds", idList);

                Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);


                using (System.IO.FileStream fs = new System.IO.FileStream(Path.Combine(csvFolder, "ProtokollReisekosten.pdf"), System.IO.FileMode.Create))
                {
                    fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
                } 
            }

        }

        private void PrepareCsvFile(List<TravelExpense> travelExpensesToBeMoved)
        {
            csvDatevTransactions.Clear();
            foreach(TravelExpense te in travelExpensesToBeMoved)
            {
                TravelExpense travelExpense = dbAccess.GetTravelExpenseById(te.Id);
                foreach (TravelExpenseItem item in travelExpense.TravelExpenseItems)
                {
                    DatevTransactionType type = dbAccess.GetTransactionById(item.DatevTransactionTypeId);
                    if (item.Amount00 !=0)
                    {
                        DatevTransaction datevItem = new DatevTransaction();
                        //Datum
                        datevItem.TransactionDate = travelExpense.ExpenseDate;
                        if (item.PerformanceDate == null)
                            datevItem.ExpenseDate = datevItem.TransactionDate;
                        else
                            datevItem.ExpenseDate = (DateTime)item.PerformanceDate;
                        
                        //Buchungstext
                        datevItem.TransactionDescription = $"RK {travelExpense.EmployeeName} {item.Description}";
                        //Belegfeld
                        datevItem.TransactionReceipt = (travelExpense.ExpenseDate.Month * 10000 + travelExpense.ExpenseDate.Year).ToString();
                        // Steuersatz
                        datevItem.TransactionVatRate = type.DatevVat07;
                        //Umsatz
                        datevItem.TransactionAmountDatev = item.Amount00;
                        // Soll / Haben
                        if (datevItem.TransactionAmountDatev > 0) datevItem.TransactionBalancePosition = "H"; else datevItem.TransactionBalancePosition = "S";
                        //konto
                        datevItem.TransactionAccount = type.DatevAccount;
                        // gegenkonto
                        datevItem.TransactionContraAccount = travelExpense.EmployeesForTravelExpenses.DatevAccount + type.SubAccount;

                        if (datevItem.TransactionDate<new DateTime(2020,1,1))
                        {
                            datevItem.TransactionAccount = datevItem.TransactionAccount / 100;
                            datevItem.TransactionContraAccount = datevItem.TransactionContraAccount / 100;
                        }
                        csvDatevTransactions.Add(datevItem);   
                    }
                    if (item.Amount07 != 0)
                    {
                        DatevTransaction datevItem = new DatevTransaction();
                        //Datum
                        datevItem.TransactionDate = travelExpense.ExpenseDate;
                        if (item.PerformanceDate == null)
                            datevItem.ExpenseDate = datevItem.TransactionDate;
                        else
                            datevItem.ExpenseDate = (DateTime)item.PerformanceDate;

                        //Buchungstext
                        datevItem.TransactionDescription = $"RK {travelExpense.EmployeeName} {item.Description}";
                        //Belegfeld
                        datevItem.TransactionReceipt = (travelExpense.ExpenseDate.Month * 10000 + travelExpense.ExpenseDate.Year).ToString();
                        // Steuersatz
                        datevItem.TransactionVatRate = type.DatevVat19;
                        //Umsatz
                        datevItem.TransactionAmountDatev = item.Amount07;
                        // Soll / Haben
                        if (datevItem.TransactionAmountDatev > 0) datevItem.TransactionBalancePosition = "H"; else datevItem.TransactionBalancePosition = "S";
                        //konto
                        datevItem.TransactionAccount = type.DatevAccount;
                        // gegenkonto
                        datevItem.TransactionContraAccount = travelExpense.EmployeesForTravelExpenses.DatevAccount + type.SubAccount;

                        if (datevItem.TransactionDate < new DateTime(2020, 1, 1))
                        {
                            datevItem.TransactionAccount = datevItem.TransactionAccount / 100;
                            datevItem.TransactionContraAccount = datevItem.TransactionContraAccount / 100;
                        }
                        csvDatevTransactions.Add(datevItem);
                    }
                    if (item.Amount19 != 0)
                    {
                        DatevTransaction datevItem = new DatevTransaction();
                        //Datum
                        datevItem.TransactionDate = travelExpense.ExpenseDate;
                        if (item.PerformanceDate == null)
                            datevItem.ExpenseDate = datevItem.TransactionDate;
                        else
                            datevItem.ExpenseDate = (DateTime)item.PerformanceDate;

                        //Buchungstext
                        datevItem.TransactionDescription = $"RK {travelExpense.EmployeeName} {item.Description}";
                        //Belegfeld
                        datevItem.TransactionReceipt = (travelExpense.ExpenseDate.Month * 10000 + travelExpense.ExpenseDate.Year).ToString();
                        // Steuersatz
                        datevItem.TransactionVatRate = type.DatevVat00;
                        //Umsatz
                        datevItem.TransactionAmountDatev = item.Amount19;
                        // Soll / Haben
                        if (datevItem.TransactionAmountDatev > 0) datevItem.TransactionBalancePosition = "H"; else datevItem.TransactionBalancePosition = "S";
                        //konto
                        datevItem.TransactionAccount = type.DatevAccount;
                        // gegenkonto
                        datevItem.TransactionContraAccount = travelExpense.EmployeesForTravelExpenses.DatevAccount + type.SubAccount;

                        if (datevItem.TransactionDate < new DateTime(2020, 1, 1))
                        {
                            datevItem.TransactionAccount = datevItem.TransactionAccount / 100;
                            datevItem.TransactionContraAccount = datevItem.TransactionContraAccount / 100;
                        }
                        csvDatevTransactions.Add(datevItem);
                    }
                }
            }
          
        }

        private void OnShowTravelExpense()
        {
            if (selectedItem == null) return;
            NavigationParameters parameter = new NavigationParameters();
            parameter.Add("TravelExpense", selectedItem);
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.EditTravelExpense, parameter);
        }

        private void OnNewTravelExpense()
        {
            NavigationParameters parameter = new NavigationParameters();
            parameter.Add("TravelExpense", new TravelExpense());
            regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.EditTravelExpense, parameter);
        }
       

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            SelectedTravelExpenses.Clear();

            // Read TravelExpenses
            travelExpenses = new ObservableCollection<TravelExpense>(dbAccess.GetAllTravelExpenses());

             
            ListOfTravelExpenses = CollectionViewSource.GetDefaultView(travelExpenses);
            
            ListOfTravelExpenses.CurrentChanged -= ListOfTravelExpenses_CurrentChanged;
            ListOfTravelExpenses.CurrentChanged += ListOfTravelExpenses_CurrentChanged;
            RaisePropertyChanged("ListOfTravelExpenses");
            if (travelExpenses.Count>0)
            {
                selectedItem = travelExpenses[0];
            }
        }

        private void ListOfTravelExpenses_CurrentChanged(object sender, EventArgs e)
        {
            selectedItem = ListOfTravelExpenses.CurrentItem as TravelExpense;
        }
    }
}
