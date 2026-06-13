using System.ComponentModel.DataAnnotations;

namespace aspnet_get_started.Models
{
    public class XenditPaymentViewModel
    {
        [Required]
        [Display(Name = "Account number")]
        public string AccountNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string CustomerEmail { get; set; }

        [Required]
        [Range(1, 100000000)]
        [Display(Name = "Amount due")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Plan or invoice description")]
        public string Description { get; set; }
    }
}
