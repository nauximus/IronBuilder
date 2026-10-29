using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Editing;

namespace HOI4Builder.App.Views;

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
            _selected = null;
            _dragFocus = null;
            InvalidateVisual();
        }
    }

    // ---- Drawing

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(BackgroundBrush, null, bounds);
        if (_tree is null)
        {
            Console.WriteLine("Render: tree is null");
            return;
        }
        try
        {
            var view = Matrix.CreateScale(_zoom, _zoom) * Matrix.CreateTranslation(_pan.X, _pan.Y);
            using (context.PushClip(bounds))
            using (context.PushTransform(view))
            {
                DrawLinks(context);
                DrawNodes(context);
            }
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

        if (point.Properties.IsLeftButtonPressed)
        {
            var world = ToWorld(screen);
            var hit = HitTest(world);

            if (hit != null)
            {
                _selected = hit;
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

            _selected = null;
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

        if(_dragFocus != null && _tree != null)
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
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
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
            _positions = FocusLayout.ResolvePositions(_tree);
        InvalidateVisual();
    }
}