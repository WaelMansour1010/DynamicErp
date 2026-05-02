using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MyERP.Repository
{
    public class DepartmentRepository : Repository<Department>
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        public DepartmentRepository(MySoftERPEntity db) : base(db)
        {
        }

        public IQueryable<dynamic> UserDepartments(int userId)
        {
            var lang = db.ERPUsers.Find(userId).Language;

            if (userId == 1)
                return ts.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = lang == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                });
            else
                return _db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = lang == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                });
        }

        public async Task<List<int?>> UserDepartmentsIds(int userId)
        {
            if (userId == 1)
                return await ts.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b =>
                    (int?)b.Id).ToListAsync();
            else
                return await _db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => b.DepartmentId).ToListAsync();
        }
    }
}