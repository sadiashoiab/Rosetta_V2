using System;
using System.Diagnostics.CodeAnalysis;

namespace ClearCareOnline.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class AgencyResponse
    {
        public int id { get; set; }
        public string url { get; set; }
        public string activities { get; set; }
        public string caregivers { get; set; }
        public string caregiver_unavailability { get; set; }
        public string care_logs { get; set; }
        public string locations { get; set; }
        public string tasks { get; set; }
        public string task_logs { get; set; }
        public string invoices { get; set; }
        public string clients { get; set; }
        public string applicants { get; set; }
        public string referral_sources { get; set; }
        public string referral_source_types { get; set; }
        public string client_contacts { get; set; }
        public string prospects { get; set; }
        public string shifts { get; set; }
        public string activity_tags { get; set; }
        public string profile_tags { get; set; }
        public string rate_categories { get; set; }
        public string billing_policy { get; set; }
        public string pay_policy { get; set; }
        public string unassigned_bill_rates { get; set; }
        public string unassigned_pay_rates { get; set; }
        public string name { get; set; }
        public string subdomain { get; set; }
        public string address { get; set; }
        public string address_line_2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
        public string phone_number { get; set; }
        public string fax_number { get; set; }
        public string website { get; set; }
        public string logo_url { get; set; }
        public string default_mileage_bill_rate { get; set; }
        public string default_mileage_pay_rate { get; set; }
        public string caregiver_certifications { get; set; }
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
    }
}