using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyPad.Views.Controls
{
    public static class TextEditorCommands
    {
        private static ICommand CreateRoutedCommand<T>(InputGestureCollection inputGestures = null, [CallerMemberName] string commandName = "")
            => new RoutedCommand(commandName, typeof(T), inputGestures);

        public static readonly ICommand ConvertToNarrow
            = CreateRoutedCommand<TextEditor>();

        public static readonly ICommand ConvertToWide
            = CreateRoutedCommand<TextEditor>();

        public static readonly ICommand ZoomIn
            = CreateRoutedCommand<TextEditor>(new InputGestureCollection { new KeyGesture(Key.OemPlus, ModifierKeys.Control) });

        public static readonly ICommand ZoomOut
            = CreateRoutedCommand<TextEditor>(new InputGestureCollection { new KeyGesture(Key.OemMinus, ModifierKeys.Control) });

        public static readonly ICommand ZoomReset
            = CreateRoutedCommand<TextEditor>(new InputGestureCollection { new KeyGesture(Key.D0, ModifierKeys.Control) });

        public static readonly ICommand Completion
            = CreateRoutedCommand<TextEditor>(new InputGestureCollection { new KeyGesture(Key.Space, ModifierKeys.Control) });

        public static readonly ICommand ReplaceNext
            = CreateRoutedCommand<TextEditor>(new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Alt) });

        public static readonly ICommand ReplaceAll
            = CreateRoutedCommand<TextEditor>( new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Alt) });
    }
}
