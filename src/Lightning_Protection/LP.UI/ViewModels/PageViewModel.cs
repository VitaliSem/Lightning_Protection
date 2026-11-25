using LP.UI.Infrastructure;

namespace LP.UI.ViewModels;

public abstract class PageViewModel : ObservableObject
{
    public abstract string Title { get; }

    public abstract string Subtitle { get; }

    // Path markup in a 16×16 box, drawn with a stroke in the sidebar.
    public abstract string IconData { get; }

    public abstract void Refresh();
}
