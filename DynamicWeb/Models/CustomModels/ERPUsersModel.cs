using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyERP.Models.CustomModels
{
    public class ERPUsersModel
    {

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public Nullable<int> EmployeeId { get; set; }
        public Nullable<int> RoleId { get; set; }
        public Nullable<int> CustodyBoxId { get; set; }
        public Nullable<bool> SystemAdmin { get; set; }
        public Nullable<int> UserId { get; set; }
        public Nullable<bool> IsDeleted { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsCashier { get; set; }
        public string Name { get; set; }
        public Nullable<bool> ShowDashBoardForUser { get; set; }
        public Nullable<bool> EnableTwoFactorAuthentication { get; set; }
        public string AppPassword { get; set; }
        public Nullable<bool> EnableAppPassword { get; set; }
        public string VerificationCode { get; set; }
        public Nullable<bool> IsWaiter { get; set; }
        public virtual EmployeesGroup EmployeesGroup { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UserPrivilege> UserPrivileges { get; set; }
        public virtual ERPRole ERPRole { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual List<GetUserDepartments_Result> UserDepartments { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual List<GetUserWarehouses_Result> UserWareHouses { get; set; }
        public virtual List<GetUserCashBoxes_Result> UserCashboxes { get; set; }
        public virtual List<GetAllUserCashBox_Result> AllCashBoxes { get; set; }
        public virtual List<GetAllUserDepartment_Result> AllDepartments { get; set; }
        public virtual List<GetAllUserWareHouse_Result> AllWareHouses { get; set; }
        public virtual List<GetAllUserPos_Result> AllPos { get; set; }
        public virtual List<GetUserPos_Result> UserPos { get; set; }

    }
}