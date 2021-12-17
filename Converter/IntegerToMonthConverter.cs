using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AccountingHelper.Converter
{
    public class IntegerToMonthConverter : IValueConverter
    {
        string[] monthNames = { " ", "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value.ToString(), out int month))
            {
                return monthNames[month];
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //int month = 0;
            //string content = value.ToString();

            //foreach (string name in monthNames)
            //{
            //    if (name.Equals(monthNames[month])) return month;
            //    month++;
            //}
            //return 0;
            return "";
        }
    }
}
