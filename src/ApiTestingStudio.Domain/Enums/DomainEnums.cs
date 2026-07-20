namespace ApiTestingStudio.Domain.Enums;

/// <summary>HTTP verbs supported by the request runner.</summary>
public enum HttpVerb
{
    Get,
    Post,
    Put,
    Patch,
    Delete,
    Head,
    Options,
}

/// <summary>How a request body is authored and sent by the runner.</summary>
public enum BodyKind
{
    /// <summary>Free-form text sent verbatim.</summary>
    Raw,

    /// <summary>JSON body (Content-Type <c>application/json</c>).</summary>
    Json,

    /// <summary>URL-encoded form fields (Content-Type <c>application/x-www-form-urlencoded</c>).</summary>
    Form,
}

/// <summary>Resolution scope for a <c>{{variable}}</c>, ordered from broadest to narrowest.</summary>
public enum VariableScope
{
    Global,
    Workspace,
    Environment,
    Workflow,
    Local,
    WorkflowOutput,
}

/// <summary>Well-known deployment environments.</summary>
public enum EnvironmentKind
{
    Development,
    QA,
    Staging,
    Production,
}

/// <summary>Identity archetype a profile simulates when a workflow runs "Run As".</summary>
public enum ProfileKind
{
    Admin,
    Staff,
    Guest,
    Developer,
    Custom,
}

/// <summary>
/// How a profile's credentials are turned into an outgoing authorization on a request.
/// Distinct from <see cref="ProfileKind"/> (which is a role archetype).
/// </summary>
public enum AuthScheme
{
    /// <summary>No authorization is applied.</summary>
    None,

    /// <summary><c>Authorization: Bearer {AccessToken}</c>.</summary>
    Bearer,

    /// <summary><c>Authorization: Basic base64(Username:Password)</c>.</summary>
    Basic,

    /// <summary>A custom header (<c>ApiKeyHeaderName</c>) carrying the API key.</summary>
    ApiKey,

    /// <summary>Caller supplies headers manually; the applicator does not inject anything.</summary>
    Custom,
}

/// <summary>Lifecycle status of a workflow / test run or one of its steps.</summary>
public enum RunStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Cancelled,
}

/// <summary>The kind of node in a visual workflow graph.</summary>
public enum WorkflowNodeKind
{
    Api,
    Condition,
    Loop,
    Delay,
    Parallel,
    Switch,
    Variable,
    Assertion,
}

/// <summary>Outcome of evaluating a single assertion.</summary>
public enum AssertionOutcome
{
    Passed,
    Failed,
    Skipped,
}

/// <summary>
/// Which part of an HTTP response an assertion evaluates against. The assertion runner maps the
/// selected source to the <c>Actual</c> string handed to the assertion plugin, giving a single,
/// consistent assertion context whether the assertion runs in a test case or a workflow node.
/// </summary>
public enum AssertionSource
{
    /// <summary>The numeric status code (e.g. <c>200</c>).</summary>
    StatusCode,

    /// <summary>The HTTP reason phrase (e.g. <c>OK</c>).</summary>
    ReasonPhrase,

    /// <summary>A named response header; the header name is carried in the assertion's <c>Target</c>.</summary>
    Header,

    /// <summary>The decoded response body text.</summary>
    Body,

    /// <summary>Total request duration in whole milliseconds.</summary>
    TimingTotalMs,
}

/// <summary>
/// How the workflow engine reacts when a node fails. <see cref="StopOnError"/> aborts the run;
/// <see cref="ContinueOnError"/> records the failure and proceeds to the next node.
/// </summary>
public enum NodeFailurePolicy
{
    StopOnError,
    ContinueOnError,
}

/// <summary>
/// How a stress run issues its workload (Sprint 12). Lives in Domain because it is both a persisted
/// attribute of a <c>StressRun</c> and part of the <c>IStressRunner</c> plugin contract.
/// </summary>
public enum StressMode
{
    /// <summary>A single pass of the workload, one request at a time.</summary>
    Sequential,

    /// <summary>A fixed number of passes back-to-back, one request at a time.</summary>
    Loop,

    /// <summary>A fixed number of virtual users issuing requests concurrently.</summary>
    Concurrent,
}

/// <summary>What a stress run drives: an ad-hoc request, a saved endpoint, or a workflow.</summary>
public enum StressTargetKind
{
    /// <summary>An ad-hoc HTTP request assembled by the caller (no saved target id).</summary>
    Request,

    /// <summary>A saved endpoint, identified by its endpoint id.</summary>
    Endpoint,

    /// <summary>A saved workflow, identified by its workflow id.</summary>
    Workflow,
}
