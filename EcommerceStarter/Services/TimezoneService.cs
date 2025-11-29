using System;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    public interface ITimezoneService
    {
        DateTime ConvertUtcToLocalTime(DateTime utcDateTime);
        DateTime? ConvertUtcToLocalTime(DateTime? utcDateTime);
        string GetConfiguredTimeZoneId();
        TimeZoneInfo GetConfiguredTimeZone();
    }

    public class TimezoneService : ITimezoneService
    {
        private readonly ApplicationDbContext _context;
        private TimeZoneInfo? _cachedTimeZone;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public TimezoneService(ApplicationDbContext context)
        {
            _context = context;
        }

        public TimeZoneInfo GetConfiguredTimeZone()
        {
            // Check cache first
            if (_cachedTimeZone != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedTimeZone;
            }

            try
            {
                // Get timezone from database
                var siteSettings = _context.SiteSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefault();
                
                if (siteSettings != null && !string.IsNullOrEmpty(siteSettings.TimeZoneId))
                {
                    try
                    {
                        _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(siteSettings.TimeZoneId);
                        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                        return _cachedTimeZone;
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        // Fall through to system timezone
                    }
                }
            }
            catch
            {
                // Fall through to system timezone
            }

            // Fallback to system local timezone
            _cachedTimeZone = TimeZoneInfo.Local;
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return _cachedTimeZone;
        }

        public string GetConfiguredTimeZoneId()
        {
            return GetConfiguredTimeZone().Id;
        }

        public DateTime ConvertUtcToLocalTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            var timeZone = GetConfiguredTimeZone();
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        }

        public DateTime? ConvertUtcToLocalTime(DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue)
            {
                return null;
            }
            return ConvertUtcToLocalTime(utcDateTime.Value);
        }
    }
}
