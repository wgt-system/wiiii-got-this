using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private void OnSearchResultClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: AtlasNodePresentationViewModel node } || shell is null)
            return;

        SelectSearchResult(node);
        e.Handled = true;
    }

    private void OnSearchKeyDownRanked(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || shell?.AtlasSearchResults.FirstOrDefault() is not { } node)
            return;

        SelectSearchResult(node);
        e.Handled = true;
    }

    private void SelectSearchResult(AtlasNodePresentationViewModel node)
    {
        shell?.SelectAtlasNodeCommand.Execute(node);
        if (shell is not null)
            shell.AtlasSearchText = string.Empty;
        CenterOnSelected();
        AtlasViewport.Focus();
    }
}
