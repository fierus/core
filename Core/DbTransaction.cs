using Core.Interfaces;
using Core.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace Core
{
    public class DbTransaction : IDbTransaction
    {
        private IDbContextTransaction dbContextTransaction;

        public DbTransaction(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        public DbContext DbContext { get; }

        public void Begin()
        {
            if (dbContextTransaction != null)
            {
                throw new InvalidOperationException(Strings.AnotherTransactionHasAlreadyStarted);
            }

            dbContextTransaction = DbContext.Database.BeginTransaction();
        }

        public void Commit()
        {
            if (dbContextTransaction != null)
            {
                DbContext.SaveChanges();
                OnCommiting(EventArgs.Empty);
                dbContextTransaction.Commit();
                OnCommitted(EventArgs.Empty);
                Dispose();
            }
        }

        public void Rollback()
        {
            if (dbContextTransaction != null)
            {
                dbContextTransaction.Rollback();
                Dispose();
            }
        }

        public void Dispose()
        {
            if (dbContextTransaction != null)
            {
                dbContextTransaction.Dispose();
                dbContextTransaction = null;
            }
        }

        private void OnCommiting(EventArgs e)
        {
            Committing?.Invoke(this, e);
            Committing = null;
        }

        private void OnCommitted(EventArgs e)
        {
            Committed?.Invoke(this, e);
            Committed = null;
        }

        public event EventHandler Committing;
        public event EventHandler Committed;
    }
}