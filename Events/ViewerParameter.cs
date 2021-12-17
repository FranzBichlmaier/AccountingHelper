using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Reporting;

namespace AccountingHelper.Events
{
    public class ViewerParameter
    {
        public TypeReportSource typeReportSource { get; set; } = null;
        public JsonDataSource JsonDataSource { get; set; } = null;
        public ReportBook reportBook { get; set; } = null;
    }
}
