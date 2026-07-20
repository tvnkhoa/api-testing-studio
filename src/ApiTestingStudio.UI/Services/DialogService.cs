using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Dialogs;
using ApiTestingStudio.UI.Views.Dialogs;

namespace ApiTestingStudio.UI.Services;

/// <summary>WPF implementation of <see cref="IDialogService"/> using modal editor windows.</summary>
public sealed class DialogService : IDialogService
{
    private readonly IImportOrchestrator _importOrchestrator;
    private readonly IFileDialogService _fileDialog;

    public DialogService(IImportOrchestrator importOrchestrator, IFileDialogService fileDialog)
    {
        _importOrchestrator = importOrchestrator ?? throw new System.ArgumentNullException(nameof(importOrchestrator));
        _fileDialog = fileDialog ?? throw new System.ArgumentNullException(nameof(fileDialog));
    }

    public ServiceDraft? PromptService(string title, ServiceDraft? existing = null)
    {
        var viewModel = new ServiceEditorViewModel(title, existing);
        var dialog = new ServiceEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public EndpointDraft? PromptEndpoint(string title, EndpointDraft? existing = null)
    {
        var viewModel = new EndpointEditorViewModel(title, existing);
        var dialog = new EndpointEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public string? PromptName(string title, string label, string? existing = null)
    {
        var viewModel = new NamePromptViewModel(title, label, existing);
        var dialog = new NamePromptDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.Result : null;
    }

    public ProfileDraft? PromptProfile(string title, ProfileDefinition? existing = null)
    {
        var viewModel = new ProfileEditorViewModel(title, existing);
        var dialog = new ProfileEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public (string Name, EnvironmentKind Kind)? PromptEnvironment(string title, EnvironmentDefinition? existing = null)
    {
        var viewModel = new EnvironmentEditorViewModel(title, existing);
        var dialog = new EnvironmentEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? (viewModel.Name.Trim(), viewModel.Kind) : null;
    }

    public VariableDraft? PromptVariable(string title, Variable? existing, IReadOnlyList<EnvironmentDefinition> environments)
    {
        var viewModel = new VariableEditorViewModel(title, existing, environments);
        var dialog = new VariableEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public TestCaseDraft? PromptTestCase(string title, IReadOnlyList<TestCaseTargetOption> targets, TestCaseDraft? existing = null)
    {
        var viewModel = new TestCaseEditorViewModel(title, targets, existing);
        var dialog = new TestCaseEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public AssertionDraft? PromptAssertion(string title, IReadOnlyList<string> kinds, AssertionDraft? existing = null)
    {
        var viewModel = new AssertionEditorViewModel(title, kinds, existing);
        var dialog = new AssertionEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public bool ShowImportWizard()
    {
        var viewModel = new ImportWizardViewModel(_importOrchestrator, _fileDialog);
        var dialog = new ImportWizardDialog { DataContext = viewModel, Owner = ActiveWindow() };
        dialog.ShowDialog();
        return viewModel.Committed;
    }

    private static Window? ActiveWindow() =>
        System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? System.Windows.Application.Current?.MainWindow;
}
