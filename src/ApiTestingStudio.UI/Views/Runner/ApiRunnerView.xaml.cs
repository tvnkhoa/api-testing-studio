using System.Windows.Controls;

namespace ApiTestingStudio.UI.Views.Runner;

/// <summary>
/// The API Runner document view. No code-behind logic: the request builder, response viewer and
/// history bind directly to <see cref="ViewModels.Runner.ApiRunnerViewModel"/>.
/// </summary>
public partial class ApiRunnerView : UserControl
{
    public ApiRunnerView() => InitializeComponent();
}
