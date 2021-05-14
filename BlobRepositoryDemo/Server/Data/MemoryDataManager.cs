using BlobRepositoryDemo.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace BlobRepositoryDemo.Server.Data
{
    public class MemoryDataManager<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private List<TEntity> Data;
        private PropertyInfo IdProperty = null;
        private string IdPropertyName = "";

        public MemoryDataManager(string idPropertyName)
        {
            IdPropertyName = idPropertyName;
            Data = new List<TEntity>();
            IdProperty = typeof(TEntity).GetProperty(idPropertyName);
        }

        public async Task<bool> Delete(TEntity EntityToDelete)
        {
            if (EntityToDelete == null) return false;

            await Task.Delay(0);
            try
            {
                if (Data.Contains(EntityToDelete))
                {
                    Data.Remove(EntityToDelete);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public async Task<bool> Delete(object Id)
        {
            try
            {
                var EntityToDelete = await GetById(Id);
                return await Delete(EntityToDelete);

            }
            catch { }
            return false;
        }

        public async Task DeleteAll()
        {
            await Task.Delay(0);
            Data.Clear();
        }

        public async Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>>
           filter = null, Func<IQueryable<TEntity>,
           IOrderedQueryable<TEntity>> orderBy = null,
           string includeProperties = "")
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Get the dbSet from the Entity passed in                
                    IQueryable<TEntity> query = Data.AsQueryable<TEntity>();

                    // Apply the filter
                    if (filter != null)
                    {
                        query = query.Where(filter);
                    }

                    // Sort
                    if (orderBy != null)
                    {
                        return orderBy(query).ToList();
                    }
                    else
                    {
                        return query.ToList();
                    }
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    return null;
                }
            });
        }

        public async Task<IEnumerable<TEntity>> GetAll()
        {
            await Task.Delay(0);
            return Data;
        }

        public async Task<TEntity> GetById(object Id)
        {
            await Task.Delay(0);
            if (IdProperty == null) return default(TEntity);
            TEntity entity = null;
            if (IdProperty.PropertyType.IsValueType)
            {
                entity = (from x in Data
                          where IdProperty.GetValue(x).ToString() == Id.ToString()
                          select x).FirstOrDefault();
            }
            else
            {
                entity = (from x in Data
                          where IdProperty.GetValue(x) == Id
                          select x).FirstOrDefault();
            }
            return entity;
        }

        public async Task<TEntity> Insert(TEntity Entity)
        {
            await Task.Delay(0);
            if (Entity == null) return default(TEntity);
            try
            {
                Data.Add(Entity);
                return Entity;
            }
            catch { }
            return default(TEntity);
        }

        public async Task<TEntity> Update(TEntity EntityToUpdate)
        {
            await Task.Delay(0);
            if (EntityToUpdate == null) return default(TEntity);
            if (IdProperty == null) return default(TEntity);
            try
            {
                var id = IdProperty.GetValue(EntityToUpdate);
                var entity = await GetById(id);
                if (entity != null)
                {
                    var index = Data.IndexOf(entity);
                    Data[index] = EntityToUpdate;
                    return EntityToUpdate;
                }
                else
                    return default(TEntity);
            }
            catch { }
            return default(TEntity);
        }
    }
}