using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EazyCash.Models
{
    public class UserViewModel

    {

        public virtual string Id { get; set; }
        [Required]
        //[EmailAddress]
        [Display(Name = "User Name")]
        public virtual string UserName { get; set; }
        [Required]
        [Display(Name = "Branch")]
        public virtual string BranchId { get; set; }
        
        public string? BranchName { get; set; }

        public virtual bool IsAdmin { get; set; }

        public virtual bool IsActive { get; set; }

        public List<SelectListItem> Branches { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string  ConfirmPassword { get; set; }
        [Required]
        [StringLength(6, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        public List<EmployeeScreenModel> Screens
        {
            get;
            set;
        }

    }


    public class EmployeeScreenModel
    {
       
            public EmployeeScreenModel()
            {
                this.RowID = Guid.NewGuid();
            }

            public Guid RowID { get; set; }
            public bool? CanAdd { get; set; }
            public bool? CanEdit { get; set; }
            public bool? CanShow { get; set; }
            public int? Emp_ID { get; set; }
            public string ScreenName { get; set; }
        

    }


}
