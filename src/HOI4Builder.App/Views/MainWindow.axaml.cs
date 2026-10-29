using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;
using Avalonia.Controls;
using System;
using HOI4Builder.Core.Editing;

namespace HOI4Builder.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        const string sample = """
            focus_tree = {
            id = demo
            focus = { id = DEMO_start x = 2 y = 0 }
            focus = { id = DEMO_left x = 1 y = 1 prerequisite = { focus = DEMO_start } mutually_exclusive = { focus = DEMO_right } }
            focus = { id = DEMO_right x = 3 y = 1 prerequisite = { focus = DEMO_start } mutually_exclusive = { focus = DEMO_left } }
            focus = { id = DEMO_join x = 0 y = 2 relative_position_id = DEMO_right prerequisite = { focus = DEMO_left focus = DEMO_right } }
    }
    """;
    FocusView.Tree = FocusTreeMapper.ReadTrees(PdxParser.Parse(sample))[0];
    FocusView.History = new UndoStack();
    }
}