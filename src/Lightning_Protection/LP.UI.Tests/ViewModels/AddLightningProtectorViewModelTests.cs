using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.UI.Tests.Fakes;
using LP.UI.ViewModels;

namespace LP.UI.Tests.ViewModels;

public class AddLightningProtectorViewModelTests
{
    private readonly FakeLightningProtectorRepository _repository = new();
    private readonly List<bool> _closeResults = [];

    private AddLightningProtectorViewModel Create(int nextNumber = 1)
    {
        var dialog = new AddLightningProtectorViewModel(_repository, nextNumber);
        dialog.CloseRequested += (_, result) => _closeResults.Add(result);
        return dialog;
    }

    private AddLightningProtectorViewModel CreateValid()
    {
        var dialog = Create();
        dialog.HeightText = "25";
        dialog.XText = "100";
        dialog.YText = "200";
        return dialog;
    }

    // -------------------------------------------------------------------------
    // Defaults
    // -------------------------------------------------------------------------

    [Fact]
    public void Types_AreNoneThenTheFiveTypesWithDescriptions()
    {
        var dialog = Create();

        Assert.Equal(["None", "Air rod", "Air mast", "Isolated terminal", "Combined mast", "Suspension point"],
            dialog.Types.Select(t => t.Name));
        Assert.Same(AddLightningProtectorViewModel.NoType, dialog.Types[0]);
        Assert.Equal(LightningProtectorTypeOption.All, dialog.Types.Skip(1));
        Assert.Equal(
            "A vertical spike-type element mounted directly onto a roof, facade, pipe stand, tripod, or substation gantry.",
            dialog.Types[1].Description);
        Assert.Equal(
            "A free-standing supporting structure (tubular or lattice) installed on its own foundation or base.",
            dialog.Types[2].Description);
        Assert.Equal(
            "A rod or mast fitted with an insulating spacer to maintain the required separation distance from conductive parts of the structure.",
            dialog.Types[3].Description);
        Assert.Equal(
            "A dual-purpose structure (such as a floodlight mast, lighting pole, or tower) equipped with an air-termination rod at the top.",
            dialog.Types[4].Description);
        Assert.Equal(
            "An attachment node used to anchor a catenary air-termination wire to a mast, pole, or building structure.",
            dialog.Types[5].Description);
    }

    [Fact]
    public void Defaults_NoTypeActiveNoTemplateNoErrors()
    {
        var dialog = Create(7);

        Assert.Same(AddLightningProtectorViewModel.NoType, dialog.SelectedType);
        Assert.True(dialog.IsActive);
        Assert.False(dialog.SaveAsTemplate);
        Assert.False(dialog.CanSaveAsTemplate);
        Assert.Equal("Number #7 is assigned automatically.", dialog.NumberHintText);
        Assert.Equal("", dialog.GetError(nameof(dialog.HeightText)));
        Assert.False(dialog.HasErrors);
        Assert.False(dialog.HasErrorMessage);
    }

