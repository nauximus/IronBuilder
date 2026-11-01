using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Editing;

namespace HOI4Builder.App.Views;

public enum LinkMode
{
    None,
    AddPrerequisite,
    MakePrerequisiteOf,
    Exclusive,
}

public class FocusCanvas : Control
{
    private const double CellWidth = 120;
    private const double CellHeight = 100;
    private const double NodeWidth = 104;
    private const double NodeHeight = 44;

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#14181f"));
    private static readonly IBrush NodeFill = new SolidColorBrush(Color.Parse("#2b3a4f"));
    private static readonly IPen NodeBorder = new Pen(new SolidColorBrush(Color.Parse("#6d8fbf")), 1.5);
    private static readonly IPen AndLine = new Pen(new SolidColorBrush(Color.Parse("#9aa5b5")), 2);
    private static readonly IPen OrLine = new Pen(new SolidColorBrush(Color.Parse("#e0a43a")), 2, DashStyle.Dash);
    private static readonly IPen ExclusiveLine = new Pen(new SolidColorBrush(Color.Parse("#d9534f")), 2);
    private static readonly IPen SelectedBorder = new Pen(new SolidColorBrush(Color.Parse("#ffd36b")), 3);

    private FocusTree? _tree;
    private Dictionary<string, GridPoint> _positions = new();
    private double _zoom = 1;
    private Point _pan = new(40, 40);
    private bool _panning;
    private Point _lastPoint;
    private UndoStack? _history;
    private Focus? _selected;
    private Focus? _dragFocus;
    private int _dragStartX;
    private int _dragStartY;
    private Vector _grabOffset;

    private static readonly IBrush BannerFill = new SolidColorBrush(Color.Parse("#cc2b3a4f"));
    
    private LinkMode _linkMode;
    private Focus? _linkSource;

    public event Action<Focus?>? SelectionChanged;
    public event Action<string?>? Notice;

    public FocusCanvas()
    {
        Focusable = true;
    }

    public FocusTree? Tree
    {
        get => _tree;
        set
        {
            _tree = value;
            _positions = value is null ? new() : FocusLayout.ResolvePositions(value);
            SetSelected(null);
            _dragFocus = null;
            EndLink();
            InvalidateVisual();
        }
    }

    private void SetSelected(Focus? focus)
    {
        if (ReferenceEquals(_selected, focus))
            return;
        
        _selected = focus;
        SelectionChanged?.Invoke(focus);
        InvalidateVisual();
    }

    // ---- Drawing

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(BackgroundBrush, null, bounds);

        if (_tree is null)
            return;

        Console.WriteLine($"render: pan={_pan} zoom={_zoom} focuses={_tree.Focuses.Count} positions={_positions.Count}");

