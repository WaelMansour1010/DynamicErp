using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyERP.Repository
{
    public class WarehouseRepository : Repository<Warehouse>
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        public WarehouseRepository(MySoftERPEntity db) : base(db)
        {
        }

        public IQueryable<dynamic> UserWarehouses(int userId, int? departmentId)
        {
            var lang = db.ERPUsers.Find(userId).Language;
            if (userId == 1)
                return ts.Where(b => b.IsActive == true && b.IsDeleted == false && (departmentId == null || b.DepartmentId == departmentId)).Select(b => new
                {
                    b.Id,
                    ArName = lang == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                });
            else
                return _db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && (departmentId==null || b.Warehouse.DepartmentId == departmentId)).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = lang == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                });
        }
    }
}