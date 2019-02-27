using RepositoryPattern.Attributes;
using RepositoryPattern.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;

namespace RepositoryPattern.Extensions
{
    public static partial class Extension
    {
        static string GetDirectionHint(this ParameterDirection direction)
        {
            switch (direction)
            {
                
                case ParameterDirection.Output:
                case ParameterDirection.InputOutput:
                    return " out";
                case ParameterDirection.ReturnValue:
                case ParameterDirection.Input:
                    return "";
                default:
                    throw new NotImplementedException();
            }
        }
        static public T ExecuteScalar<T>(this IUnitOfWork uow, string sqlCommand, List<SqlParameter> parameters)
        {
            try
            {
                return uow.Context.Database.SqlQuery<T>(sqlCommand, parameters.ToArray()).FirstOrDefault();
            }
            catch
            {
                throw;
            }
            finally
            {
                parameters.Clear();
            }
        }

        static public IEnumerable<T> ExecuteProcedure<T>(this IUnitOfWork uow, string procedure, [Optional]List<SqlParameter> parameters) where T : class
        {
            try
            {
                if (parameters == null)
                    return uow.Context.Database.SqlQuery<T>(procedure).ToList();

                string parameterList = string.Join(",", parameters.ToList().Select(x => string.Format("{0}={0}{1}", x.ParameterName, x.Direction.GetDirectionHint())));
                procedure = string.Format("{0} {1}", procedure, parameterList);

                return uow.Context.Database.SqlQuery<T>(procedure, parameters.ToArray()).ToList();
            }
            catch (System.Exception ex)
            {
                throw;
            }
            finally
            {
            }
        }

        static public IEnumerable<T> ExecuteProcedure2<T>(this IUnitOfWork uow, string procedure, [Optional]List<SqlParameter> parameters) where T : class
        {
            try
            {
                if (parameters == null)
                    return uow.Context.Database.SqlQuery<T>(procedure).ToList();

                return uow.Context.Database.SqlQuery<T>(procedure, parameters.ToArray()).ToList();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        static public IEnumerable<T> ExecuteFunction<T>(this IUnitOfWork uow, string procedure, [Optional]List<SqlParameter> parameters) where T : class
        {
            try
            {
                if (parameters == null)
                    return uow.Context.Database.SqlQuery<T>(procedure).ToList();

                var result = uow.Context.Database.SqlQuery<T>(procedure, parameters).ToList();

                return result;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        static public void ExecuteNonQueryProcedure(this IUnitOfWork uow, string procedure, [Optional]List<SqlParameter> parameters) 
        {
            using (var cmd = new SqlCommand(procedure, (SqlConnection)uow.Context.Database.Connection) { CommandType = CommandType.StoredProcedure })
            {
                try
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters.ToArray());
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();

                }
                catch
                {

                    throw;
                }
                finally
                {
                    cmd.Connection.Close();
                }
            }
        }

       
    }
}
