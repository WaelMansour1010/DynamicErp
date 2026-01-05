using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EazyCash.Auth
{
    public class LoginViewModel
    {
       
            [Required]
            public string EmployeeCode { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
            public List<SelectListItem> Users { set; get; }
            public string  EmployeeName { get; set; }
    }
}
