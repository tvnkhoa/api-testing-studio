using System.Globalization;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Testing;

/// <summary>
/// Builds an <see cref="HttpExecutionResult"/> from the <c>status</c>/<c>reason</c>/<c>body</c>
/// outputs a Request node publishes, so assertions on a workflow evaluate against the same context
/// shape as an endpoint request. Shared by the test-suite executor and the Assertion node handler.
/// </summary>
public static class SyntheticHttpResponse
{
    public static HttpExecutionResult FromNodeOutputs(IReadOnlyDictionary<string, string> outputs, TimeSpan total)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        outputs.TryGetValue("status", out var status);
        outputs.TryGetValue("reason", out var reason);
        outputs.TryGetValue("body", out var body);

        var statusCode = int.TryParse(status, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

        return new HttpExecutionResult
        {
            Response = new HttpResponseModel
            {
                StatusCode = statusCode,
                ReasonPhrase = reason ?? string.Empty,
                Body = body,
            },
            Timing = new RequestTiming { Total = total },
        };
    }
}
