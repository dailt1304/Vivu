using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Domain.Shared
{
    public class Result<T> : Result
    {
        private readonly T? _value;

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value of a failed result.");

        protected internal Result(T? value, bool isSuccess, Error error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        public static Result<T> Success(T value) => new(value, true, Error.None);
        public static new Result<T> Failure(Error error) => new(default, false, error);

        public static implicit operator Result<T>(T value) => Success(value);

        public static implicit operator Result<T>(Error error) => Failure(error);
    }
}
