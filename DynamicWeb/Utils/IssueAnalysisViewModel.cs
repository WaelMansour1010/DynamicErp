using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Utils
{
    public class IssueAnalysisViewModel
    {
        public IssueAnalysis issueAnalysis;
        public SelectList IssueAnalysisAccountId { get; set; } // Dropdown data source
    }
}