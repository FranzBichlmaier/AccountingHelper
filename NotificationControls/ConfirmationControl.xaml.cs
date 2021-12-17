using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AccountingHelper.NotificationControls
{
    /// <summary>
    /// Interaktionslogik für ConfirmationControl.xaml
    /// </summary>
    public partial class ConfirmationControl : UserControl, IInteractionRequestAware
    {
        public ConfirmationControl()
        {
            InitializeComponent();
        }
        private Confirmation _notification;


        public Action FinishInteraction { get; set; }
        INotification IInteractionRequestAware.Notification
        {
            get { return _notification; }
            set { _notification = value as Confirmation; }
        }

        private void NotifiedButton_Click(object sender, RoutedEventArgs e)
        {
            _notification.Confirmed = true;
            FinishInteraction?.Invoke();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _notification.Confirmed = false;
            FinishInteraction?.Invoke();
        }
    }
}
