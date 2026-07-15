using System.Text;
using System.Text.Json;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Importing;

namespace ApiTestingStudio.Import.Curl;

/// <summary>
/// Parses a single <c>curl</c> command line into an <see cref="ImportResult"/>. Tolerant of quoting
/// (single/double), backslash and caret line continuations, and the common flag spellings.
/// </summary>
internal static class CurlCommandParser
{
    private static readonly JsonSerializerOptions HeaderJsonOptions = new(JsonSerializerDefaults.Web);

    public static ImportResult Parse(string command)
    {
        var tokens = Tokenize(command);

        string? method = null;
        string? url = null;
        var headers = new List<HttpHeader>();
        var bodyParts = new List<string>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token)
            {
                case "curl":
                    break;

                case "-X":
                case "--request":
                    if (TryNext(tokens, ref i, out var verb))
                    {
                        method = verb;
                    }

                    break;

                case "-H":
                case "--header":
                    if (TryNext(tokens, ref i, out var header))
                    {
                        AddHeader(headers, header);
                    }

                    break;

                case "-d":
                case "--data":
                case "--data-raw":
                case "--data-ascii":
                case "--data-binary":
                case "--data-urlencode":
                    if (TryNext(tokens, ref i, out var data))
                    {
                        bodyParts.Add(data);
                    }

                    break;

                case "--url":
                    if (TryNext(tokens, ref i, out var explicitUrl))
                    {
                        url = explicitUrl;
                    }

                    break;

                default:
                    // A bare (non-flag) token is the URL; flags we don't model are skipped, along
                    // with their value when they take one (best effort: only known value-flags above
                    // consume a following token, so an unknown "--flag value" leaves "value" to be
                    // treated as a URL only if it isn't itself a flag).
                    if (!token.StartsWith('-') && url is null && LooksLikeUrl(token))
                    {
                        url = token;
                    }

                    break;
            }
        }

        var body = bodyParts.Count == 0 ? null : string.Join("&", bodyParts);
        var verbEnum = ResolveVerb(method, body is not null);

        var (baseUrl, path, name) = SplitUrl(url);

        var service = new Service
        {
            Name = name,
            BaseUrl = baseUrl,
        };

        var endpoint = new Endpoint
        {
            ServiceId = service.Id,
            Name = $"{verbEnum.ToString().ToUpperInvariant()} {path}",
            Method = verbEnum,
            Path = path,
            DefaultHeaders = headers.Count == 0
                ? null
                : JsonSerializer.Serialize(headers, HeaderJsonOptions),
            DefaultBody = body,
        };

        return new ImportResult([service], [endpoint]);
    }

    private static void AddHeader(List<HttpHeader> headers, string raw)
    {
        var separator = raw.IndexOf(':');
        if (separator <= 0)
        {
            return;
        }

        var name = raw[..separator].Trim();
        var value = raw[(separator + 1)..].Trim();
        if (name.Length > 0)
        {
            headers.Add(new HttpHeader(name, value));
        }
    }

    private static HttpVerb ResolveVerb(string? method, bool hasBody)
    {
        if (!string.IsNullOrWhiteSpace(method) && Enum.TryParse<HttpVerb>(method.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        // curl defaults to POST when data is supplied without an explicit method, otherwise GET.
        return hasBody ? HttpVerb.Post : HttpVerb.Get;
    }

    private static (string? BaseUrl, string Path, string Name) SplitUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return (null, "/", "Imported Request");
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var baseUrl = $"{uri.Scheme}://{uri.Authority}";
            var path = uri.PathAndQuery.Length == 0 ? "/" : uri.PathAndQuery;
            return (baseUrl, path, uri.Host);
        }

        // Relative or malformed URL: keep the whole thing as the path.
        return (null, url, "Imported Request");
    }

    private static bool LooksLikeUrl(string token) =>
        token.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || token.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || token.Contains('/', StringComparison.Ordinal)
        || token.Contains('.', StringComparison.Ordinal);

    private static bool TryNext(List<string> tokens, ref int index, out string value)
    {
        if (index + 1 < tokens.Count)
        {
            value = tokens[++index];
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Splits a command line into tokens, honouring single/double quotes and dropping shell line
    /// continuations (trailing <c>\</c> or <c>^</c> followed by a newline).
    /// </summary>
    private static List<string> Tokenize(string command)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inToken = false;
        char? quote = null;

        for (var i = 0; i < command.Length; i++)
        {
            var c = command[i];

            if (quote is { } q)
            {
                if (c == q)
                {
                    quote = null;
                }
                else
                {
                    current.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '\'':
                case '"':
                    quote = c;
                    inToken = true;
                    break;

                case '\\':
                case '^':
                    // Line continuation: swallow the marker and the following newline(s).
                    if (i + 1 < command.Length && (command[i + 1] == '\r' || command[i + 1] == '\n'))
                    {
                        break;
                    }

                    current.Append(c);
                    inToken = true;
                    break;

                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    if (inToken)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        inToken = false;
                    }

                    break;

                default:
                    current.Append(c);
                    inToken = true;
                    break;
            }
        }

        if (inToken)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}
