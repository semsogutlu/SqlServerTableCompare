using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataWarehouse;

namespace SqlServerTableCompare
{
    public class UnMatchedData
    {
        public Guid SurveyId { get; set; }
        public string ColumnName { get; set; }
        public string OldResponse { get; set; }
        public string NewResponse { get; set; }
    }
}
