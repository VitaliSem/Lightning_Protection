using LP.UI.Services;
using LP.UI.ViewModels;

namespace LP.UI.Tests.Fakes;

internal class FakeDialogService : IDialogService
{
    public Func<AddLightningProtectorViewModel, bool> OnAdd { get; set; } = _ => false;

    public List<AddLightningProtectorViewModel> AddDialogs { get; } = [];

    public bool ShowAddLightningProtector(AddLightningProtectorViewModel viewModel)
    {
        AddDialogs.Add(viewModel);
        return OnAdd(viewModel);
    }

    public static bool FillAndConfirm(AddLightningProtectorViewModel dialog)
    {
        var closed = false;
        dialog.CloseRequested += (_, result) => closed = result;

        dialog.HeightText = "25";
        dialog.XText = "10";
        dialog.YText = "20";
        dialog.AddCommand.Execute(null);

        return closed;
    }
}
