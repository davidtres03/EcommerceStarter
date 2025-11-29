using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using EcommerceStarter.Services;

namespace EcommerceStarter.Converters
{
    /// <summary>
    /// Factory for creating timezone-aware DateTime converters.
    /// </summary>
    public class TimezoneAwareDateTimeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(DateTime) || typeToConvert == typeof(DateTime?);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(DateTime))
            {
                return new TimezoneAwareDateTimeConverter();
            }
            else if (typeToConvert == typeof(DateTime?))
            {
                return new TimezoneAwareDateTimeNullableConverter();
            }
            return null;
        }
    }

    /// <summary>
    /// Global JSON converter that automatically converts UTC DateTime values to the configured timezone
    /// </summary>
    public class TimezoneAwareDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateTimeString = reader.GetString();
            if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
                return dateTime;
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var timezoneService = TimezoneServiceAccessor.TimezoneService;
            
            if (timezoneService == null)
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"));
                return;
            }

            DateTime localTime;
            
            if (value.Kind == DateTimeKind.Utc)
            {
                localTime = timezoneService.ConvertUtcToLocalTime(value);
            }
            else if (value.Kind == DateTimeKind.Unspecified)
            {
                var utcTime = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                localTime = timezoneService.ConvertUtcToLocalTime(utcTime);
            }
            else
            {
                localTime = value;
            }

            writer.WriteStringValue(localTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"));
        }
    }

    /// <summary>
    /// Nullable DateTime converter
    /// </summary>
    public class TimezoneAwareDateTimeNullableConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var dateTimeString = reader.GetString();
            if (string.IsNullOrEmpty(dateTimeString))
            {
                return null;
            }

            if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
                return dateTime;
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            var timezoneService = TimezoneServiceAccessor.TimezoneService;
            
            if (timezoneService == null)
            {
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"));
                return;
            }

            DateTime localTime;
            var dateTime = value.Value;

            if (dateTime.Kind == DateTimeKind.Utc)
            {
                localTime = timezoneService.ConvertUtcToLocalTime(dateTime);
            }
            else if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                var utcTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                localTime = timezoneService.ConvertUtcToLocalTime(utcTime);
            }
            else
            {
                localTime = dateTime;
            }

            writer.WriteStringValue(localTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"));
        }
    }

    /// <summary>
    /// Static accessor for ITimezoneService set by middleware on each request
    /// </summary>
    public static class TimezoneServiceAccessor
    {
        private static readonly AsyncLocal<ITimezoneService?> _timezoneService = new AsyncLocal<ITimezoneService?>();

        public static ITimezoneService? TimezoneService
        {
            get => _timezoneService.Value;
            set => _timezoneService.Value = value;
        }
    }
}
