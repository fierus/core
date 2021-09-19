using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Core.Resources;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Core.Services
{
    public abstract class EntityRepositoryService<TEntity, TFilter>
        where TEntity : class, IEntityBase, new()
        where TFilter : class, IEntityFilter, new()
    {
        #region Properties

        private readonly IDbTransaction dbTransaction;

        #endregion Properties

        #region Ctors

        public EntityRepositoryService(IDbTransaction dbTransaction)
        {
            this.dbTransaction = dbTransaction ?? throw new ArgumentNullException(nameof(dbTransaction));
        }

        #endregion Ctors

        #region Public methods

        public virtual async Task<TEntity> GetAsync(Guid id, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null)
        {
            if (id.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(id));
            }

            await FindingAsync(id);

            var query = (IQueryable<TEntity>)dbTransaction.DbContext.Set<TEntity>();

            if (getAssociations != null)
            {
                query = getAssociations(query);
            }

            // Temp solution
            var entity = await query.SingleOrDefaultAsync(GetMatchById(id));

            _ = entity ?? throw new EntityNotFoundException(string.Format(Strings.AnEntityOfTypeWithIdWasNotFound, typeof(TEntity).Name, id));

            await FoundAsync(entity);

            return entity;
        }

        public async Task<TEntity> GetFirstAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null)
        {
            if (filter == null)
            {
                filter = new TFilter();
            }

            var page = new Page()
            {
                PageSize = 1
            };

            var entities = await ReadAsync(filter, null, page, getAssociations);

            return entities.Items.Single();
        }

        public async Task<TEntity> GetFirstOrDefaultAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null)
        {
            if (filter == null)
            {
                filter = new TFilter();
            }

            var page = new Page()
            {
                PageSize = 1
            };

            var entities = await ReadAsync(filter, null, page, getAssociations);

            return entities.Items.SingleOrDefault();
        }

        public async Task<TEntity> GetUniqueAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null)
        {
            if (filter == null)
            {
                filter = new TFilter();
            }

            var page = new Page()
            {
                PageSize = 2
            };

            var entities = await ReadAsync(filter, null, page, getAssociations);

            return entities.Items.Single();
        }

        public async Task<TEntity> GetUniqueOrDefaultAsync(TFilter filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null)
        {
            if (filter == null)
            {
                filter = new TFilter();
            }

            var page = new Page()
            {
                PageSize = 2
            };

            var entities = await ReadAsync(filter, null, page, getAssociations);

            return entities.Items.SingleOrDefault();
        }

        public async Task<PageResult<TEntity>> ReadAsync(TFilter filter = null, IEnumerable<Sort> sorts = null, Page page = null, Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null)
        {
            var result = new PageResult<TEntity>();

            if (filter == null)
            {
                filter = new TFilter();
            }

            if (sorts == null)
            {
                sorts = new List<Sort>();
            }
            if (sorts.Any(x => string.IsNullOrWhiteSpace(x.Field)))
            {
                throw new ArgumentException(Strings.ASortingFieldIsEitherNullOrEmpty, nameof(sorts));
            }

            if (page == null)
            {
                page = new Page();
            }
            if (!page.PageNumber.HasValue)
            {
                page.PageNumber = 1;
            }
            if (!page.PageSize.HasValue)
            {
                page.PageSize = int.MaxValue;
            }

            var suppressQuery = false;
            var filterExpression = GetMatchByFilter(filter, ref suppressQuery);

            if (!suppressQuery)
            {
                var pageResult = await FindAllPagedAsync(
                    filterExpression,
                    page.PageNumber.Value,
                    page.PageSize.Value,
                    GetOrderFields(sorts),
                    getAssociations);

                result.Items = pageResult.items;
                result.TotalCount = pageResult.count;
            }

            return result;
        }

        public async Task<int> CountAsync(TFilter filter)
        {
            var result = 0;

            if (filter == null)
            {
                filter = new TFilter();
            }

            var suppressQuery = false;
            var filterExpression = GetMatchByFilter(filter, ref suppressQuery);

            if (!suppressQuery)
            {
                result = await CountAsync(filterExpression);
            }

            return result;
        }

        public virtual async Task<bool> ExistsAsync(TFilter filter)
        {
            var result = false;

            if (filter == null)
            {
                filter = new TFilter();
            }

            var suppressQuery = false;
            var filterExpression = GetMatchByFilter(filter, ref suppressQuery);

            if (!suppressQuery)
            {
                result = await dbTransaction.DbContext.Set<TEntity>().AnyAsync(filterExpression);
            }

            return result;
        }

        public virtual async Task SaveAsync(TEntity entity, IDictionary<string, object> additionalParameters = null)
        {
            if (additionalParameters == null)
            {
                additionalParameters = new Dictionary<string, object>();
            }

            TEntity originalEntity = GetOriginalEntity(entity, additionalParameters);

            await ValidateAsync(entity, originalEntity, additionalParameters);

            bool suppressSave = false;

            await SavingAsync(entity, originalEntity, suppressSave, additionalParameters);

            if (!suppressSave)
            {
                if (entity.IsNew())
                {
                    entity = await AddAsync(entity);
                }
                else
                {
                    Update(entity);
                }

                await SaveAsync();
                await SavedAsync(entity, originalEntity, additionalParameters);
            }
        }

        public virtual async Task<TEntity> DeleteAsync(TEntity entity, IDictionary<string, object> additionalParameters = null)
        {
            if (additionalParameters == null)
            {
                additionalParameters = new Dictionary<string, object>();
            }

            await DeletingAsync(entity, additionalParameters);
            await RemoveAsync(entity);
            await SaveAsync();
            await DeletedAsync(entity, additionalParameters);

            return entity;
        }

        #endregion Public methods

        #region Protected methods

        protected virtual TEntity GetOriginalEntity(TEntity entity, IDictionary<string, object> additionalParameters)
        {
            return (TEntity)dbTransaction.DbContext
                .Entry(entity)
                .OriginalValues
                .ToObject();
        }

        protected virtual async Task ValidateAsync(TEntity entity, TEntity originalEntity, IDictionary<string, object> additionalParameters)
        {
            var validationContext = new ValidationContext(entity);

            Validator.ValidateObject(entity, validationContext, true);

            await Task.CompletedTask;
        }

        protected virtual async Task FindingAsync(Guid id)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task FoundAsync(TEntity entity)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task SavingAsync(TEntity entity, TEntity originalEntity, bool suppressSave, IDictionary<string, object> additionalParameters)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task SavedAsync(TEntity entity, TEntity originalEntity, IDictionary<string, object> additionalParameters)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task DeletingAsync(TEntity entity, IDictionary<string, object> additionalParameters)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task DeletedAsync(TEntity entity, IDictionary<string, object> additionalParameters)
        {
            await Task.CompletedTask;
        }

        protected virtual Expression<Func<TEntity, bool>> GetMatchById(Guid id)
        {
            return x => x.Id == id;
        }

        protected virtual Expression<Func<TEntity, bool>> GetMatchByFilter(TFilter filter, ref bool suppressQuery)
        {
            return x => true;
        }

        protected virtual List<(Expression<Func<TEntity, object>>, SortDirection)> GetOrderFields(IEnumerable<Sort> sorts)
        {
            return new List<(Expression<Func<TEntity, object>>, SortDirection)>();
        }

        #endregion Protected methods

        #region Private methods

        private async Task<(IEnumerable<TEntity> items, int count)> FindAllPagedAsync(
            Expression<Func<TEntity, bool>> match,
            int currentPage = 1,
            int pageSize = 10,
            IEnumerable<(Expression<Func<TEntity, object>> item, SortDirection direction)> orderFields = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> getAssociations = null)
        {
            var expression = (IQueryable<TEntity>)dbTransaction.DbContext.Set<TEntity>();

            if (getAssociations != null)
            {
                expression = getAssociations(expression);
            }

            expression = expression.Where(match);

            if (orderFields != null && orderFields.Any())
            {
                expression = Order(expression, orderFields);
            }

            // EF Core doesn't support multiple parallel operations being run on the same context instance.
            // Info: https://docs.microsoft.com/en-us/ef/core/querying/async
            var result = await expression.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();
            var count = await CountAsync(match);

            return (result, count);
        }

        private IOrderedQueryable<TEntity> Order(IQueryable<TEntity> expression, IEnumerable<(Expression<Func<TEntity, object>> item, SortDirection direction)> orders)
        {
            IOrderedQueryable<TEntity> orderedResult = null;

            var ordersArray = orders.ToArray();
            var orderBy = ordersArray[0];

            switch (orderBy.direction)
            {
                case SortDirection.Asc:
                    orderedResult = expression.OrderBy(orderBy.item);
                    break;
                case SortDirection.Desc:
                    orderedResult = expression.OrderByDescending(orderBy.item);
                    break;
            }

            if (orders.Count() > 1)
            {
                for (int i = 1; i < ordersArray.Count(); i++)
                {
                    orderBy = ordersArray[i];
                    switch (orderBy.direction)
                    {
                        case SortDirection.Asc:
                            orderedResult = orderedResult.ThenBy(orderBy.item);
                            break;
                        case SortDirection.Desc:
                            orderedResult = orderedResult.ThenByDescending(orderBy.item);
                            break;
                    }
                }
            }

            return orderedResult;
        }

        private async Task<int> CountAsync(Expression<Func<TEntity, bool>> filter)
        {
            return await dbTransaction.DbContext.Set<TEntity>()
                .AsNoTracking()
                .Where(filter)
                .CountAsync();
        }

        private async Task<int> SaveAsync()
        {
            return await dbTransaction.DbContext.SaveChangesAsync();
        }

        private async Task<TEntity> AddAsync(TEntity entity)
        {
            var result = await dbTransaction.DbContext
                .Set<TEntity>()
                .AddAsync(entity);

            return result.Entity;
        }

        private void Update(TEntity entity)
        {
            entity = entity ?? throw new ArgumentNullException(nameof(entity));

            dbTransaction.DbContext.Entry(entity).State = EntityState.Modified;
        }

        public virtual async Task RemoveAsync(TEntity entity)
        {
            dbTransaction.DbContext.Set<TEntity>().Remove(entity);

            await Task.CompletedTask;
        }

        #endregion
    }
}