        try
        {
            var view = Matrix.CreateScale(_zoom, _zoom) * Matrix.CreateTranslation(_pan.X, _pan.Y);
            using (context.PushClip(bounds))
            using (context.PushTransform(view))
            {
                DrawLinks(context);
                DrawNodes(context);
            }

            if (_linkMode != LinkMode.None && _linkSource != null)
                DrawBanner(context, bounds, LinkPrompt());
        }
        catch (Exception ex)
        {
            Console.WriteLine("Render failed: " + ex);
        }
    }
    
    private void DrawLinks(DrawingContext context)
    {
        foreach (var focus in _tree!.Focuses)
        {
            if (!_positions.ContainsKey(focus.Id))
                continue;
            
            foreach (var group in focus.Prerequisites)
            {
                var pen = group.Count > 1 ? OrLine : AndLine;
                foreach (var preId in group)
                {
                    if (_positions.ContainsKey(preId))
                        DrawStep(context, pen, preId, focus.Id);
                }
            }
            foreach (var otherId in focus.MutuallyExclusive)
            {
                 if (!_positions.ContainsKey(otherId))
                    continue;
                    
                bool reciprocal = _tree.FindFocus(otherId)?.MutuallyExclusive.Contains(focus.Id) == true;
                if (!reciprocal || string.CompareOrdinal(focus.Id, otherId) < 0)
                    context.DrawLine(ExclusiveLine, Center(focus.Id), Center(otherId));
                
            }
        }
    }
    private void DrawStep(DrawingContext context, IPen pen, string fromId, string toId)
    {
        var from = BottomCenter(fromId);
        var to = TopCenter(toId);
        double midY = (from.Y + to.Y) / 2;

        context.DrawLine(pen, from, new Point(from.X, midY));
        context.DrawLine(pen, new Point(from.X, midY), new Point(to.X, midY));
        context.DrawLine(pen, new Point(to.X, midY), to);
    }

    private void DrawNodes(DrawingContext context)
    {
        foreach (var focus in _tree!.Focuses)
        {
            if (!_positions.TryGetValue(focus.Id, out var p))
                continue;
            
            var rect = NodeRect(p);
            var border = ReferenceEquals(focus, _selected) ? SelectedBorder : NodeBorder;
            context.DrawRectangle(NodeFill, border, rect, 6, 6);

            var text = new FormattedText(
                focus.Id, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Typeface.Default, 13, Brushes.White)
            {
                MaxTextWidth = NodeWidth - 10,
                MaxLineCount = 1,
                Trimming = TextTrimming.CharacterEllipsis,
            };
            context.DrawText(text, new Point(rect.X + 5, rect.Y + (NodeHeight - text.Height) / 2));
        }
    }

    private static Rect NodeRect(GridPoint p) =>
        new(p.X * CellWidth, p.Y * CellHeight, NodeWidth, NodeHeight);

    private Point ToWorld(Point screen) =>
        new((screen.X - _pan.X) / _zoom, (screen.Y - _pan.Y) / _zoom);

    private Focus? HitTest(Point world)
    {
        if (_tree is null)
            return null;
        
        for (int i = _tree.Focuses.Count - 1; i >= 0; i--)
        {
            var focus = _tree.Focuses[i];
            if (_positions.TryGetValue(focus.Id, out var p) && NodeRect(p).Contains(world))
                return focus;
        }
        return null;
    }

    private Point TopCenter(string id)
    {
        var p = _positions[id];
        return new Point(p.X * CellWidth + NodeWidth / 2, p.Y * CellHeight);
    }

    private Point BottomCenter(string id) => TopCenter(id) + new Vector(0, NodeHeight);

    private Point Center(string id) => TopCenter(id) + new Vector(0, NodeHeight / 2);

    // ---- Pan and zoom

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        this.Focus();

        var point = e.GetCurrentPoint(this);
        var screen = point.Position;
        Console.WriteLine($"pressed: left={point.Properties.IsLeftButtonPressed} right={point.Properties.IsRightButtonPressed} middle={point.Properties.IsMiddleButtonPressed} linkMode={_linkMode} panning={_panning}");

        if (point.Properties.IsRightButtonPressed)
        {
            // The menu itself opens when the button is released.
            var rightHit = HitTest(ToWorld(screen));
            if (rightHit != null && _linkMode == LinkMode.None)
                SetSelected(rightHit);

            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            var world = ToWorld(screen);
            var hit = HitTest(world);

            if (_linkMode != LinkMode.None)
            {
                CompleteLink(hit);
                e.Handled = true;
                return;
            }

            if (hit != null)
            {
                if (_selected != null && !ReferenceEquals(hit, _selected) && _tree != null && _history != null
                    && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
                {
                    string? error;
                    var command = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                        ? FocusLinkEditing.ToggleExclusive(_selected, hit, out error)
                        : FocusLinkEditing.TogglePrerequisite(_tree, hit, _selected.Id, out error);

                    if (command != null)
                        _history.Execute(command);

                    Notice?.Invoke(error);
                    e.Handled = true;
                    return;
                }

                SetSelected(hit);
                _dragFocus = hit;
                _dragStartX = hit.X;
                _dragStartY = hit.Y;

                var p = _positions[hit.Id];
                _grabOffset = new Vector(world.X - p.X * CellWidth, world.Y - p.Y * CellHeight);

                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;
            }

            SetSelected(null);
        }

        if (point.Properties.IsLeftButtonPressed || point.Properties.IsMiddleButtonPressed)
        {
            _panning = true;
            _lastPoint = screen;
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var screen = e.GetPosition(this);

        if (_dragFocus != null && _tree != null)
        {
            var world = ToWorld(screen);
            var cell = new GridPoint(
                (int)Math.Round((world.X - _grabOffset.X) / CellWidth),
                (int)Math.Round((world.Y - _grabOffset.Y) / CellHeight));

            var own = FocusLayout.ToOwnCoordinates(_dragFocus, _positions, cell);

            if (own.X != _dragFocus.X || own.Y != _dragFocus.Y)
            {
                _dragFocus.X = own.X;
                _dragFocus.Y = own.Y;
                _positions = FocusLayout.ResolvePositions(_tree);
                InvalidateVisual();
            }
            return;
        }

        if (!_panning)
            return;

        _pan += screen - _lastPoint;
        _lastPoint = screen;
        Console.WriteLine($"moved: pan={_pan}");
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        Console.WriteLine($"released: {e.InitialPressMouseButton} panning={_panning} dragging={_dragFocus != null}");

        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            if (_linkMode != LinkMode.None)
            {
                EndLink();
            }
            else
                OpenContextMenu(e.GetPosition(this));
            
            e.Handled = true;
            return;
        }

        if (_dragFocus != null)
        {
            var focus = _dragFocus;
            _dragFocus = null;
            e.Pointer.Capture(null);

            int endX = focus.X;
            int endY = focus.Y;

            if ((endX != _dragStartX || endY != _dragStartY) && _history != null)
            {
                // command records the old values itself, so restores them first
                focus.X = _dragStartX;
                focus.Y = _dragStartY;
                _history.Execute(new MoveFocusCommand(focus, endX, endY));
            }
            return;
        }

        if (!_panning)
            return;

        _panning = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var mouse = e.GetPosition(this);
        double newZoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.1 : 1 / 1.1), 0.1, 4);
        double ratio = newZoom / _zoom;

        // Keep the world point under the cursor fixed while zooming

        _pan = new Point(
            mouse.X - (mouse.X - _pan.X) * ratio,
            mouse.Y - (mouse.Y - _pan.Y) * ratio);
        _zoom = newZoom;

        InvalidateVisual();
        e.Handled = true;
    }

    public UndoStack? History
    {
        get => _history;
        set
        {
            if (_history != null)
                _history.Changed -= OnHistoryChanged;

            _history = value;

            if (_history != null)
                _history.Changed += OnHistoryChanged;
        }
    }

    private void OnHistoryChanged()
    {
        if (_tree != null)
        {
            _positions = FocusLayout.ResolvePositions(_tree);
            if (_selected != null && !_tree.Focuses.Contains(_selected))
                SetSelected(null);
            if (_linkSource != null && !_tree.Focuses.Contains(_linkSource))
                EndLink();
        }
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && _linkMode != LinkMode.None)
        {
            EndLink();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && _selected != null)
        {
            DeleteSelected();
            e.Handled = true;
        }
    }

    // Actions used by menu
    public void AddFocusAt(int cellX, int cellY)
    {
        Console.WriteLine($"AddFocusAt: cell={cellX},{cellY} tree={_tree?.Id} history={_history != null}");
        if (_tree is null || _history is null)
            return;
        
        var focus = new Focus { Id = _tree.MakeUniqueId("NEW_FOCUS_"), X = cellX, Y= cellY};
        _history.Execute(new AddFocusCommand(_tree, focus));
        SetSelected(focus);
    }
    // Used by the toolbar, puts the focs in the first free cell at or after middle of the view.
    public void AddFocusInView()
    {
        if (_tree is null)
            return;
        
        var center = ToWorld(new Point(Bounds.Width / 2, Bounds.Height / 2));
        int startX = (int)Math.Floor(center.X / CellWidth);
        int x = startX;
        int y = (int)Math.Floor(center.Y / CellHeight);

        int guard = 0;

        while (IsCellOccupied(x, y) && guard++ < 500)
        {
            x++;
            if (x > startX + 7)
            {
                x = startX;
                y++;
            }

            AddFocusAt(x, y);
        }
    }

    public void DeleteSelected()
    {
        if (_selected  != null)
            DeleteFocus(_selected);
    }

    private void DeleteFocus(Focus focus)
    {
        if (_tree is null || _history is null)
            return;
        
        if (ReferenceEquals(_selected, focus))
            SetSelected(null);
        
        _history.Execute(new DeleteFocusCommand(_tree, focus));
    }

    public void ResetView()
    {
        _zoom = 1;
        _pan = new Point(40, 40);
        InvalidateVisual();
    }

    private bool IsCellOccupied(int x, int y) => _positions.Values.Any(p => p.X == x && p.Y == y);

    // Link mode
    public void BeginLink(LinkMode mode)
    {
        if (_selected is null || _tree is null || _history is null)
            return;
        
        _linkMode = mode;
        _linkSource = _selected;
        Notice?.Invoke(null);
        this.Focus();
        InvalidateVisual();
    }

    private void EndLink()
    {
        _linkMode = LinkMode.None;
        _linkSource = null;
        InvalidateVisual();
    }

    private void CompleteLink(Focus? clicked)
    {
        var source = _linkSource;
        var mode = _linkMode;

        if (clicked is null || source is null || _tree is null || _history is null)
        {
            EndLink();
            return;
        }

        if (ReferenceEquals(clicked, source))
        {
            Notice?.Invoke("Pick a different focus or press Esc to cancel.");
            return;
        }

        string? error = null;
        IEditCommand? command = mode switch
        {
            LinkMode.AddPrerequisite => FocusLinkEditing.TogglePrerequisite(_tree, source, clicked.Id, out error),
            LinkMode.MakePrerequisiteOf => FocusLinkEditing.TogglePrerequisite(_tree, clicked, source.Id, out error),
            _ => FocusLinkEditing.ToggleExclusive(source,  clicked, out error),
        };

        if (command != null)
            _history.Execute(command);

        Notice?.Invoke(error);
        EndLink();
    }

    private string LinkPrompt() => _linkMode switch
    {
        LinkMode.AddPrerequisite => $"Click thefocus that {_linkSource!.Id} requires. ESC or right-click to cancel.",
        LinkMode.MakePrerequisiteOf => $"Click the focus that requires {_linkSource!.Id}.",
        _ => $"Click the focus that is mutually exclusive with {_linkSource!.Id}. ESC or right-click to cancel"
    };

    private static void DrawBanner(DrawingContext context, Rect bounds, string text)
    {
        var formatted = new FormattedText(
            text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            Typeface.Default, 14, Brushes.White)
        {
            MaxTextWidth = Math.Max(50, bounds.Width - 40),
        };

        var rect = new Rect(10, 10, formatted.Width + 20, formatted.Height + 14);
        context.DrawRectangle(BannerFill, null, rect, 6, 6);
        context.DrawText(formatted, new Point(20, 17));
    }

    // Context menu

    private void StartLinkFrom(Focus focus, LinkMode mode)
    {
        SetSelected(focus);
        BeginLink(mode);
    }

    private static MenuItem MenuEntry(string header, Action action, bool enabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled};
        item.Click += (_, _) => action();
        return item;
    }

    private void OpenContextMenu(Point screen)
    {
        if (_tree is null || _history is null)
            return;
        
        var world = ToWorld(screen);
        Console.WriteLine($"menu: screen={screen} world={world}");
        var menu = new ContextMenu();

        if (HitTest(world) is { } focus)
        {
            bool others = _tree.Focuses.Count > 1;

            menu.Items.Add(MenuEntry("Add prerequisite...", () => StartLinkFrom(focus, LinkMode.AddPrerequisite), others));
            menu.Items.Add(MenuEntry("Make prerequisite of...", () => StartLinkFrom(focus, LinkMode.MakePrerequisiteOf), others));
            menu.Items.Add(MenuEntry("Mutually exclusive with...", () => StartLinkFrom(focus, LinkMode.Exclusive), others));
            menu.Items.Add(new Separator());
            menu.Items.Add(MenuEntry("Delete focus", () => DeleteFocus(focus)));
        }
        else
        {
            int cellX = (int)Math.Floor(world.X / CellWidth);
            int cellY = (int)Math.Floor(world.Y / CellHeight);
            bool occupied = IsCellOccupied(cellX, cellY);

            menu.Items.Add(MenuEntry(
                occupied ? "Add focus here (cell occupied)" : "Add focus here",
                () => AddFocusAt(cellX, cellY),
                !occupied));

            menu.Items.Add(new Separator());
            menu.Items.Add(MenuEntry("Reset view", ResetView));
        }

        menu.Open(this);
    }
}