using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Import.Curl;
using ApiTestingStudio.Import.OpenApi;
using ApiTestingStudio.Import.Postman;
using ApiTestingStudio.Import.Scalar;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using FluentAssertions;

namespace ApiTestingStudio.PluginHost.Tests;

/// <summary>Parsing behaviour of the four Sprint 07 importers.</summary>
public sealed class ImporterParseTests
{
    [Fact]
    public async Task Curl_parses_method_url_headers_and_body()
    {
        const string command =
            "curl -X POST https://api.example.com/users " +
            "-H \"Content-Type: application/json\" -H \"Accept: application/json\" " +
            "-d '{\"name\":\"ada\"}'";

        var importer = new CurlImporter();
        importer.CanImport(new ImportSource("curl", command)).Should().BeTrue();

        var result = await importer.ImportAsync(new ImportSource("curl", command));

        result.Services.Should().ContainSingle();
        result.Services[0].BaseUrl.Should().Be("https://api.example.com");

        var endpoint = result.Endpoints.Should().ContainSingle().Subject;
        endpoint.Method.Should().Be(HttpVerb.Post);
        endpoint.Path.Should().Be("/users");
        endpoint.DefaultHeaders.Should().Contain("Content-Type").And.Contain("Accept");
        endpoint.DefaultBody.Should().Be("{\"name\":\"ada\"}");
    }

    [Fact]
    public async Task Curl_defaults_to_get_without_method_or_body()
    {
        var importer = new CurlImporter();
        var result = await importer.ImportAsync(new ImportSource("curl", "curl https://api.example.com/ping"));

        result.Endpoints.Should().ContainSingle().Which.Method.Should().Be(HttpVerb.Get);
    }

    [Fact]
    public async Task OpenApi_parses_json_paths_and_operations()
    {
        const string json = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Petstore", "version": "1.0" },
          "servers": [ { "url": "https://api.pet.com" } ],
          "paths": {
            "/pets": {
              "get": { "summary": "List pets", "operationId": "listPets" },
              "post": { "summary": "Create pet", "operationId": "createPet" }
            }
          }
        }
        """;

        var importer = new OpenApiImporter();
        importer.CanImport(new ImportSource("openapi", json)).Should().BeTrue();

        var result = await importer.ImportAsync(new ImportSource("openapi", json));

        result.Services.Should().ContainSingle().Which.Name.Should().Be("Petstore");
        result.Services[0].BaseUrl.Should().Be("https://api.pet.com");
        result.Endpoints.Should().HaveCount(2);
        result.Endpoints.Should().Contain(e => e.Method == HttpVerb.Get && e.Path == "/pets");
        result.Endpoints.Should().Contain(e => e.Method == HttpVerb.Post && e.Path == "/pets");
    }

    [Fact]
    public async Task OpenApi_parses_yaml()
    {
        const string yaml = """
        openapi: 3.0.0
        info:
          title: YamlApi
          version: 1.0.0
        paths:
          /things:
            get:
              summary: list things
        """;

        var importer = new OpenApiImporter();
        var result = await importer.ImportAsync(new ImportSource("openapi", yaml));

        result.Services.Should().ContainSingle().Which.Name.Should().Be("YamlApi");
        result.Endpoints.Should().ContainSingle().Which.Path.Should().Be("/things");
    }

    [Fact]
    public async Task OpenApi_parses_swagger_2()
    {
        const string json = """
        {
          "swagger": "2.0",
          "info": { "title": "Legacy", "version": "1" },
          "host": "legacy.example.com",
          "basePath": "/",
          "schemes": [ "https" ],
          "paths": { "/legacy": { "get": { "summary": "legacy op" } } }
        }
        """;

        var importer = new OpenApiImporter();
        var result = await importer.ImportAsync(new ImportSource("openapi", json));

        result.Services.Should().ContainSingle().Which.Name.Should().Be("Legacy");
        result.Endpoints.Should().ContainSingle().Which.Path.Should().Be("/legacy");
    }

    [Fact]
    public async Task OpenApi_throws_on_garbage()
    {
        var importer = new OpenApiImporter();
        var act = () => importer.ImportAsync(new ImportSource("openapi", "this is not a spec"));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Scalar_reuses_openapi_mapping()
    {
        const string json = """
        { "openapi": "3.0.0", "info": { "title": "Scaled", "version": "1" },
          "paths": { "/x": { "get": {} } } }
        """;

        var importer = new ScalarImporter();
        importer.CanImport(new ImportSource("scalar", json)).Should().BeTrue();
        importer.CanImport(new ImportSource("", null, "https://localhost:5001/scalar")).Should().BeTrue();

        var result = await importer.ImportAsync(new ImportSource("scalar", json));
        result.Endpoints.Should().ContainSingle().Which.Path.Should().Be("/x");
    }

    [Fact]
    public async Task Postman_maps_collection_and_folders()
    {
        const string json = """
        {
          "info": {
            "name": "MyCol",
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
          },
          "item": [
            {
              "name": "Get Users",
              "request": {
                "method": "GET",
                "url": { "raw": "https://api.example.com/users" },
                "header": [ { "key": "Accept", "value": "application/json" } ]
              }
            },
            {
              "name": "Admin",
              "item": [
                {
                  "name": "Create",
                  "request": {
                    "method": "POST",
                    "url": "https://api.example.com/users",
                    "body": { "mode": "raw", "raw": "{}" }
                  }
                }
              ]
            }
          ]
        }
        """;

        var importer = new PostmanImporter();
        importer.CanImport(new ImportSource("postman", json)).Should().BeTrue();

        var result = await importer.ImportAsync(new ImportSource("postman", json));

        result.Services.Should().ContainSingle().Which.Name.Should().Be("MyCol");
        result.Endpoints.Should().HaveCount(2);
        result.Endpoints.Should().Contain(e => e.Method == HttpVerb.Get && e.Path == "/users");
        result.Endpoints.Should().Contain(e => e.Name == "Admin / Create" && e.Method == HttpVerb.Post);
    }
}
