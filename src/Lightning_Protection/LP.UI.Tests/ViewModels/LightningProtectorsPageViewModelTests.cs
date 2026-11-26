using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.UI.ViewModels;
using static LP.UI.Tests.ViewModels.FakeLightningProtectorRepository;

namespace LP.UI.Tests.ViewModels;

public class LightningProtectorsPageViewModelTests
{
    private readonly FakeLightningProtectorRepository _repository = new();

    private LightningProtectorsPageViewModel CreateLoaded()
    {
        var page = new LightningProtectorsPageViewModel(_repository);
        page.Refresh();
        return page;
    }

    // -------------------------------------------------------------------------
    // Loading
    // -------------------------------------------------------------------------

    [Fact]
    public void Refresh_FillsItemsInRepositoryOrder()
    {
        _repository.SetEntries(Entry(1), Entry(2), Entry(5));

        var page = CreateLoaded();

        Assert.Equal([1, 2, 5], page.Items.Select(i => i.Number));
    }

    [Fact]
    public void Refresh_SummaryCountsTotalAndActive()
    {
        _repository.SetEntries(Entry(1), Entry(2, isActive: false), Entry(3));

        var page = CreateLoaded();

        Assert.Equal("3 total · 2 active", page.Summary);
        Assert.Equal(page.Summary, page.Subtitle);
    }

    [Fact]
    public void Refresh_EmptyDrawing_ShowsNoItems()
    {
        var page = CreateLoaded();

        Assert.Empty(page.Items);
        Assert.Equal("0 total · 0 active", page.Summary);
        Assert.False(page.HasStatus);
    }

    [Fact]
    public void Refresh_RaisesSummaryChanged()
    {
        var page = CreateLoaded();
        var changed = new List<string?>();
        page.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _repository.SetEntries(Entry(1));
        page.Refresh();

        Assert.Contains(nameof(page.Summary), changed);
        Assert.Contains(nameof(page.Subtitle), changed);
    }

    [Fact]
    public void Refresh_ReplacesItemsOnReload()
    {
        _repository.SetEntries(Entry(1), Entry(2));
        var page = CreateLoaded();

        _repository.SetEntries(Entry(3));
        page.Refresh();

        Assert.Equal([3], page.Items.Select(i => i.Number));
    }

    [Fact]
    public void Refresh_Failure_ClearsItemsAndShowsMessage()
    {
        _repository.SetEntries(Entry(1));
        var page = CreateLoaded();

        _repository.GetAllResult = OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Fail("Read failed.");
        page.Refresh();

        Assert.Empty(page.Items);
        Assert.Equal("Read failed.", page.StatusMessage);
        Assert.True(page.HasStatus);
    }

    [Fact]
    public void Refresh_SuccessAfterFailure_ClearsLoadError()
    {
        _repository.GetAllResult = OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Fail("Read failed.");
        var page = CreateLoaded();

        _repository.SetEntries(Entry(1));
        page.Refresh();

        Assert.False(page.HasStatus);
    }

    [Fact]
    public void Refresh_KeepsActionMessage()
    {
        _repository.SetEntries(Entry(1));
        var page = CreateLoaded();
        page.AddCommand.Execute(null);
        string message = page.StatusMessage;

        page.Refresh();

        Assert.Equal(message, page.StatusMessage);
    }

    // -------------------------------------------------------------------------
    // Selection
    // -------------------------------------------------------------------------

    [Fact]
    public void Refresh_KeepsSelectionByNumber()
    {
        _repository.SetEntries(Entry(1), Entry(2), Entry(3));
        var page = CreateLoaded();
        page.SelectedItem = page.Items[1];

        _repository.SetEntries(Entry(2), Entry(3));
        page.Refresh();

        Assert.NotNull(page.SelectedItem);
        Assert.Equal(2, page.SelectedItem!.Number);
        Assert.Same(page.Items[0], page.SelectedItem);
    }

    [Fact]
    public void Refresh_SelectedProtectorGone_ClearsSelection()
    {
        _repository.SetEntries(Entry(1), Entry(2));
        var page = CreateLoaded();
        page.SelectedItem = page.Items[1];

        _repository.SetEntries(Entry(1));
        page.Refresh();

        Assert.Null(page.SelectedItem);
        Assert.Equal("none", page.SelectedText);
    }

    // -------------------------------------------------------------------------
    // Focus
    // -------------------------------------------------------------------------

    [Fact]
    public void FocusCommand_FocusesRowNumberAndSelectsRow()
    {
        _repository.SetEntries(Entry(1), Entry(7));
        var page = CreateLoaded();
        var row = page.Items[1];

        page.FocusCommand.Execute(row);

        Assert.Equal([7], _repository.FocusedNumbers);
        Assert.Same(row, page.SelectedItem);
    }

    [Fact]
    public void FocusCommand_Success_ShowsMessage()
    {
        _repository.SetEntries(Entry(1));
        _repository.FocusResult = OperationResult<string>.Ok("", "Lightning protector #1 in focus.");
        var page = CreateLoaded();

        page.FocusCommand.Execute(page.Items[0]);

        Assert.Equal("Lightning protector #1 in focus.", page.StatusMessage);
    }

    [Fact]
    public void FocusCommand_Failure_ShowsMessage()
    {
        _repository.SetEntries(Entry(1));
        _repository.FocusResult = OperationResult<string>.Fail("Lightning protector #1 not found.");
        var page = CreateLoaded();

        page.FocusCommand.Execute(page.Items[0]);

        Assert.Equal("Lightning protector #1 not found.", page.StatusMessage);
    }

    [Fact]
    public void FocusCommand_CannotExecuteWithoutRow()
    {
        var page = CreateLoaded();

        Assert.False(page.FocusCommand.CanExecute(null));
    }
}
