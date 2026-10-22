using System.Windows;
using System.Windows.Input;

namespace Tm.Ant.Module.Rework.Behaviors;

/// <summary>
/// 将图片坐标平面上的点击转换为归一化坐标并交给标定命令。
/// </summary>
public static class ScrewCalibrationBehavior
{
    public static readonly DependencyProperty ClickCommandProperty = DependencyProperty.RegisterAttached(
        "ClickCommand", typeof(ICommand), typeof(ScrewCalibrationBehavior),
        new PropertyMetadata(null, OnCommandChanged));

    public static ICommand GetClickCommand(DependencyObject target) => (ICommand)target.GetValue(ClickCommandProperty);

    public static void SetClickCommand(DependencyObject target, ICommand value)
    {
        target.SetValue(ClickCommandProperty, value);
    }

    private static void OnCommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is FrameworkElement element)
        {
            element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            if (args.NewValue != null)
            {
                element.MouseLeftButtonDown += OnMouseLeftButtonDown;
            }
        }
    }

    private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
    {
        if (args.ClickCount != 1)
        {
            return;
        }

        var element = (FrameworkElement)sender;
        var position = args.GetPosition(element);
        var point = ScrewDisplayGeometry.Normalize(position.X, position.Y, element.ActualWidth, element.ActualHeight);
        var command = GetClickCommand(element);
        if (point != null && command?.CanExecute(point) == true)
        {
            command.Execute(point);
            args.Handled = true;
        }
    }
}
