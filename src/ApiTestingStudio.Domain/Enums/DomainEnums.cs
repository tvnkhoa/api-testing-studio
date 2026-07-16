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
/// How the workflow engine reacts when a node fails. <see cref="StopOnError"/> aborts the run;
/// <see cref="ContinueOnError"/> records the failure and proceeds to the next node.
/// </summary>
public enum NodeFailurePolicy
{
    StopOnError,
    ContinueOnError,
}
