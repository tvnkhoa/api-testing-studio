using System.Collections;
using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.UI.ViewModels.Panels;
using AvalonDock;
using AvalonDock.Layout.Serialization;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.UI.Services;

/// <summary>
/// AvalonDock implementation of <see cref="IDockManager"/>. Serializes the docking layout to an XML
/// string with <see cref="XmlLayoutSerializer"/> and delegates storage to
/// <see cref="ILayoutPersistenceService"/> — keeping WPF serialization out of view models and file
/// I/O out of the UI. On restore, panes are re-associated with the live view models bound to the
/// manager's <c>DocumentsSource</c>/<c>AnchorablesSource</c> by matching
/// <see cref="PanelViewModel.ContentId"/>; unknown panes (e.g. a removed plugin) are dropped.
/// </summary>
public sealed class DockManagerService : IDockManager
{
    private readonly ILayoutPersistenceService _layoutPersistence;
    private readonly ILogger<DockManagerService> _logger;

    private DockingManager? _dockingManager;
    private string? _defaultLayoutXml;

    public DockManagerService(ILayoutPersistenceService layoutPersistence, ILogger<DockManagerService> logger)
    {
        ArgumentNullException.ThrowIfNull(layoutPersistence);
        ArgumentNullException.ThrowIfNull(logger);
        _layoutPersistence = layoutPersistence;
        _logger = logger;
    }

    public void Attach(object dockingManager)
    {
        ArgumentNullException.ThrowIfNull(dockingManager);
        _dockingManager = dockingManager as DockingManager
            ?? throw new ArgumentException("Expected an AvalonDock DockingManager.", nameof(dockingManager));

        // Snapshot the default (XAML-generated) arrangement so Reset Layout can restore it.
        _defaultLayoutXml = Serialize();
    }

    public async Task<bool> LoadLayoutAsync(CancellationToken cancellationToken = default)
    {
        EnsureAttached();

        var xml = await _layoutPersistence.LoadLayoutAsync(cancellationToken).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return false;
        }

        try
        {
            Deserialize(xml);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Xml.XmlException)
        {
            // A layout written by an incompatible AvalonDock version or a corrupt file must not stop
            // the app: fall back to the default arrangement and discard the bad layout.
            _logger.LogWarning(ex, "Saved dock layout could not be restored; using the default layout.");
            if (_defaultLayoutXml is not null)
            {
                Deserialize(_defaultLayoutXml);
            }

            return false;
        }
    }

    public async Task SaveLayoutAsync(CancellationToken cancellationToken = default)
    {
        EnsureAttached();

        var xml = Serialize();
        await _layoutPersistence.SaveLayoutAsync(xml, cancellationToken).ConfigureAwait(true);
    }

    public async Task ResetLayoutAsync(CancellationToken cancellationToken = default)
    {
        EnsureAttached();

        await _layoutPersistence.ClearAsync(cancellationToken).ConfigureAwait(true);
        if (_defaultLayoutXml is not null)
        {
            Deserialize(_defaultLayoutXml);
        }
    }

    private string Serialize()
    {
        var serializer = new XmlLayoutSerializer(_dockingManager!);
        using var writer = new StringWriter();
        serializer.Serialize(writer);
        return writer.ToString();
    }

    private void Deserialize(string xml)
    {
        var serializer = new XmlLayoutSerializer(_dockingManager!);
        serializer.LayoutSerializationCallback += OnLayoutSerializationCallback;
        try
        {
            using var reader = new StringReader(xml);
            serializer.Deserialize(reader);
        }
        finally
        {
            serializer.LayoutSerializationCallback -= OnLayoutSerializationCallback;
        }
    }

    private void OnLayoutSerializationCallback(object? sender, LayoutSerializationCallbackEventArgs e)
    {
        var match = EnumeratePanels().FirstOrDefault(p => p.ContentId == e.Model.ContentId);
        if (match is not null)
        {
            e.Content = match;
        }
        else
        {
            // Unknown pane (e.g. a plugin that is no longer present): drop it gracefully.
            e.Cancel = true;
        }
    }

    private IEnumerable<PanelViewModel> EnumeratePanels()
    {
        foreach (var source in new[] { _dockingManager!.DocumentsSource, _dockingManager!.AnchorablesSource })
        {
            if (source is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is PanelViewModel panel)
                    {
                        yield return panel;
                    }
                }
            }
        }
    }

    private void EnsureAttached()
    {
        if (_dockingManager is null)
        {
            throw new InvalidOperationException("Attach must be called with the DockingManager before using the dock manager.");
        }
    }
}
