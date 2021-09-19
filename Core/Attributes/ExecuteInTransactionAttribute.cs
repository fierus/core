using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ExecuteInTransactionAttribute : ServiceFilterAttribute
    {
        public ExecuteInTransactionAttribute()
            : base(typeof(ExecuteInTransactionImplAttribute))
        {
        }
    }

    public class ExecuteInTransactionImplAttribute : IActionFilter
    {
        private readonly IDbTransaction dbTransaction;

        public ExecuteInTransactionImplAttribute(IDbTransaction dbTransaction)
        {
            this.dbTransaction = dbTransaction;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            dbTransaction.Begin();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception == null)
            {
                dbTransaction.Commit();
            }
            else
            {
                dbTransaction.Rollback();
            }
        }
    }
}