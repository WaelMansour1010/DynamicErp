using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyERP.Repository
{
    public class CashboxReposistory : Repository<CashBox>
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        public CashboxReposistory(MySoftERPEntity db) : base(db)
        {
        }

        public IQueryable<dynamic> UserCashboxes(int userId, int? departmentId)
        {
            var lang = db.ERPUsers.Find(userId).Language;
            if (userId == 1)
                return ts.Where(b => b.IsActive == true && b.IsDeleted == false && (b.DepartmentId == departmentId)).Select(b => new
                {
                    b.Id,
                    ArName = lang == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                });
            else
                return _db.UserCashBoxes.Where(b => b.UserId == userId && b.Privilege == true && b.CashBox.IsActive == true && b.CashBox.IsDeleted == false && (departmentId == null || b.CashBox.DepartmentId == departmentId)).Select(b => new
                {
                    Id = b.CashBoxId,
                    ArName = lang == "en" && b.CashBox.EnName != null ? b.CashBox.Code + " - " + b.CashBox.EnName : b.CashBox.Code + " - " + b.CashBox.ArName
                });
        }
    }
}