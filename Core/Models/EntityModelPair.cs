namespace Core.Models
{
    public class EntityModelPair<TEntity, TModel>
        where TEntity : class
        where TModel : class
    {
        public TEntity Entity { get; set; }
        public TModel Model { get; set; }
    }
}