using System.Collections.ObjectModel;

namespace WiiiiGotThis.Presentation;

public sealed partial class ShellViewModel
{
    public ObservableCollection<AtlasNodePresentationViewModel> AtlasSearchResults { get; } = [];
    public bool HasAtlasSearchResults => AtlasSearchResults.Count > 0;

    partial void OnAtlasSearchTextChanged(string value) => RebuildAtlasSearchResults(value);

    private void RebuildAtlasSearchResults(string? query)
    {
        AtlasSearchResults.Clear();
        foreach (var result in AtlasSearch.Find(AtlasNodes, query))
            AtlasSearchResults.Add(result);
        OnPropertyChanged(nameof(HasAtlasSearchResults));
    }
}
