using Core.Extensions;
using Core.Interfaces;
using Core.Interfaces.Services;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Services
{
    public abstract class ModelRepositoryService<TEntity, TModel, TFilter>
        where TEntity : class, IEntityBase, new()
        where TModel : ModelBase, new()
        where TFilter : class, IEntityFilter, new()
    {
        public IEntityRepositoryService<TEntity, TFilter> EntityRepositoryService { get; }

        public ModelRepositoryService(IEntityRepositoryService<TEntity, TFilter> entityRepositoryService)
        {
            EntityRepositoryService = entityRepositoryService;
        }

        public async Task<TModel> GetAsync(Guid id)
        {
            var entity = await EntityRepositoryService.GetAsync(id, GetAssociations);

            var model = (await ToModels(new List<EntityModelPair<TEntity, TModel>>()
            {
                new EntityModelPair<TEntity, TModel>()
                {
                    Entity = entity,
                    Model = new TModel()
                }
            })).Single().Model;

            return model;
        }

        public async Task<PageResult<TModel>> ReadAsync(TFilter filter = null, IEnumerable<Sort> sorts = null, Page page = null)
        {
            var entitiesPageResult = await EntityRepositoryService.ReadAsync(filter, sorts, page, GetAssociations);
            var pairs = entitiesPageResult.Items.Select(entity => new EntityModelPair<TEntity, TModel>() { Entity = entity, Model = new TModel() }).ToList();

            var models = (await ToModels(pairs)).Select(x => x.Model);

            return new PageResult<TModel>()
            {
                Items = models,
                TotalCount = entitiesPageResult.TotalCount
            };
        }

        public async Task<TModel> CreateAsync(TModel model, IDictionary<string, object> additionalParameters = null)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var entity = new TEntity();

            entity = (await ToEntities(new List<EntityModelPair<TEntity, TModel>>()
            {
                new EntityModelPair<TEntity, TModel>()
                {
                    Entity = entity,
                    Model = model
                }
            })).Single().Entity;

            await EntityRepositoryService.SaveAsync(entity, additionalParameters);

            model = (await ToModels(new List<EntityModelPair<TEntity, TModel>>()
            {
                new EntityModelPair<TEntity, TModel>()
                {
                    Entity = entity,
                    Model = new TModel(),
                }
            })).Single().Model;

            return model;
        }

        public async Task<TModel> UpdateAsync(TModel model, IDictionary<string, object> additionalParameters = null)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (model.Id.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(model.Id));
            }

            var entity = await EntityRepositoryService.GetAsync(model.Id, GetAssociations);

            entity = (await ToEntities(new List<EntityModelPair<TEntity, TModel>>()
            {
                new EntityModelPair<TEntity, TModel>()
                {
                    Entity = entity,
                    Model = model
                }
            })).Single().Entity;

            await EntityRepositoryService.SaveAsync(entity, additionalParameters);

            var newModel = (await ToModels(new List<EntityModelPair<TEntity, TModel>>()
            {
                new EntityModelPair<TEntity, TModel>()
                {
                    Entity = entity,
                    Model = new TModel()
                }
            })).Single().Model;

            return newModel;
        }

        public async Task<TModel> DeleteAsync(Guid id)
        {
            var entity = await EntityRepositoryService.GetAsync(id, GetAssociations);

            var model = (await ToModels(new List<EntityModelPair<TEntity, TModel>>()
            {
                new EntityModelPair<TEntity, TModel>()
                {
                    Entity = entity,
                    Model = new TModel()
                }
            })).Single().Model;

            if (entity is ISoftDeletable)
            {
                ((ISoftDeletable)entity).IsDeleted = true;

                await EntityRepositoryService.SaveAsync(entity);
            }
            else
            {
                await EntityRepositoryService.DeleteAsync(entity);
            }

            return model;
        }

        protected virtual async Task<List<EntityModelPair<TEntity, TModel>>> ToModels(List<EntityModelPair<TEntity, TModel>> pairs)
        {
            foreach (var pair in pairs)
            {
                pair.Model.Id = pair.Entity.Id;

                var trEntity = pair.Entity as ITrackable;
                var trModel = pair.Model as ITrackable;

                if (trEntity != null && trModel != null)
                {
                    trModel.CreatedAt = trEntity.CreatedAt;
                    trModel.CreatedById = trEntity.CreatedById;
                    trModel.ModifiedAt = trEntity.ModifiedAt;
                    trModel.ModifiedById = trEntity.ModifiedById;
                }
            }

            return await Task.FromResult(pairs);
        }

        protected virtual async Task<List<EntityModelPair<TEntity, TModel>>> ToEntities(List<EntityModelPair<TEntity, TModel>> pairs)
        {
            return await Task.FromResult(pairs);
        }

        protected virtual IQueryable<TEntity> GetAssociations(IQueryable<TEntity> query)
        {
            return query;
        }
    }
}
