using System;
using System.Diagnostics.CodeAnalysis;

namespace ClearCareOnline.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class LocationResponse
    {
        public int id { get; set; }
        public string url { get; set; }
        public string agency { get; set; }
        public string name { get; set; }
        public string payroll_dept_code { get; set; }
        public string payroll_branch_id { get; set; }
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
    }
}