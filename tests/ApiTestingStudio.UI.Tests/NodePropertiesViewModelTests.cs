using ApiTestingStudio.Application.Common;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Workflow;
using FluentAssertions;
using Xunit;

namespace ApiTestingStudio.UI.Tests;

public sealed class NodePropertiesViewModelTests
{
    private static NodeViewModel AssertionNode(params AssertionSpec[] specs) =>
        new(Guid.NewGuid(), WorkflowNodeKind.Assertion, "Assert")
        {
            Config = new AssertionNodeConfig { SourceNode = "Login", Assertions = specs },
        };

    [Fact]
    public void AddAssertion_appends_spec_and_is_undoable()
    {
        var undo = new UndoRedoService();
        var dialog = new FakeDialogService
        {
            AssertionResult = new AssertionDraft("json", AssertionSource.Body, "$.token", null, "exists", string.Empty, true),
        };
        var vm = new NodePropertiesViewModel(undo, dialog, ["json"]);
        var node = AssertionNode();

        vm.Load(node);
        vm.AddAssertionCommand.Execute(null);

        vm.Assertions.Should().ContainSingle();
        ((AssertionNodeConfig)node.Config!).Assertions.Should().ContainSingle()
            .Which.Target.Should().Be("$.token");

        undo.Undo();
        ((AssertionNodeConfig)node.Config!).Assertions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveAssertion_removes_selected_row()
    {
        var undo = new UndoRedoService();
        var vm = new NodePropertiesViewModel(undo, new FakeDialogService(), ["json"]);
        var node = AssertionNode(new AssertionSpec { Kind = "json", Source = AssertionSource.Body, Expected = "x" });

        vm.Load(node);
        vm.Assertions.Should().ContainSingle();

        vm.RemoveAssertionCommand.Execute(vm.Assertions[0]);

        vm.Assertions.Should().BeEmpty();
        ((AssertionNodeConfig)node.Config!).Assertions.Should().BeEmpty();
    }

    [Fact]
    public void EditingSourceNode_preserves_existing_assertions()
    {
        var undo = new UndoRedoService();
        var vm = new NodePropertiesViewModel(undo, new FakeDialogService(), ["json"]);
        var node = AssertionNode(new AssertionSpec { Kind = "json", Source = AssertionSource.Body, Expected = "x" });

        vm.Load(node);
        vm.AssertionSourceNode = "Checkout";

        var config = (AssertionNodeConfig)node.Config!;
        config.SourceNode.Should().Be("Checkout");
        config.Assertions.Should().ContainSingle();
    }
}
