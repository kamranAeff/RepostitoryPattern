using System;

namespace RepositoryPattern.Common
{
    public class AppException : Exception
    {
        public AppException(string message)
            : base(message)
        {

        }
    }
}
