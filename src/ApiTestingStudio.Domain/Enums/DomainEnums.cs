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
