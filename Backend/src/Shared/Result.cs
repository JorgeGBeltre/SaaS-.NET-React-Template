using System;

namespace Shared
{
    public class Result
    {
        protected Result(bool isSuccess, string error)
        {
            if (isSuccess && error != string.Empty)
            {
                throw new InvalidOperationException("A successful result cannot have an error message.");
            }
            if (!isSuccess && error == string.Empty)
            {
                throw new InvalidOperationException("A failed result must have an error message.");
            }

            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        public static Result Success() => new(true, string.Empty);
        public static Result Failure(string error) => new(false, error);

        public static Result<TValue> Success<TValue>(TValue value) => new(value, true, string.Empty);
        public static Result<TValue> Failure<TValue>(string error) => new(default!, false, error);
    }

    public class Result<TValue> : Result
    {
        private readonly TValue _value;

        protected internal Result(TValue value, bool isSuccess, string error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        public TValue Value => IsSuccess
            ? _value
            : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

        public static implicit operator Result<TValue>(TValue value) => Success(value);
    }
}
