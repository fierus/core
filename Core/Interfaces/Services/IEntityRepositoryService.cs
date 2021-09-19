using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface IEntityRepositoryService<TEntity, TFilter>
        where TEntity : class
        where TFilter : class, IEntityFilter
    {
        Task<PageResult<TEntity>> ReadAsync(
            TFilter filter = null,
            IEnumerable<Sort> sorts = null,
            Page page = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null);

        Task<TEntity> GetAsync(Guid id, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null);
        Task<TEntity> GetFirstAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null);
        Task<TEntity> GetFirstOrDefaultAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null);
        Task<TEntity> GetUniqueAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null);
        Task<TEntity> GetUniqueOrDefaultAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null);
        Task<int> CountAsync(TFilter filter);
        Task<bool> ExistsAsync(TFilter filter);
        Task SaveAsync(TEntity entity, IDictionary<string, object> additionalParameters = null);
        Task<TEntity> DeleteAsync(TEntity entity, IDictionary<string, object> additionalParameters = null);
    }
}