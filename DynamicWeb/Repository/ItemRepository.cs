using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MyERP.Repository
{
    public class ItemRepository : Repository<Warehouse>
    {
        public ItemRepository(MySoftERPEntity db) : base(db)
        {
        }

        public async Task<decimal> GetItemAvgPrice(int itemId, int departmentId)
        {
            return await _db.Database.SqlQuery<decimal>("select dbo.Item_AvgCost(@itemId, @departmentId)", new SqlParameter("@itemId", itemId), new SqlParameter("@departmentId", departmentId)).FirstOrDefaultAsync();
        }

        public async Task<dynamic> GetItemAvgPriceBulk(List<int> itemIds, int departmentId)
        {
            var avgCosts =await Task.Run(()=> itemIds.Distinct().Select(x => new
            {
                ItemId = x,
                AvgCost = _db.Database.SqlQuery<decimal>("select dbo.Item_AvgCost(@itemId, @departmentId)", new SqlParameter("@itemId", x), new SqlParameter("@departmentId", departmentId)).FirstOrDefault()
            }));
            return avgCosts;
        }
    }
}