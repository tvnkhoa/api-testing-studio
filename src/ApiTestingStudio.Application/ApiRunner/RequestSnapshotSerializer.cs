using System.Text.Json;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.ApiRunner;

/// <summary>
/// Serializes request/response models to and from the JSON snapshot text stored in
/// <see cref="RequestHistoryEntry"/>. Centralized so the write and replay paths stay in sync.
/// </summary>
internal static class RequestSnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(HttpRequestModel request) =>
        JsonSerializer.Serialize(request, Options);

    public static string Serialize(HttpResponseModel response) =>
        JsonSerializer.Serialize(response, Options);

    public static HttpRequestModel? DeserializeRequest(string snapshot) =>
        JsonSerializer.Deserialize<HttpRequestModel>(snapshot, Options);

    public static HttpResponseModel? DeserializeResponse(string snapshot) =>
        JsonSerializer.Deserialize<HttpResponseModel>(snapshot, Options);
}
