using System.Collections.Concurrent;
using System.Globalization;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Serilog.Core;
using Serilog.Events;

namespace ApiTestingStudio.Infrastructure.Logging;

/// <summary>
/// A Serilog sink (Sprint 13) that persists application log events to the open workspace's database for
/// the Log Viewer. Because Serilog is configured before the DI container exists and before any workspace
/// is open, the sink buffers events in a bounded ring and only flushes once <see cref="Bind"/> has
/// supplied the store/session and a workspace is open. Events emitted while no workspace is open are held
/// (up to the cap, then the oldest are dropped) and flushed when one opens. Flushing is batched on a
/// timer off the caller's thread so logging never blocks. See <c>.claude/FEATURES/Logging.md</c>.
/// </summary>
public sealed class WorkspaceDbLogSink : ILogEventSink, IDisposable
{
    private const int MaxBufferedEvents = 5000;
    private const int FlushBatchSize = 500;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly ConcurrentQueue<PendingLog> _buffer = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);

    private ILogEventStore? _store;
    private IWorkspaceSession? _session;
    private Timer? _timer;
    private volatile bool _disposed;

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        if (_disposed)
        {
            return;
        }

        _buffer.Enqueue(new PendingLog(
            logEvent.Timestamp,
            logEvent.Level.ToString(),
            ExtractSource(logEvent),
            logEvent.RenderMessage(CultureInfo.InvariantCulture),
            logEvent.Exception?.ToString()));

        // Bound memory: drop the oldest events beyond the cap.
        while (_buffer.Count > MaxBufferedEvents && _buffer.TryDequeue(out _))
        {
        }
    }

    /// <summary>Supplies the persistence target + session and starts the periodic flush.</summary>
    public void Bind(ILogEventStore store, IWorkspaceSession session)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _timer = new Timer(_ => _ = FlushAsync(), null, FlushInterval, FlushInterval);
    }

    /// <summary>Drains buffered events to the store for the open workspace. Safe to call concurrently.</summary>
    public async Task FlushAsync()
    {
        if (_store is null || _session is null || _session.Current is not { } workspace)
        {
            return;
        }

        if (!_flushGate.Wait(0))
        {
            return;
        }

        try
        {
            while (!_buffer.IsEmpty)
            {
                var batch = new List<LogEventRecord>(FlushBatchSize);
                while (batch.Count < FlushBatchSize && _buffer.TryDequeue(out var pending))
                {
                    batch.Add(new LogEventRecord
                    {
                        WorkspaceId = workspace.Id,
                        TimestampUtc = pending.Timestamp,
                        Level = pending.Level,
                        Source = pending.Source,
                        Message = pending.Message,
                        Exception = pending.Exception,
                    });
                }

                if (batch.Count == 0)
                {
                    break;
                }

                await _store.AppendAsync(batch).ConfigureAwait(false);
            }
        }
        catch
        {
            // Best-effort: a persistence failure (e.g. the workspace closed mid-flush) must not throw
            // out of the logging pipeline. Dropped events are acceptable for application diagnostics.
        }
        finally
        {
            _flushGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer?.Dispose();
        _flushGate.Dispose();
    }

    private static string ExtractSource(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("SourceContext", out var value) && value is ScalarValue { Value: string source }
            ? source
            : string.Empty;

    private readonly record struct PendingLog(
        DateTimeOffset Timestamp,
        string Level,
        string Source,
        string Message,
        string? Exception);
}
