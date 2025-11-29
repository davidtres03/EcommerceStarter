using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Models.VisitorTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Queued event for batch processing
    /// </summary>
    public abstract class QueuedEvent
    {
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Queued page view event
    /// </summary>
    public class QueuedPageView : QueuedEvent
    {
        public int SessionId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? PageTitle { get; set; }
        public string? Referrer { get; set; }
        public DateTime ViewedAt { get; set; }
    }

    /// <summary>
    /// Queued visitor event
    /// </summary>
    public class QueuedVisitorEvent : QueuedEvent
    {
        public int SessionId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Label { get; set; }
        public decimal? Value { get; set; }
        public string? Metadata { get; set; }
        public DateTime EventTime { get; set; }
    }

    /// <summary>
    /// Queued security audit event
    /// </summary>
    public class QueuedSecurityAudit : QueuedEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? Details { get; set; }
        public string? Endpoint { get; set; }
        public string? UserAgent { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Queued customer audit event
    /// </summary>
    public class QueuedCustomerAudit : QueuedEvent
    {
        public string CustomerId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public AuditEventCategory Category { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Interface for queued event service
    /// </summary>
    public interface IQueuedEventService
    {
        void QueueGeolocation(int sessionId, string ipAddress);
        void QueuePageView(int sessionId, string url, string? pageTitle, string? referrer);
        void QueueVisitorEvent(int sessionId, string category, string action, string? label = null, decimal? value = null, string? metadata = null);
        void QueueSecurityAudit(string eventType, string severity, string ipAddress, string? userId = null, string? userEmail = null, string? details = null, string? endpoint = null, string? userAgent = null, bool isBlocked = false);
        void QueueCustomerAudit(string customerId, AuditEventCategory category, string eventType, string description, string? details = null, string? ipAddress = null, string? userAgent = null, bool success = true, string? errorMessage = null);
        
        Task<int> ProcessQueuedEventsAsync(CancellationToken cancellationToken = default);
        int GetQueueSize();
        QueueMetrics GetMetrics();
    }

    /// <summary>
    /// Service for queuing analytics and audit events for batch processing
    /// Reduces database load by batching writes instead of individual inserts
    /// </summary>
    public class QueuedEventService : IQueuedEventService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedEventService> _logger;
        
        private readonly ConcurrentQueue<QueuedPageView> _pageViewQueue = new();
        private readonly ConcurrentQueue<QueuedVisitorEvent> _visitorEventQueue = new();
        private readonly ConcurrentQueue<QueuedSecurityAudit> _securityAuditQueue = new();
        private readonly ConcurrentQueue<QueuedCustomerAudit> _customerAuditQueue = new();

        private const int MAX_QUEUE_SIZE = 10000; // Prevent memory overflow
        private const int MIN_BATCH_SIZE = 100; // Minimum batch size
        private const int MAX_BATCH_SIZE = 1000; // Maximum batch size
        private const int WARNING_THRESHOLD = 8000; // 80% capacity warning
        private const int MAX_RETRIES = 3;

        // Performance metrics
        private int _totalEventsProcessed = 0;
        private int _totalFailures = 0;
        private int _peakQueueSize = 0;
        private DateTime _lastWarningTime = DateTime.MinValue;
        private readonly object _metricsLock = new();

        public QueuedEventService(
            IServiceScopeFactory scopeFactory,
            ILogger<QueuedEventService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void QueueGeolocation(int sessionId, string ipAddress)
        {
            // Placeholder: implement background geolocation processing if needed
            _logger.LogDebug("[Queue] Geolocation for session {SessionId} ({Ip})", sessionId, ipAddress);
        }

        /// <summary>
        /// Queue a page view event (non-blocking)
        /// </summary>
        public void QueuePageView(int sessionId, string url, string? pageTitle, string? referrer)
        {
            var queueSize = _pageViewQueue.Count;
            
            if (queueSize >= MAX_QUEUE_SIZE)
            {
                _logger.LogWarning("Page view queue is full ({Count}). Dropping event.", MAX_QUEUE_SIZE);
                lock (_metricsLock) { _totalFailures++; }
                return;
            }

            CheckQueueCapacity(queueSize, "PageView");
            
            _pageViewQueue.Enqueue(new QueuedPageView
            {
                SessionId = sessionId,
                Url = url,
                PageTitle = pageTitle,
                Referrer = referrer,
                ViewedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Queue a visitor event (non-blocking)
        /// </summary>
        public void QueueVisitorEvent(int sessionId, string category, string action, string? label = null, decimal? value = null, string? metadata = null)
        {
            var queueSize = _visitorEventQueue.Count;
            
            if (queueSize >= MAX_QUEUE_SIZE)
            {
                _logger.LogWarning("Visitor event queue is full ({Count}). Dropping event.", MAX_QUEUE_SIZE);
                lock (_metricsLock) { _totalFailures++; }
                return;
            }

            CheckQueueCapacity(queueSize, "VisitorEvent");
            
            _visitorEventQueue.Enqueue(new QueuedVisitorEvent
            {
                SessionId = sessionId,
                Category = category,
                Action = action,
                Label = label,
                Value = value,
                Metadata = metadata,
                EventTime = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Queue a security audit event (non-blocking for non-critical events)
        /// </summary>
        public void QueueSecurityAudit(string eventType, string severity, string ipAddress, 
            string? userId = null, string? userEmail = null, string? details = null, 
            string? endpoint = null, string? userAgent = null, bool isBlocked = false)
        {
            var queueSize = _securityAuditQueue.Count;
            
            if (queueSize >= MAX_QUEUE_SIZE)
            {
                _logger.LogWarning("Security audit queue is full ({Count}). Dropping event.", MAX_QUEUE_SIZE);
                lock (_metricsLock) { _totalFailures++; }
                return;
            }

            CheckQueueCapacity(queueSize, "SecurityAudit");
            
            _securityAuditQueue.Enqueue(new QueuedSecurityAudit
            {
                EventType = eventType,
                Severity = severity,
                IpAddress = ipAddress,
                UserId = userId,
                UserEmail = userEmail,
                Details = details,
                Endpoint = endpoint,
                UserAgent = userAgent,
                IsBlocked = isBlocked,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Queue a customer audit event (non-blocking)
        /// </summary>
        public void QueueCustomerAudit(string customerId, AuditEventCategory category, string eventType, string description, 
            string? details = null, string? ipAddress = null, string? userAgent = null, bool success = true, string? errorMessage = null)
        {
            var queueSize = _customerAuditQueue.Count;
            
            if (queueSize >= MAX_QUEUE_SIZE)
            {
                _logger.LogWarning("Customer audit queue is full ({Count}). Dropping event.", MAX_QUEUE_SIZE);
                lock (_metricsLock) { _totalFailures++; }
                return;
            }

            CheckQueueCapacity(queueSize, "CustomerAudit");
            
            _customerAuditQueue.Enqueue(new QueuedCustomerAudit
            {
                CustomerId = customerId,
                Category = category,
                EventType = eventType,
                Description = description,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Process all queued events in batches (called by background service)
        /// </summary>
        public async Task<int> ProcessQueuedEventsAsync(CancellationToken cancellationToken = default)
        {
            int totalProcessed = 0;
            var startTime = DateTime.UtcNow;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Calculate dynamic batch size based on total queue depth
                var queueSize = GetQueueSize();
                var batchSize = GetDynamicBatchSize(queueSize);

                // Process page views with retry
                totalProcessed += await ProcessWithRetryAsync(
                    () => ProcessPageViewsAsync(context, batchSize, cancellationToken),
                    "PageViews");

                // Process visitor events with retry
                totalProcessed += await ProcessWithRetryAsync(
                    () => ProcessVisitorEventsAsync(context, batchSize, cancellationToken),
                    "VisitorEvents");

                // Process security audits with retry
                totalProcessed += await ProcessWithRetryAsync(
                    () => ProcessSecurityAuditsAsync(context, batchSize, cancellationToken),
                    "SecurityAudits");

                // Process customer audits with retry
                totalProcessed += await ProcessWithRetryAsync(
                    () => ProcessCustomerAuditsAsync(context, batchSize, cancellationToken),
                    "CustomerAudits");

                if (totalProcessed > 0)
                {
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("Processed {Count} queued events in {Duration}ms (batch size: {BatchSize})", 
                        totalProcessed, (int)duration, batchSize);
                    
                    lock (_metricsLock)
                    {
                        _totalEventsProcessed += totalProcessed;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued events");
                lock (_metricsLock)
                {
                    _totalFailures++;
                }
            }

            return totalProcessed;
        }

        /// <summary>
        /// Process with retry logic (exponential backoff)
        /// </summary>
        private async Task<int> ProcessWithRetryAsync(Func<Task<int>> processFunc, string queueName)
        {
            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    return await processFunc();
                }
                catch (Exception ex)
                {
                    if (attempt == MAX_RETRIES)
                    {
                        _logger.LogError(ex, "Failed to process {QueueName} after {Attempts} attempts", queueName, attempt);
                        lock (_metricsLock)
                        {
                            _totalFailures++;
                        }
                        throw;
                    }

                    var delayMs = (int)Math.Pow(2, attempt) * 100; // 200ms, 400ms, 800ms
                    _logger.LogWarning("Error processing {QueueName}, attempt {Attempt}/{Max}. Retrying in {Delay}ms", 
                        queueName, attempt, MAX_RETRIES, delayMs);
                    await Task.Delay(delayMs);
                }
            }

            return 0;
        }

        #region Private Processing Methods

        private async Task<int> ProcessPageViewsAsync(ApplicationDbContext context, int batchSize, CancellationToken cancellationToken)
        {
            var batch = new List<PageView>();
            
            while (batch.Count < batchSize && _pageViewQueue.TryDequeue(out var queuedEvent))
            {
                batch.Add(new PageView
                {
                    SessionId = queuedEvent.SessionId,
                    Url = queuedEvent.Url,
                    PageTitle = queuedEvent.PageTitle,
                    Referrer = queuedEvent.Referrer,
                    Timestamp = queuedEvent.ViewedAt
                });
            }

            if (batch.Count > 0)
            {
                context.PageViews.AddRange(batch);
                await context.SaveChangesAsync(cancellationToken);
                return batch.Count;
            }

            return 0;
        }

        private async Task<int> ProcessVisitorEventsAsync(ApplicationDbContext context, int batchSize, CancellationToken cancellationToken)
        {
            var batch = new List<VisitorEvent>();
            
            while (batch.Count < batchSize && _visitorEventQueue.TryDequeue(out var queuedEvent))
            {
                batch.Add(new VisitorEvent
                {
                    SessionId = queuedEvent.SessionId,
                    Category = queuedEvent.Category,
                    Action = queuedEvent.Action,
                    Label = queuedEvent.Label,
                    Value = queuedEvent.Value,
                    Metadata = queuedEvent.Metadata,
                    Timestamp = queuedEvent.EventTime
                });
            }

            if (batch.Count > 0)
            {
                context.VisitorEvents.AddRange(batch);
                await context.SaveChangesAsync(cancellationToken);
                return batch.Count;
            }

            return 0;
        }

        private async Task<int> ProcessSecurityAuditsAsync(ApplicationDbContext context, int batchSize, CancellationToken cancellationToken)
        {
            var batch = new List<SecurityAuditLog>();
            
            while (batch.Count < batchSize && _securityAuditQueue.TryDequeue(out var queuedEvent))
            {
                batch.Add(new SecurityAuditLog
                {
                    EventType = queuedEvent.EventType,
                    Severity = queuedEvent.Severity,
                    IpAddress = queuedEvent.IpAddress,
                    UserId = queuedEvent.UserId,
                    UserEmail = queuedEvent.UserEmail,
                    Details = queuedEvent.Details,
                    Endpoint = queuedEvent.Endpoint,
                    UserAgent = queuedEvent.UserAgent,
                    IsBlocked = queuedEvent.IsBlocked,
                    Timestamp = queuedEvent.Timestamp
                });
            }

            if (batch.Count > 0)
            {
                context.SecurityAuditLogs.AddRange(batch);
                await context.SaveChangesAsync(cancellationToken);
                return batch.Count;
            }

            return 0;
        }

        private async Task<int> ProcessCustomerAuditsAsync(ApplicationDbContext context, int batchSize, CancellationToken cancellationToken)
        {
            var batch = new List<CustomerAuditLog>();
            
            while (batch.Count < batchSize && _customerAuditQueue.TryDequeue(out var queuedEvent))
            {
                batch.Add(new CustomerAuditLog
                {
                    CustomerId = queuedEvent.CustomerId,
                    EventType = queuedEvent.EventType,
                    Category = queuedEvent.Category,
                    Description = queuedEvent.Description,
                    Details = queuedEvent.Details,
                    IpAddress = queuedEvent.IpAddress,
                    UserAgent = queuedEvent.UserAgent,
                    Success = queuedEvent.Success,
                    ErrorMessage = queuedEvent.ErrorMessage,
                    CreatedAt = queuedEvent.CreatedAt
                });
            }

            if (batch.Count > 0)
            {
                context.CustomerAuditLogs.AddRange(batch);
                await context.SaveChangesAsync(cancellationToken);
                return batch.Count;
            }

            return 0;
        }

        /// <summary>
        /// Check queue capacity and log warning if approaching limit
        /// </summary>
        private void CheckQueueCapacity(int queueSize, string queueName)
        {
            lock (_metricsLock)
            {
                if (queueSize > _peakQueueSize)
                {
                    _peakQueueSize = queueSize;
                }
            }

            if (queueSize >= WARNING_THRESHOLD)
            {
                // Only warn once per minute to avoid log spam
                var now = DateTime.UtcNow;
                if ((now - _lastWarningTime).TotalMinutes >= 1)
                {
                    _logger.LogWarning("{QueueName} queue approaching capacity: {Size}/{Max} ({Percent}%)", 
                        queueName, queueSize, MAX_QUEUE_SIZE, (queueSize * 100 / MAX_QUEUE_SIZE));
                    _lastWarningTime = now;
                }
            }
        }

        /// <summary>
        /// Calculate dynamic batch size based on queue depth
        /// </summary>
        private int GetDynamicBatchSize(int queueSize)
        {
            // Scale batch size based on queue depth (100-1000)
            if (queueSize < 500) return MIN_BATCH_SIZE;
            if (queueSize < 2000) return 250;
            if (queueSize < 5000) return 500;
            return MAX_BATCH_SIZE;
        }

        /// <summary>
        /// Get current queue size for all queues
        /// </summary>
        public int GetQueueSize()
        {
            return _pageViewQueue.Count +
                   _visitorEventQueue.Count +
                   _securityAuditQueue.Count +
                   _customerAuditQueue.Count;
        }

        /// <summary>
        /// Get performance metrics
        /// </summary>
        public QueueMetrics GetMetrics()
        {
            lock (_metricsLock)
            {
                return new QueueMetrics
                {
                    TotalEventsProcessed = _totalEventsProcessed,
                    TotalFailures = _totalFailures,
                    PeakQueueSize = _peakQueueSize,
                    CurrentQueueSize = GetQueueSize(),
                    PageViewQueueSize = _pageViewQueue.Count,
                    VisitorEventQueueSize = _visitorEventQueue.Count,
                    SecurityAuditQueueSize = _securityAuditQueue.Count,
                    CustomerAuditQueueSize = _customerAuditQueue.Count
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// Queue performance metrics
    /// </summary>
    public class QueueMetrics
    {
        public int TotalEventsProcessed { get; set; }
        public int TotalFailures { get; set; }
        public int PeakQueueSize { get; set; }
        public int CurrentQueueSize { get; set; }
        public int PageViewQueueSize { get; set; }
        public int VisitorEventQueueSize { get; set; }
        public int SecurityAuditQueueSize { get; set; }
        public int CustomerAuditQueueSize { get; set; }
    }
}
