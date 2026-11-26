using System.Collections.ObjectModel;
using LP.Core.Interfaces;
using LP.UI.Infrastructure;

namespace LP.UI.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ILightningProtectorRepository _repository;

    private PageViewModel _selectedPage;
    private string _sourceName;

    public MainViewModel(ILightningProtectorRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _sourceName = repository.SourceName;
        Pages = [new LightningProtectorsPageViewModel(repository)];
        _selectedPage = Pages[0];
    }

    public string WindowTitle => "Lightning Protection";

    public string SourceName { get => _sourceName; private set => SetProperty(ref _sourceName, value); }

    public ObservableCollection<PageViewModel> Pages { get; }

    public PageViewModel SelectedPage
    {
        get => _selectedPage;
        set
        {
            // The sidebar always keeps one page selected.
            if (value is not null)
                SetProperty(ref _selectedPage, value);
        }
    }

    public void Refresh()
    {
        SourceName = _repository.SourceName;

        foreach (PageViewModel page in Pages)
            page.Refresh();
    }
}