    [Fact]
    public void SelectedType_IgnoresNull()
    {
        var dialog = Create();
        dialog.SelectedType = dialog.Types[3];

        dialog.SelectedType = null!;

        Assert.Same(dialog.Types[3], dialog.SelectedType);
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Add_EmptyForm_ShowsRequiredErrorsAndDoesNotInsert()
    {
        var dialog = Create();

        dialog.AddCommand.Execute(null);

        Assert.Equal("Enter the height.", dialog.GetError(nameof(dialog.HeightText)));
        Assert.Equal("Enter X and Y or pick the position on the drawing.", dialog.ErrorMessage);
        Assert.Empty(_repository.Inserted);
        Assert.Empty(_closeResults);
        Assert.Null(dialog.Inserted);
    }

    [Theory]
    [InlineData("abc", "The height must be a number.")]
    [InlineData("0", "The height must be greater than 0 m.")]
    [InlineData("-5", "The height must be greater than 0 m.")]
    [InlineData("150.5", "The height must not exceed 150 m.")]
    public void Add_InvalidHeight_ShowsErrorAndDoesNotInsert(string height, string expected)
    {
        var dialog = CreateValid();
        dialog.HeightText = height;

        dialog.AddCommand.Execute(null);

        Assert.Equal(expected, dialog.GetError(nameof(dialog.HeightText)));
        Assert.Empty(_repository.Inserted);
    }

    [Fact]
    public void Add_InvalidBaseHeight_ShowsErrorAndDoesNotInsert()
    {
        var dialog = CreateValid();
        dialog.BaseHeightText = "1..5";

        dialog.AddCommand.Execute(null);

        Assert.Equal("The base height must be a number.", dialog.GetError(nameof(dialog.BaseHeightText)));
        Assert.Empty(_repository.Inserted);
    }

    [Fact]
    public void Add_InvalidCoordinate_ShowsErrorAndDoesNotInsert()
    {
        var dialog = CreateValid();
        dialog.YText = "north";

        dialog.AddCommand.Execute(null);

        Assert.Equal("X and Y must be numbers.", dialog.ErrorMessage);
        Assert.Empty(_repository.Inserted);
    }

    [Fact]
    public void Add_MissingCoordinate_MarksBothBoxesAndShowsMessageAtTheBottom()
    {
        var dialog = CreateValid();
        dialog.XText = "";

        dialog.AddCommand.Execute(null);

        const string expected = "Enter X and Y or pick the position on the drawing.";
        Assert.Equal(expected, dialog.GetError(nameof(dialog.XText)));
        Assert.Equal(expected, dialog.GetError(nameof(dialog.YText)));
        Assert.Equal(expected, dialog.ErrorMessage);
        Assert.True(dialog.HasErrorMessage);
    }

    [Fact]
    public void FieldErrors_AreReportedPerProperty()
    {
        var dialog = Create();
        var changed = new List<string?>();
        dialog.ErrorsChanged += (_, e) => changed.Add(e.PropertyName);

        dialog.AddCommand.Execute(null);

        Assert.True(dialog.HasErrors);
        Assert.Equal(["Enter the height."], dialog.GetErrors(nameof(dialog.HeightText)).Cast<string>());
        Assert.Empty(dialog.GetErrors(nameof(dialog.BaseHeightText)).Cast<string>());
        Assert.Contains(nameof(dialog.HeightText), changed);
        Assert.DoesNotContain(nameof(dialog.BaseHeightText), changed);
    }

    [Fact]
    public void CoordinatesFixed_ClearsBottomMessage()
    {
        var dialog = CreateValid();
        dialog.XText = "";
        dialog.AddCommand.Execute(null);

        dialog.XText = "100";

        Assert.Equal("", dialog.GetError(nameof(dialog.XText)));
        Assert.Equal("", dialog.GetError(nameof(dialog.YText)));
        Assert.False(dialog.HasErrorMessage);
        Assert.False(dialog.HasErrors);
    }

    [Fact]
    public void PickFailure_WithCoordinateError_ShowsBothMessages()
    {
        _repository.PickResult = OperationResult<Point3D>.Fail("No position was picked on the drawing.");
        var dialog = Create();
        dialog.HeightText = "25";
        dialog.AddCommand.Execute(null);

        dialog.PickPosition();

        Assert.Equal("Enter X and Y or pick the position on the drawing.\nNo position was picked on the drawing.",
            dialog.ErrorMessage);
    }

    [Fact]
    public void Add_TooLongName_ShowsErrorAndDoesNotInsert()
    {
        var dialog = CreateValid();
        dialog.Name = new string('N', 101);

        dialog.AddCommand.Execute(null);

        Assert.Equal("Name must not exceed 100 characters (entered 101).", dialog.GetError(nameof(dialog.Name)));
        Assert.Empty(_repository.Inserted);
    }

    [Fact]
    public void Errors_AreNotShownBeforeTheFirstConfirmation()
    {
        var dialog = Create();

        dialog.HeightText = "abc";

        Assert.Equal("", dialog.GetError(nameof(dialog.HeightText)));
    }

    [Fact]
    public void Errors_FollowChangesAfterTheFirstConfirmation()
    {
        var dialog = Create();
        dialog.AddCommand.Execute(null);

        dialog.HeightText = "12";

        Assert.Equal("", dialog.GetError(nameof(dialog.HeightText)));
        Assert.NotEqual("", dialog.ErrorMessage);
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("12,5", 12.5)]
    [InlineData(" 7 ", 7)]
    [InlineData("-3.25", -3.25)]
    public void TryParseNumber_AcceptsDotOrCommaSeparator(string text, double expected)
    {
        Assert.True(AddLightningProtectorViewModel.TryParseNumber(text, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1e400")]
    [InlineData("NaN")]
    [InlineData("12 m")]
    public void TryParseNumber_RejectsNonFiniteOrText(string text)
    {
        Assert.False(AddLightningProtectorViewModel.TryParseNumber(text, out _));
    }

    // -------------------------------------------------------------------------
    // Insertion
    // -------------------------------------------------------------------------

    [Fact]
    public void Add_Valid_InsertsEnteredValuesAndCloses()
    {
        var dialog = CreateValid();
        dialog.Name = "MP-7";
        dialog.Title = "Mast MM-30";
        dialog.SelectedType = dialog.Types[2];
        dialog.HeightText = "30,5";
        dialog.BaseHeightText = "-1.2";
        dialog.Description = "East facade";

        dialog.AddCommand.Execute(null);

        LightningProtectorInsertDto dto = Assert.Single(_repository.Inserted);
        Assert.Equal("MP-7", dto.Name);
        Assert.Equal("Mast MM-30", dto.Title);
        Assert.Equal("Air mast", dto.Type);
        Assert.Equal(30.5, dto.HeightM);
        Assert.Equal(-1.2, dto.BaseHeightM);
        Assert.Equal("East facade", dto.Description);
        Assert.Equal(100, dto.Position.X);
        Assert.Equal(200, dto.Position.Y);
        Assert.Equal([true], _closeResults);
        Assert.Equal(1, dialog.Inserted?.Number);
    }

    [Fact]
    public void Add_NoType_InsertsWithEmptyType()
    {
        var dialog = CreateValid();

        dialog.AddCommand.Execute(null);

        Assert.Equal("", Assert.Single(_repository.Inserted).Type);
        Assert.Equal([true], _closeResults);
    }

    [Fact]
    public void Add_TypeChangedBackToNone_InsertsWithEmptyType()
    {
        var dialog = CreateValid();
        dialog.SelectedType = dialog.Types[1];
        dialog.SelectedType = AddLightningProtectorViewModel.NoType;

        dialog.AddCommand.Execute(null);

        Assert.Equal("", Assert.Single(_repository.Inserted).Type);
    }

    [Fact]
    public void Add_EmptyBaseHeight_InsertsWithoutBaseHeight()
    {
        var dialog = CreateValid();

        dialog.AddCommand.Execute(null);

        Assert.Null(Assert.Single(_repository.Inserted).BaseHeightM);
    }

    [Fact]
    public void Add_InsertFails_ShowsMessageAndStaysOpen()
    {
        _repository.InsertResult = OperationResult<LightningProtectorBlockData>.Fail("No drawing is open.");
        var dialog = CreateValid();

        dialog.AddCommand.Execute(null);

        Assert.Equal("No drawing is open.", dialog.ErrorMessage);
        Assert.True(dialog.HasErrorMessage);
        Assert.Empty(_closeResults);
        Assert.Null(dialog.Inserted);
    }

    [Fact]
    public void Add_SucceedsAfterFailedAttempt_ClearsMessage()
    {
        _repository.InsertResult = OperationResult<LightningProtectorBlockData>.Fail("No drawing is open.");
        var dialog = CreateValid();
        dialog.AddCommand.Execute(null);

        _repository.InsertResult = null;
        dialog.AddCommand.Execute(null);

        Assert.False(dialog.HasErrorMessage);
        Assert.Equal([true], _closeResults);
    }

    // -------------------------------------------------------------------------
    // Picking the position
    // -------------------------------------------------------------------------

    [Fact]
    public void PickCommand_RequestsPickAndClosesWithoutInserting()
    {
        var dialog = CreateValid();

        dialog.PickCommand.Execute(null);

        Assert.True(dialog.IsPickRequested);
        Assert.Equal([false], _closeResults);
        Assert.Empty(_repository.Inserted);
        Assert.Equal(0, _repository.PickCount);
    }

    [Fact]
    public void PickPosition_Success_FillsCoordinates()
    {
        _repository.PickResult = OperationResult<Point3D>.Ok(new Point3D(1234.56789, -20.5, 3));
        var dialog = Create();
        dialog.PickCommand.Execute(null);

        dialog.PickPosition();

        Assert.False(dialog.IsPickRequested);
        Assert.Equal("1234.5679", dialog.XText);
        Assert.Equal("-20.5", dialog.YText);
        Assert.False(dialog.HasErrorMessage);
    }

    [Fact]
    public void PickPosition_Failure_KeepsCoordinatesAndShowsMessage()
    {
        _repository.PickResult = OperationResult<Point3D>.Fail("No position was picked on the drawing.");
        var dialog = CreateValid();

        dialog.PickPosition();

        Assert.Equal("100", dialog.XText);
        Assert.Equal("200", dialog.YText);
        Assert.Equal("No position was picked on the drawing.", dialog.ErrorMessage);
    }
}
