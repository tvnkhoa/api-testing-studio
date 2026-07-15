using ApiTestingStudio.Application.Import;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class SourceFormatDetectorTests
{
    private readonly SourceFormatDetector _detector = new();

    [Fact]
    public void Detects_curl_from_content() =>
        _detector.Detect("curl https://api.example.com/x").Should().Be("curl");

    [Fact]
    public void Detects_postman_from_schema_url() =>
        _detector.Detect("{ \"info\": { \"schema\": \"https://schema.getpostman.com/x\" } }").Should().Be("postman");

    [Fact]
    public void Detects_openapi_from_json_content() =>
        _detector.Detect("{ \"openapi\": \"3.0.0\" }").Should().Be("openapi");

    [Fact]
    public void Detects_openapi_from_swagger_yaml() =>
        _detector.Detect("swagger: \"2.0\"").Should().Be("openapi");

    [Fact]
    public void Detects_scalar_from_url() =>
        _detector.Detect(content: null, uri: "https://localhost:5001/scalar").Should().Be("scalar");

    [Fact]
    public void Defaults_url_to_openapi() =>
        _detector.Detect(content: null, uri: "https://localhost:5001").Should().Be("openapi");

    [Fact]
    public void Detects_openapi_from_file_extension() =>
        _detector.Detect(content: null, fileName: "api.yaml").Should().Be("openapi");

    [Fact]
    public void Returns_null_when_nothing_matches() =>
        _detector.Detect("hello world").Should().BeNull();
}
