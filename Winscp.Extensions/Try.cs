using System;

namespace CTX.WinscpExtensions
{
    internal static class Try
    {
        public static GetResult<T> Get<T>(Func<T> factory, T @default)
        {
            try
            {
                return new GetResult<T>
                    {
                        Success = true,
                        Value = factory()
                    };
            }
            catch (Exception ex)
            {
                return new GetResult<T>
                    {
                        Exception = ex,
                        Success = false,
                        Value = @default
                    };
            }
        }

        public static Result Do(Action action)
        {
            try
            {
                action();
                return new Result { Success = true };
            }
            catch (Exception ex)
            {
                return new Result { Success = false, Exception = ex };
            }
        }

        internal class GetResult<T> : Result
        {
            public T Value { get; set; }
        }

        internal class Result
        {
            public bool Success { get; set; }
            public Exception Exception { get; set; }
        }
    }
}