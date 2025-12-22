using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BareORM.Core
{


    /// <summary>
    /// Guard helpers for argument validation.
    /// Keep it tiny, dependency-free, and fast.
    /// </summary>
    /// <example>
    /// Guard.NotNullOrEmpty(name, nameof(name));          // permite "   " (whitespace OK)
    /// Guard.NotNullOrWhiteSpace(name, nameof(name));     // NO permite "   "
    /// Guard.MaxLength(name, 50, nameof(name));
    /// Guard.MatchesRegex(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", nameof(email));
    /// </example>
    internal static class Guard
    {
        // Regex cache to avoid recompiling patterns repeatedly.
        private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

        public static T NotNull<T>(T? value, string paramName) where T : class
            => value ?? throw new ArgumentNullException(paramName);

        // --- String guards ---

        public static string NotNullOrEmpty(string? value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);

            if (value.Length == 0)
                throw new ArgumentException("Value cannot be empty.", paramName);

            return value;
        }

        public static string NotNullOrWhiteSpace(string? value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);

            return value;
        }

        public static string MaxLength(string value, int maxLength, string paramName)
        {
            NotNull(value, paramName);
            Positive(maxLength, nameof(maxLength));

            if (value.Length > maxLength)
                throw new ArgumentException($"Value length cannot exceed {maxLength}. Actual: {value.Length}.", paramName);

            return value;
        }

        public static string MatchesRegex(string value, string pattern, string paramName, RegexOptions options = RegexOptions.None)
        {
            NotNull(value, paramName);
            NotNullOrEmpty(pattern, nameof(pattern));

            // Always add culture-invariant; compiled is default in cache.
            var cacheKey = $"{pattern}||{(int)(options | RegexOptions.CultureInvariant | RegexOptions.Compiled)}";
            var regex = _regexCache.GetOrAdd(cacheKey, _ =>
                new Regex(pattern, options | RegexOptions.CultureInvariant | RegexOptions.Compiled));

            if (!regex.IsMatch(value))
                throw new ArgumentException("Value does not match the required pattern.", paramName);

            return value;
        }

        // --- Numeric / range guards ---

        public static int Positive(int value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, value, "Value must be > 0.");

            return value;
        }

        public static int NonNegative(int value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, value, "Value must be >= 0.");

            return value;
        }

        public static int InRange(int value, int minInclusive, int maxInclusive, string paramName)
        {
            if (minInclusive > maxInclusive)
                throw new ArgumentException("minInclusive cannot be greater than maxInclusive.", nameof(minInclusive));

            if (value < minInclusive || value > maxInclusive)
                throw new ArgumentOutOfRangeException(paramName, value, $"Value must be in range [{minInclusive}..{maxInclusive}].");

            return value;
        }

        public static long InRange(long value, long minInclusive, long maxInclusive, string paramName)
        {
            if (minInclusive > maxInclusive)
                throw new ArgumentException("minInclusive cannot be greater than maxInclusive.", nameof(minInclusive));

            if (value < minInclusive || value > maxInclusive)
                throw new ArgumentOutOfRangeException(paramName, value, $"Value must be in range [{minInclusive}..{maxInclusive}].");

            return value;
        }

        public static TimeSpan InRange(TimeSpan value, TimeSpan minInclusive, TimeSpan maxInclusive, string paramName)
        {
            if (minInclusive > maxInclusive)
                throw new ArgumentException("minInclusive cannot be greater than maxInclusive.", nameof(minInclusive));

            if (value < minInclusive || value > maxInclusive)
                throw new ArgumentOutOfRangeException(paramName, value, $"Value must be in range [{minInclusive}..{maxInclusive}].");

            return value;
        }

        // --- Collections ---

        public static IReadOnlyCollection<T> NotNullOrEmpty<T>(IReadOnlyCollection<T>? value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);

            if (value.Count == 0)
                throw new ArgumentException("Collection cannot be empty.", paramName);

            return value;
        }

        // --- Enum / default guards ---

        public static TEnum EnumDefined<TEnum>(TEnum value, string paramName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
                throw new ArgumentOutOfRangeException(paramName, value, $"Value is not defined for enum '{typeof(TEnum).Name}'.");

            return value;
        }

        public static Guid NotDefault(Guid value, string paramName)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Value cannot be an empty Guid.", paramName);

            return value;
        }

        public static T NotDefault<T>(T value, string paramName) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
                throw new ArgumentException("Value cannot be the default value for this type.", paramName);

            return value;
        }

        /// <summary>
        /// Generic condition check. If paramName is provided, throws ArgumentException; otherwise InvalidOperationException.
        /// </summary>
        public static void Ensure(bool condition, string message, string? paramName = null)
        {
            if (!condition)
            {
                if (!string.IsNullOrWhiteSpace(paramName))
                    throw new ArgumentException(message, paramName);

                throw new InvalidOperationException(message);
            }
        }
    }
}
