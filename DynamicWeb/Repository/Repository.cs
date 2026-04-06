using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using MyERP.Models;

namespace MyERP.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected MySoftERPEntity _db;
        protected DbSet<T> ts;
        public Repository(MySoftERPEntity db)
        {
            _db = db;
            ts = _db.Set<T>();
        }
        public IQueryable<T> GetAll()
        {
            return ts;
        }

        public T GetById(int id)
        {
            return ts.Find(id);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await ts.FindAsync(id);
        }

        public virtual IEnumerable<T> GetByPaging(int take, int pageIndex)
        {
            return _db.Database.SqlQuery<T>($"select * from [{this.GetType().BaseType.GenericTypeArguments[0].Name}] where isdeleted=0").Skip(pageIndex > 1 ? take * pageIndex : 0).Take(take);
        }

        public T Insert(T entity)
        {
            ts.Add(entity);
            _db.SaveChanges();
            return entity;
        }

        public async Task<T> InsertAsync(T entity)
        {
            ts.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public bool SetIsDeletedTrue(int id)
        {
            T entity = GetById(id);
            if (entity == null)
                return false;
            entity.GetType().GetProperty("IsDeleted").SetValue(entity, true);
            return Update(entity);
        }

        public async Task<bool> SetIsDeletedTrueAsync(int id)
        {
            T entity =await GetByIdAsync(id);
            if (entity == null)
                return false;
            entity.GetType().GetProperty("IsDeleted").SetValue(entity, true);
            return await UpdateAsync(entity);
        }

        public bool Update(T entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return _db.SaveChanges() > 0;
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return await _db.SaveChangesAsync() > 0;
        }
    }
}