namespace ApiTestingStudio.Application.Import;

/// <inheritdoc />
public sealed class SourceFormatDetector : ISourceFormatDetector
{
    public string? Detect(string? content, string? fileName = null, string? uri = null)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            var trimmed = content.TrimStart();

            if (trimmed.StartsWith("curl", StringComparison.OrdinalIgnoreCase))
            {
                return "curl";
            }

            if (content.Contains("schema.getpostman.com", StringComparison.OrdinalIgnoreCase))
            {
                return "postman";
            }

            if (content.Contains("\"openapi\"", StringComparison.OrdinalIgnoreCase)
                || content.Contains("\"swagger\"", StringComparison.OrdinalIgnoreCase)
                || content.Contains("openapi:", StringComparison.OrdinalIgnoreCase)
                || content.Contains("swagger:", StringComparison.OrdinalIgnoreCase))
            {
                return "openapi";
            }
        }

        if (!string.IsNullOrWhiteSpace(uri))
        {
            if (uri.Contains("/scalar", StringComparison.OrdinalIgnoreCase))
            {
                return "scalar";
            }

            // Any other URL is treated as an OpenAPI/Swagger definition endpoint.
            return "openapi";
        }

        // Fall back to the file extension when content sniffing was inconclusive.
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                return "openapi";
            }
        }

        return null;
    }
}
