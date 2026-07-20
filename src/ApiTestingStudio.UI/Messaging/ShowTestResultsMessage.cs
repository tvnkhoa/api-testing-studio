using System.Collections.Generic;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.UI.Messaging;

/// <summary>One case's run result paired with its display name (results carry only ids).</summary>
public sealed record TestRunResultView(string CaseName, TestRunResult Result);

/// <summary>
/// Broadcast on the CommunityToolkit <c>IMessenger</c> when a test case or suite has finished
/// running. The shell subscribes to open/focus the Test Results document and show the outcomes.
/// </summary>
public sealed record ShowTestResultsMessage(string Title, IReadOnlyList<TestRunResultView> Results);
