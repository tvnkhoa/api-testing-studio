using System.Reflection;
using ApiTestingStudio.Assertion.Json;
using ApiTestingStudio.Assertion.Regex;
using ApiTestingStudio.Assertion.Schema;
using ApiTestingStudio.Export.ApiStudio;
using ApiTestingStudio.Import.Curl;
using ApiTestingStudio.Import.OpenApi;
using ApiTestingStudio.Import.Postman;
using ApiTestingStudio.Import.Scalar;
using ApiTestingStudio.Runner.Stress;

namespace ApiTestingStudio.Host.Composition;

/// <summary>
/// Supplies the set of plugin assemblies to the plugin host. In Phase 1 the host references the
/// plugin projects directly, so the assemblies are named via a representative module type from
/// each. When directory-based dynamic loading is introduced, only this catalog changes — the
/// plugin loader and all business code stay the same.
/// </summary>
internal static class PluginCatalog
{
    public static IReadOnlyList<Assembly> GetPluginAssemblies() =>
    [
        typeof(CurlImportPluginModule).Assembly,
        typeof(OpenApiImportPluginModule).Assembly,
        typeof(ScalarImportPluginModule).Assembly,
        typeof(PostmanImportPluginModule).Assembly,
        typeof(JsonAssertionPluginModule).Assembly,
        typeof(RegexAssertionPluginModule).Assembly,
        typeof(SchemaAssertionPluginModule).Assembly,
        typeof(StressRunnerPluginModule).Assembly,
        typeof(ApiStudioExportPluginModule).Assembly,
    ];
}
