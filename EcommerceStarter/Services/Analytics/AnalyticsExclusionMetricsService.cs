using System.Collections.Concurrent;

namespace EcommerceStarter.Services.Analytics
{
    public interface IAnalyticsExclusionMetricsService
    {
        void RecordWhitelistExclusion();
        void RecordPrivateOrLocalExclusion();
        void RecordAdminPageExclusion();
        (int whitelist, int privateOrLocal, int adminPages) GetCountsSince(TimeSpan window);
    }

    /// <summary>
    /// In-memory rolling counters for analytics exclusion events.
    /// Keeps timestamps and computes counts within a sliding window (e.g., last 24h).
    /// </summary>
    public class AnalyticsExclusionMetricsService : IAnalyticsExclusionMetricsService
    {
        private readonly ConcurrentQueue<DateTime> _whitelistEvents = new();
        private readonly ConcurrentQueue<DateTime> _privateEvents = new();
        private readonly ConcurrentQueue<DateTime> _adminPageEvents = new();

        public void RecordWhitelistExclusion()
        {
            _whitelistEvents.Enqueue(DateTime.UtcNow);
            PruneOld(_whitelistEvents, TimeSpan.FromDays(2));
        }

        public void RecordPrivateOrLocalExclusion()
        {
            _privateEvents.Enqueue(DateTime.UtcNow);
            PruneOld(_privateEvents, TimeSpan.FromDays(2));
        }

        public void RecordAdminPageExclusion()
        {
            _adminPageEvents.Enqueue(DateTime.UtcNow);
            PruneOld(_adminPageEvents, TimeSpan.FromDays(2));
        }

        public (int whitelist, int privateOrLocal, int adminPages) GetCountsSince(TimeSpan window)
        {
            var since = DateTime.UtcNow - window;
            var wl = CountSince(_whitelistEvents, since);
            var pr = CountSince(_privateEvents, since);
            var admin = CountSince(_adminPageEvents, since);
            return (wl, pr, admin);
        }

        private static int CountSince(ConcurrentQueue<DateTime> queue, DateTime since)
        {
            PruneOld(queue, TimeSpan.FromDays(2));
            return queue.Count(ts => ts >= since);
        }

        private static void PruneOld(ConcurrentQueue<DateTime> queue, TimeSpan maxAge)
        {
            var cutoff = DateTime.UtcNow - maxAge;
            while (queue.TryPeek(out var ts) && ts < cutoff)
            {
                queue.TryDequeue(out _);
            }
        }
    }
}
