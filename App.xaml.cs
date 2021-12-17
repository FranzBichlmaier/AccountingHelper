using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace AccountingHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
       
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ensure the formatting of textboxes ... are in German
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            FrameworkElement.LanguageProperty.OverrideMetadata(
                              typeof(FrameworkElement),
                              new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            //
            // The following statement defines the location of the viewmodel of a view:
            // it is located in the same namespace as the view using the name: viewname followed by ViewModel
            //
                    
           
            Bootstrapper bs = new Bootstrapper();
            bs.Run();
        }
    }
}
