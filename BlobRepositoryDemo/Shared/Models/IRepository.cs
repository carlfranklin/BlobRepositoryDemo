using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BlobRepositoryDemo.Shared.Models
{
    public interface IRepository<TEntity> where TEntity : class 
    {
        Task<bool> Delete(TEntity EntityToDelete);
        Task<bool> Delete(object Id);
        Task DeleteAll(); // Be Careful!!!
        Task<IEnumerable<TEntity>> Get(
            Expression<Func<TEntity, bool>> Filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> OrderBy = null,
            string IncludeProperties = "");
        Task<IEnumerable<TEntity>> GetAll();
        Task<TEntity> GetById(object Id);
        Task<TEntity> Insert(TEntity Entity);
        Task<TEntity> Update(TEntity EntityToUpdate);
    }
}
