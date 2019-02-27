using RepositoryPattern.Attributes;
using RepositoryPattern.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RepositoryPattern.Repository
{
    /// <summary>
    /// EntityFramework ucun hazirladigimiz bu repositorini evvelceden hazirladigimniz generik IRepository interfeysi vasitesi ile implementasiya edirik.
    /// bu yanasmamizin esas sebebi VBIS-den asililigi azaldaraq daha boyuk oblastli tetbiqler hazirlamaqdir.Misal ucun sabah MongoDb,MySql ucun de uygun olaraq Repository sinfi(MySqlRepository ve s.) hazirlayib implement ede bilerik
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EFRepository<T> : IRepository<T> where T : class
    {
        private readonly DbContext dbContext;
        private readonly DbSet<T> dbSet;

        public EFRepository(DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException("dbContext can not be null.");

            this.dbContext = dbContext;
            this.dbSet = this.dbContext.Set<T>();
        }

        #region IRepository Members
        public IQueryable<T> GetAll()
        {
            return dbSet.AsNoTracking<T>();
        }

        public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return GetAll();

            return dbSet.Where(predicate).AsNoTracking<T>();
        }
        public Task<IQueryable<T>> GetAllAsync()
        {
            return Task.FromResult(GetAll());
        }

        public Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            return Task.FromResult(GetAll(predicate));
        }

        public T GetById(int id)
        {
            return dbSet.Find(id);
        }

        public T Get(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return dbSet.AsNoTracking().SingleOrDefault();

            return dbSet.Where(predicate).AsNoTracking().SingleOrDefault();
        }
        public Task<T> GetByIdAsync(int id)
        {
            return dbSet.FindAsync(id);
        }

        public Task<T> GetAsync(Expression<Func<T, bool>> predicate)
        {
            return Task.FromResult(Get(predicate));
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        public void Update(T entity)
        {
            dbSet.Attach(entity);
            dbContext.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(T entity)
        {
            // eger melumati fiziki olaraq silmek evezine silindi kimi isarelemeyi istesek onda Asagidaki kimi sade refleksiya mentiqi ile bu meseleni hell ede bilerik
            if (entity.GetType().GetProperty("IsDelete") != null)
            {
                //T _entity = entity;

                entity.GetType().GetProperty("IsDelete").SetValue(entity, true);

                this.Update(entity);
            }
            else
            {
                DbEntityEntry dbEntityEntry = dbContext.Entry(entity);

                if (dbEntityEntry.State != EntityState.Deleted)
                {
                    dbEntityEntry.State = EntityState.Deleted;
                }
                else
                {
                    dbSet.Attach(entity);
                    dbSet.Remove(entity);
                }
            }
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity == null) return;
            else
            {
                if (entity.GetType().GetProperty("IsDelete") != null)
                {
                    //T _entity = entity;
                    entity.GetType().GetProperty("IsDelete").SetValue(entity, true);

                    this.Update(entity);
                }
                else
                {
                    Delete(entity);
                }
            }
        }


        /// <summary>
        /// Bir nece entitinin birden silinmesi
        /// </summary>
        /// <param name="predicate">Sogu filteri</param>
        public void Delete(Expression<Func<T, bool>> predicate)
        {
            GetAll(predicate)
                .ToList()
                .ForEach(entity => Delete(entity));
        }

        public Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return Task.FromResult(dbSet.Count(predicate));
        }

        public IEnumerable<T> ExecuteProcedure<D>(string procedure, D searchTmp) where D : class
        {
            var pmt = new List<SqlParameter>();
            try
            {
                if (searchTmp == null)
                    return dbContext.Database.SqlQuery<T>(procedure).ToList();


                var props = searchTmp.GetType().GetProperties()
                    .Where(prop => prop.GetValue(searchTmp, null) != null && prop.GetCustomAttributes(typeof(ParamAttribute), false).Any());

                    props.Select(prop => new { Value = prop.GetValue(searchTmp), Attribute = prop.GetCustomAttributes(typeof(ParamAttribute), false).First() as ParamAttribute })
                    .ToList()
                    .ForEach(prop => {
                        pmt.Add(prop.Attribute.Size.HasValue ?
                            new SqlParameter(prop.Attribute.Name, prop.Attribute.Type, prop.Attribute.Size.Value) { Value = prop.Value, Direction = prop.Attribute.Direction }
                          : new SqlParameter(prop.Attribute.Name, prop.Attribute.Type) { Value = prop.Value, Direction = prop.Attribute.Direction });
                    });

                    var result = dbContext.Database.SqlQuery<T>(string.Concat(procedure
                        , " "
                        , string.Join(",", pmt.Where(p => p.Direction != ParameterDirection.ReturnValue)
                        .Select(p => string.Format("{0}={0}{1}", p.ParameterName, (p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output) ? " out" : ""))))
                        , pmt.ToArray()).ToList();

                    foreach (var prop in props
                        .Select(v => new { Property = v, Attribute = v.GetCustomAttributes(typeof(ParamAttribute), false).First() as ParamAttribute })
                        .Where(p => p.Attribute.Direction != ParameterDirection.Input && p.Property.GetValue(searchTmp, null) != null))
                    {
                        prop.Property.SetValue(searchTmp, pmt.Find(p => p.ParameterName == prop.Attribute.Name).Value);
                    }

                    return result;
            }
            catch
            {
                throw;
            }
            finally
            {
                pmt.Clear();
                pmt = null;
            }
        }

        public IEnumerable<T> ExecuteFunction<D>(string procedure, D searchTmp) where D : class
        {
            var pmt = new List<SqlParameter>();
            try
            {
                var props = searchTmp.GetType().GetProperties()
                    .Where(prop => prop.GetValue(searchTmp, null) != null && prop.GetCustomAttributes(typeof(ParamAttribute), false).Any());

                props.Select(prop => new { Value = prop.GetValue(searchTmp), Attribute = prop.GetCustomAttributes(typeof(ParamAttribute), false).First() as ParamAttribute })
                .ToList()
                .ForEach(prop => {
                    pmt.Add(prop.Attribute.Size.HasValue ?
                        new SqlParameter(prop.Attribute.Name, prop.Attribute.Type, prop.Attribute.Size.Value) { Value = prop.Value, Direction = prop.Attribute.Direction }
                      : new SqlParameter(prop.Attribute.Name, prop.Attribute.Type) { Value = prop.Value, Direction = prop.Attribute.Direction });
                });

                var result = dbContext.Database.SqlQuery<T>(procedure, pmt.ToArray()).ToList();

                return result;
            }
            catch
            {
                throw;
            }
            finally
            {
                pmt.Clear();
                pmt = null;
            }
        }

        public void ExecuteNonQueryProcedure<D>(string procedure, D searchTmp) where D : class
        {
            var pmt = new List<SqlParameter>();
            try
            {
                var props = searchTmp.GetType().GetProperties()
                    .Where(prop => prop.GetValue(searchTmp, null) != null && prop.GetCustomAttributes(typeof(ParamAttribute), false).Any());

                props.Select(prop => new { Value = prop.GetValue(searchTmp), Attribute = prop.GetCustomAttributes(typeof(ParamAttribute), false).First() as ParamAttribute })
                .ToList()
                .ForEach(prop => {
                    pmt.Add(prop.Attribute.Size.HasValue ?
                        new SqlParameter(prop.Attribute.Name, prop.Attribute.Type, prop.Attribute.Size.Value) { Value = prop.Value, Direction = prop.Attribute.Direction }
                      : new SqlParameter(prop.Attribute.Name, prop.Attribute.Type) { Value = prop.Value, Direction = prop.Attribute.Direction });
                });

                using (var cmd = new SqlCommand(procedure, new SqlConnection(this.dbContext.Database.Connection.ConnectionString)) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddRange(pmt.ToArray());
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }
                
                foreach (var prop in props
                    .Select(v => new { Property = v, Attribute = v.GetCustomAttributes(typeof(ParamAttribute), false).First() as ParamAttribute })
                    .Where(p => p.Attribute.Direction != ParameterDirection.Input))
                {
                    prop.Property.SetValue(searchTmp,pmt.Find(p => p.ParameterName == prop.Attribute.Name).Value);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                pmt.Clear();
                pmt = null;
            }
        }

        #endregion
    }
}
