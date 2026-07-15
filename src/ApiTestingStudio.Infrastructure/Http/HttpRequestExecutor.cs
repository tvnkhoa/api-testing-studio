using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Http;

/// <summary>
/// <see cref="IRequestExecutor"/> backed by a single long-lived <see cref="SocketsHttpHandler"/>
/// (the MS-recommended pattern for a desktop app — avoids socket exhaustion, refreshes DNS via
/// <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>). Measures total and time-to-first-byte
/// directly; DNS and connect are captured per new connection via a <see cref="ConnectCallback"/>
/// and are null when an existing pooled connection is reused. Transport failures become typed
/// <see cref="Result"/> failures rather than exceptions.
/// </summary>
public sealed class HttpRequestExecutor : IRequestExecutor, IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);

    private readonly HttpClient _client;
    private readonly ILogger<HttpRequestExecutor> _logger;

    // Set before each send; the ConnectCallback (which inherits the send's execution context)
    // mutates the same holder instance so connect timings flow back to the caller.
    private readonly AsyncLocal<ConnectMetrics?> _connect = new();

    public HttpRequestExecutor(ILogger<HttpRequestExecutor> logger)
    {
        _logger = logger;

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = ConnectWithTimingAsync,
        };

        _client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = DefaultTimeout,
        };
    }

    public async Task<Result<HttpExecutionResult>> ExecuteAsync(HttpRequestModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = BuildMessage(request);
        var metrics = new ConnectMetrics();
        _connect.Value = metrics;

        var total = Stopwatch.StartNew();
        try
        {
            using var response = await _client
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            var timeToFirstByte = total.Elapsed;

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            total.Stop();

            var model = new HttpResponseModel
            {
                StatusCode = (int)response.StatusCode,
                ReasonPhrase = response.ReasonPhrase ?? string.Empty,
                Headers = ReadHeaders(response),
                Body = body,
                ContentLengthBytes = response.Content.Headers.ContentLength ?? Encoding.UTF8.GetByteCount(body),
            };

            var timing = new RequestTiming
            {
                Total = total.Elapsed,
                TimeToFirstByte = timeToFirstByte,
                Dns = metrics.Dns,
                Connect = metrics.Connect,
            };

            return Result.Success(new HttpExecutionResult { Response = model, Timing = timing });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.Cancelled);
        }
        catch (OperationCanceledException ex)
        {
            // The linked token wasn't cancelled, so HttpClient.Timeout elapsed.
            _logger.LogWarning(ex, "Request to {Url} timed out.", request.Url);
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.Timeout);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Request to {Url} failed.", request.Url);
            return Result.Failure<HttpExecutionResult>(RequestExecutionErrors.RequestFailed(ex.Message));
        }
        finally
        {
            _connect.Value = null;
        }
    }

    private static HttpRequestMessage BuildMessage(HttpRequestModel request)
    {
        var message = new HttpRequestMessage(ToHttpMethod(request.Method), BuildUrl(request));

        if (AllowsBody(request.Method) && request.Body is { } body)
        {
            message.Content = new StringContent(body, Encoding.UTF8, MediaType(request.BodyKind));
        }

        foreach (var header in request.Headers)
        {
            if (!header.Enabled || string.IsNullOrEmpty(header.Name))
            {
                continue;
            }

            // Request headers first; content headers (e.g. an explicit Content-Type) fall through.
            if (!message.Headers.TryAddWithoutValidation(header.Name, header.Value))
            {
                message.Content?.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        return message;
    }

    private async ValueTask<Stream> ConnectWithTimingAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var metrics = _connect.Value;

        var dns = Stopwatch.StartNew();
        var addresses = await System.Net.Dns
            .GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken)
            .ConfigureAwait(false);
        dns.Stop();

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            var connect = Stopwatch.StartNew();
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
            connect.Stop();

            if (metrics is not null)
            {
                metrics.Dns = dns.Elapsed;
                metrics.Connect = connect.Elapsed;
            }

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static List<HttpHeader> ReadHeaders(HttpResponseMessage response) =>
        response.Headers
            .Concat(response.Content.Headers)
            .Select(h => new HttpHeader(h.Key, string.Join(", ", h.Value)))
            .ToList();

    private static string BuildUrl(HttpRequestModel request)
    {
        var enabled = request.QueryParams
            .Where(p => p.Enabled && !string.IsNullOrEmpty(p.Name))
            .ToList();
        if (enabled.Count == 0)
        {
            return request.Url;
        }

        var builder = new UriBuilder(request.Url);
        var existing = builder.Query.TrimStart('?');
        var added = string.Join(
            '&',
            enabled.Select(p => $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.Value)}"));
        builder.Query = string.IsNullOrEmpty(existing) ? added : $"{existing}&{added}";
        return builder.Uri.ToString();
    }

    private static bool AllowsBody(HttpVerb verb) =>
        verb is HttpVerb.Post or HttpVerb.Put or HttpVerb.Patch or HttpVerb.Delete;

    private static string MediaType(BodyKind kind) => kind switch
    {
        BodyKind.Json => "application/json",
        BodyKind.Form => "application/x-www-form-urlencoded",
        _ => "text/plain",
    };

    private static HttpMethod ToHttpMethod(HttpVerb verb) => verb switch
    {
        HttpVerb.Get => HttpMethod.Get,
        HttpVerb.Post => HttpMethod.Post,
        HttpVerb.Put => HttpMethod.Put,
        HttpVerb.Patch => HttpMethod.Patch,
        HttpVerb.Delete => HttpMethod.Delete,
        HttpVerb.Head => HttpMethod.Head,
        HttpVerb.Options => HttpMethod.Options,
        _ => HttpMethod.Get,
    };

    public void Dispose() => _client.Dispose();

    private sealed class ConnectMetrics
    {
        public TimeSpan? Dns { get; set; }

        public TimeSpan? Connect { get; set; }
    }
}
