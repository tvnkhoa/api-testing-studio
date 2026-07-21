using System.Collections.ObjectModel;
using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Import;
using ApiTestingStudio.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>The step the wizard is currently on.</summary>
public enum ImportWizardStep
{
    Source,
    Preview,
    Result,
}

/// <summary>
/// Backs the modal Import wizard: choose a source (paste / file / URL) → preview the parsed
/// services/endpoints → commit. All parsing/merging is delegated to <see cref="IImportOrchestrator"/>;
/// this view model only orchestrates step flow and surfaces state to the view.
/// </summary>
public sealed partial class ImportWizardViewModel : ObservableObject
{
    private const string ImportFileFilter =
        "API definitions (*.json;*.yaml;*.yml)|*.json;*.yaml;*.yml|All files (*.*)|*.*";

    private readonly IImportOrchestrator _orchestrator;
    private readonly IFileDialogService _fileDialog;
    private readonly IFileContentReader _fileReader;

    private ImportPreview? _preview;

    public ImportWizardViewModel(IImportOrchestrator orchestrator, IFileDialogService fileDialog, IFileContentReader fileReader)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _fileDialog = fileDialog ?? throw new ArgumentNullException(nameof(fileDialog));
        _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
    }

    /// <summary>True once an import has been committed to the catalog (drives the caller's refresh).</summary>
    public bool Committed { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSourceStep))]
    [NotifyPropertyChangedFor(nameof(IsPreviewStep))]
    [NotifyPropertyChangedFor(nameof(IsResultStep))]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(ShowNavigation))]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private ImportWizardStep _step = ImportWizardStep.Source;

    // Source step inputs. The three modes are mutually exclusive radio buttons.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    private bool _isPasteMode = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    private bool _isFileMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    private bool _isUrlMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    private string _pastedText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    private string _filePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    private string _url = string.Empty;

    // Preview step.
    public ObservableCollection<ParsedService> PreviewServices { get; } = [];

    [ObservableProperty]
    private string _previewFormat = string.Empty;

    [ObservableProperty]
    private int _previewEndpointCount;

    [ObservableProperty]
    private bool _overwriteExisting;

    // Shared state.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    public bool IsSourceStep => Step == ImportWizardStep.Source;

    public bool IsPreviewStep => Step == ImportWizardStep.Preview;

    public bool IsResultStep => Step == ImportWizardStep.Result;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool CanGoBack => Step == ImportWizardStep.Preview;

    /// <summary>The Back/Cancel/primary buttons show on every step except the terminal Result step.</summary>
    public bool ShowNavigation => Step != ImportWizardStep.Result;

    public string PrimaryButtonText => Step switch
    {
        ImportWizardStep.Source => "Next",
        ImportWizardStep.Preview => "Import",
        _ => "Close",
    };

    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private void Browse()
    {
        var path = _fileDialog.PromptOpenFile("Import from file", ImportFileFilter);
        if (path is not null)
        {
            FilePath = path;
        }
    }

    private bool CanBrowse() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (Step == ImportWizardStep.Preview)
        {
            Step = ImportWizardStep.Source;
        }
    }

    [RelayCommand(CanExecute = nameof(CanPrimary))]
    private async Task PrimaryAsync(CancellationToken cancellationToken)
    {
        switch (Step)
        {
            case ImportWizardStep.Source:
                await PreviewAsync(cancellationToken).ConfigureAwait(true);
                break;

            case ImportWizardStep.Preview:
                await CommitAsync(cancellationToken).ConfigureAwait(true);
                break;

            default:
                break;
        }
    }

    private bool CanPrimary()
    {
        if (IsBusy)
        {
            return false;
        }

        return Step switch
        {
            ImportWizardStep.Source => HasSourceInput(),
            _ => true,
        };
    }

    private bool HasSourceInput()
    {
        if (IsPasteMode)
        {
            return !string.IsNullOrWhiteSpace(PastedText);
        }

        if (IsFileMode)
        {
            return !string.IsNullOrWhiteSpace(FilePath);
        }

        return IsUrlMode && !string.IsNullOrWhiteSpace(Url);
    }

    private async Task PreviewAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            var request = await BuildRequestAsync(cancellationToken).ConfigureAwait(true);
            if (request is null)
            {
                return;
            }

            var result = await _orchestrator.PreviewAsync(request, cancellationToken).ConfigureAwait(true);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            _preview = result.Value;
            PreviewFormat = _preview.Format;
            PreviewEndpointCount = _preview.EndpointCount;
            PreviewServices.Clear();
            foreach (var service in _preview.Services)
            {
                PreviewServices.Add(service);
            }

            Step = ImportWizardStep.Preview;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_preview is null)
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            var options = new ImportOptions { OverwriteExisting = OverwriteExisting };
            var result = await _orchestrator.CommitAsync(_preview, options, cancellationToken).ConfigureAwait(true);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            Committed = true;
            ResultSummary = Describe(result.Value);
            Step = ImportWizardStep.Result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<ImportRequest?> BuildRequestAsync(CancellationToken cancellationToken)
    {
        if (IsUrlMode)
        {
            return new ImportRequest { Uri = Url.Trim() };
        }

        if (IsFileMode)
        {
            var read = await _fileReader.ReadTextAsync(FilePath, cancellationToken).ConfigureAwait(true);
            if (read.IsFailure)
            {
                ErrorMessage = read.Error.Message;
                return null;
            }

            return new ImportRequest { Content = read.Value, FileName = Path.GetFileName(FilePath) };
        }

        return new ImportRequest { Content = PastedText };
    }

    private static string Describe(ImportSummary summary)
    {
        var parts = new List<string>();
        if (summary.ServicesCreated > 0)
        {
            parts.Add($"{summary.ServicesCreated} service(s) created");
        }

        if (summary.ServicesUpdated > 0)
        {
            parts.Add($"{summary.ServicesUpdated} service(s) updated");
        }

        parts.Add($"{summary.EndpointsCreated} endpoint(s) created");

        if (summary.EndpointsUpdated > 0)
        {
            parts.Add($"{summary.EndpointsUpdated} endpoint(s) updated");
        }

        if (summary.EndpointsSkipped > 0)
        {
            parts.Add($"{summary.EndpointsSkipped} endpoint(s) skipped");
        }

        return string.Join(", ", parts) + ".";
    }
}
