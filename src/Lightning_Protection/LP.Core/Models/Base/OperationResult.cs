using System;

namespace LP.Core.Models.Base
{
    public class OperationResult<T> where T : class
    {
        public T Result { get; private set; }
        public string Message { get; private set; }
        public bool IsSuccess { get; private set; }
        public Exception Exception { get; private set; }

        private OperationResult(T result, string message, bool isSuccess, Exception exception)
        {
            Result = result;
            Message = message;
            IsSuccess = isSuccess;
            Exception = exception;
        }

        public static OperationResult<T> Ok(T result, string message = null)
        {
            return new OperationResult<T>(result, message, true, null);
        }

        public static OperationResult<T> Fail(string message, Exception exception = null)
        {
            return new OperationResult<T>(default, message, false, exception);
        }
    }
}
