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
    /// Interaktionslogik für NotificationControl.xaml
    /// </summary>
    public partial class NotificationControl : UserControl, IInteractionRequestAware
    {
        public NotificationControl()
        {
            InitializeComponent();
        }
        private Notification _notification;


        public Action FinishInteraction { get; set; }
        INotification IInteractionRequestAware.Notification
        {
            get { return _notification; }
            set { _notification = value as Notification; }
        }

        private void NotifiedButton_Click(object sender, RoutedEventArgs e)
        {
            //_notification.Confirmed = true;
            FinishInteraction?.Invoke();

        }
    }
}
