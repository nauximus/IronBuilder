using HOI4Builder.Core.Focuses;

namespace HOI4Builder.Core.Editing;

public sealed class MoveFocusCommand : IEditCommand
{
    private readonly Focus _focus;
    private readonly int _oldX, _oldY, _newX, _newY;

    public MoveFocusCommand(Focus focus, int newX, int newY)
    {
        _focus = focus;
        _oldX = focus.X;
        _oldY = focus.Y;
        _newX = newX;
        _newY = newY;
    }

    public string Description => $"Move {_focus.Id}";

    public void Do()
    {
        _focus.X = _newX;
        _focus.Y = _newY;

    }

    public void Undo()
    {
        _focus.X = _oldX;
        _focus.Y = _oldY;
    }
}