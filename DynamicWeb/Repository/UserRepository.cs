using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MyERP.Repository
{
    public class UserRepository : Repository<ERPUser>
    {
        public UserRepository(MySoftERPEntity db) : base(db)
        {
        }

        public async Task<bool> HasActionPrivilege(int userId, string action, string enName)
        {
            return userId == 1 ? true : await _db.UserPrivileges.Where(u => u.PageAction.EnName == enName && u.PageAction.Action == action && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync() == true;
        }
    }
}