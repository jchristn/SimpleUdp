namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class AssertEx
    {
        public static void True(bool condition, string message)
        {
            if (!condition) throw new TestAssertionException(message);
        }

        public static void False(bool condition, string message)
        {
            if (condition) throw new TestAssertionException(message);
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new TestAssertionException(message + " Expected <" + expected + "> but found <" + actual + ">.");
            }
        }

        public static void NotEqual<T>(T unexpected, T actual, string message)
        {
            if (EqualityComparer<T>.Default.Equals(unexpected, actual))
            {
                throw new TestAssertionException(message + " Did not expect <" + actual + ">.");
            }
        }

        public static void Null(object? value, string message)
        {
            if (value != null) throw new TestAssertionException(message + " Expected null.");
        }

        public static void NotNull(object? value, string message)
        {
            if (value == null) throw new TestAssertionException(message + " Expected a non-null value.");
        }

        public static void Contains<T>(T expected, IEnumerable<T> values, string message)
        {
            if (!values.Contains(expected))
            {
                throw new TestAssertionException(message + " Missing <" + expected + ">.");
            }
        }

        public static void SequenceEqual(byte[] expected, byte[] actual, string message)
        {
            if (!expected.SequenceEqual(actual))
            {
                throw new TestAssertionException(message + " Byte sequences differed.");
            }
        }

        public static TException Throws<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new TestAssertionException(message + " Expected " + typeof(TException).Name + ".");
        }

        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new TestAssertionException(message + " Expected " + typeof(TException).Name + ".");
        }
    }
}
