using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyERP.Repository
{
    public interface IRepository<T> where T:class
    {
        Task<T> GetByIdAsync(int id);
        T GetById(int id);
        IQueryable<T> GetAll();
        Task<bool> SetIsDeletedTrueAsync(int id);
        bool SetIsDeletedTrue(int id);
        Task<T> InsertAsync(T entity);
        T Insert(T entity);
        Task<bool> UpdateAsync(T entity);
        bool Update(T entity);
    }
}