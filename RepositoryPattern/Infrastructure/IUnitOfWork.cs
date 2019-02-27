using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace RepositoryPattern.Infrastructure
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> GetRepository<T>() where T : class;
        DbContext Context { get; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
