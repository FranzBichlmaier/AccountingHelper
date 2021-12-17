using System.Windows;
using Telerik.Windows.Controls;

namespace AccountingHelper.Views
{
    /// <summary>
    /// Interaktionslogik für Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        public Shell()
        {
            StyleManager.ApplicationTheme = new MaterialTheme();
            InitializeComponent();
        }

       
    }
}
