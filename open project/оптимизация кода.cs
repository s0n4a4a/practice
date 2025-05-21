using System;
using System.Collections.Generic;
using System.Linq;

namespace Sprache
{
    public static class Result
    {
        public static IResult<T> Success<T>(T value, IInput remainder)
        {
            if (remainder == null)
                throw new ArgumentNullException(nameof(remainder));

            return new Result<T>(value, remainder);
        }

        public static IResult<T> Failure<T>(IInput remainder, string message, IEnumerable<string> expectations)
        {
            if (remainder == null)
                throw new ArgumentNullException(nameof(remainder));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (expectations == null)
                throw new ArgumentNullException(nameof(expectations));

            return new Result<T>(remainder, message, expectations);
        }
    }

    internal class Result<T> : IResult<T>
    {
        private readonly T _value;
        private readonly IInput _remainder;
        private readonly bool _wasSuccessful;
        private readonly string _message;
        private readonly IReadOnlyCollection<string> _expectations;
        private string _cachedToString;

        public Result(T value, IInput remainder)
        {
            _value = value;
            _remainder = remainder ?? throw new ArgumentNullException(nameof(remainder));
            _wasSuccessful = true;
            _message = null;
            _expectations = Array.Empty<string>();
        }

        public Result(IInput remainder, string message, IEnumerable<string> expectations)
        {
            _value = default;
            _remainder = remainder ?? throw new ArgumentNullException(nameof(remainder));
            _wasSuccessful = false;
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _expectations = (expectations ?? throw new ArgumentNullException(nameof(expectations)))
                          .ToList().AsReadOnly();
        }

        public T Value => _wasSuccessful 
            ? _value 
            : throw new InvalidOperationException("No value can be computed from failed result.");

        public bool WasSuccessful => _wasSuccessful;
        public string Message => _message;
        public IEnumerable<string> Expectations => _expectations;
        public IInput Remainder => _remainder;

        public override string ToString()
        {
            if (_cachedToString != null)
                return _cachedToString;

            if (WasSuccessful)
            {
                _cachedToString = $"Successful parsing of {Value}.";
                return _cachedToString;
            }

            var expMsg = _expectations.Count > 0
                ? " expected " + string.Join(" or ", _expectations)
                : string.Empty;

            var recentlyConsumed = CalculateRecentlyConsumed();
            
            _cachedToString = $"Parsing failure: {Message}{expMsg} ({Remainder}); recently consumed: {recentlyConsumed}";
            return _cachedToString;
        }

        private string CalculateRecentlyConsumed()
        {
            const int windowSize = 10;
            var totalConsumedChars = Remainder.Position;
            var windowStart = Math.Max(totalConsumedChars - windowSize, 0);
            var numberOfRecentlyConsumedChars = totalConsumedChars - windowStart;

            return numberOfRecentlyConsumedChars > 0
                ? Remainder.Source.Substring(windowStart, numberOfRecentlyConsumedChars)
                : string.Empty;
        }
    }
}