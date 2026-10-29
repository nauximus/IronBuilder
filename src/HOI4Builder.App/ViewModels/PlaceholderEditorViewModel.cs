namespace HOI4Builder.App.ViewModels;

public class PlaceholderEditorViewModel : ViewModelBase
{
    public PlaceholderEditorViewModel(string title) => Message = $"The {title} editor isn't built yet.";

    public string Message { get; }
}