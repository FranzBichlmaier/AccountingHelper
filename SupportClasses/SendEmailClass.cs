using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AccountingHelper.SupportClasses
{
    public class SendEmailClass: BindableBase
    {
        private string userName = "bichlmaier@quantco.com";
        private string password = "ojoicvjvfeqdbamc";

        private string subject = string.Empty;
        public string Subject
        {
            get { return subject; }
            set { SetProperty(ref subject, value); }
        }
        private string toAddress = string.Empty;
        public string ToAddress
        {
            get { return toAddress; }
            set { SetProperty(ref toAddress, value); }
        }
        private string ccAddress = string.Empty;
        public string CcAddress
        {
            get { return ccAddress; }
            set { SetProperty(ref ccAddress, value); }
        }
        private string body = string.Empty;
        public string Body
        {
            get { return body; }
            set { SetProperty(ref body, value); }
        }

        private List<string> attachments;
        public List<string> Attachments
        {
            get { return attachments; }
            set { SetProperty(ref attachments, value); }
        }
        private bool useTestServer;
        public bool UserTestServer
        {
            get { return useTestServer; }
            set { SetProperty(ref useTestServer, value); }
        }
        private string host = "smtp.gmail.com";
        private bool enableSsl = true;
        private int port = 587;

        public SendEmailClass(bool useTestServer = false)
        {
            if (useTestServer)
            {
                host = "localhost";
                enableSsl = false;
                port = 25;
            }
            Attachments = new List<string>();

        }

        public bool SendEmailToServer()
        {
            SmtpClient email = new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = enableSsl,
                Host = host,
                Port = port,
                Credentials = new NetworkCredential(userName, password)
            };

            string[] adrs = ToAddress.Split(';');        

            MailMessage msg = new MailMessage(userName, adrs[0]);
            
            msg.Subject = Subject;
            msg.Body = Body;    
            
            if (adrs.Length>1)
            {
                for (int i =1; i<adrs.Length; i++)
                {
                    msg.To.Add(adrs[i]);
                }
            }
           

            if (!string.IsNullOrEmpty(CcAddress))
            {
                string[] adrscc = CcAddress.Split(';');
                foreach (string adr in adrscc)
                {
                    msg.To.Add(adr);
                }
            }      

           
            foreach(string filename in attachments)
            {
                msg.Attachments.Add(new Attachment(filename));
            }

            try
            {
                email.Send(msg);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
