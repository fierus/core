using Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface IModelRepositoryService<TEntity, TModel, TFilter>
        where TEntity : class
        where TModel : ModelBase
        where TFilter : class, IEntityFilter
    {
        IEntityRepositoryService<TEntity, TFilter> EntityRepositoryService { get; }

        Task<TModel> GetAsync(Guid id);
        Task<PageResult<TModel>> ReadAsync(TFilter filter = null, IEnumerable<Sort> sorts = null, Page page = null);
        Task<TModel> CreateAsync(TModel model, IDictionary<string, object> additionalParameters = null);
        Task<TModel> UpdateAsync(TModel model, IDictionary<string, object> additionalParameters = null);
        Task<TModel> DeleteAsync(Guid id);
    }
}