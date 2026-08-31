using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Newtonsoft.Json;

using Tm.Ant.Module.Rework.Models;

namespace Tm.Ant.Module.Rework.Views;
/// <summary>
/// PCBResultDisplay.xaml 的交互逻辑
/// </summary>
public partial class PCBResultDisplay : UserControl
{
    private readonly DrawHelper _drawHelper;
    private readonly IEventBus _eventBus;
    public PCBResultDisplay(IEventBus eventBus)
    {
        InitializeComponent();
        _eventBus = eventBus;
        _eventBus.Subscribe<FlowEventPubSub>(ReceiveFlowResult);
        _drawHelper = new DrawHelper(MapCanvas, false);
    }
    private void ReceiveFlowResult(FlowEventPubSub flowEventPubSub)
    {
        if (flowEventPubSub == null || flowEventPubSub.CaveResults.Count == 0) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            _drawHelper.RemoveAll();
            _tbProductCode.Text = flowEventPubSub.ProductCode;
            foreach (var item in flowEventPubSub.CaveResults)
            {
                OnMarker(item.Key, item.Value, GetColor(item.Value));
            }
        });
    }
    private void OnMarker(int index, string text, Color color)
    {
        //backgroud
        var datas = FileHelper.GetFileRectangleDatas();
        var rect = datas.Find(x => x.Index == index);
        if (rect == null) return;
        _drawHelper.AddRect(rect);
        _drawHelper.SetMarker(index, text, color);
    }
    private Color GetColor(string text)
    {
        return text == "OK" ? Colors.DarkGreen : Colors.Red;
    }


    #region test
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var index = int.Parse(tbIndex.Text.Trim());
        if (sender is Button btn && btn.Tag != null)
        {
            var flag = btn.Tag.ToString();
            if (flag == "OK")
            {
                // _drawHelper.SetColor(0, 122, 204, flag);
                // _drawHelper.SetColor(Colors.Green);

                OnMarker(index, "OK", Colors.DarkGreen);
            }
            else if (flag == "NG")
            {
                // _drawHelper.SetColor(220, 50, 50, flag);
                //_drawHelper.SetColor(Colors.Red);
                OnMarker(index, "NG", Colors.Red);
            }
            else
            {
                // _drawHelper.SetColor(0, 153, 68, "待加工");
                // _drawHelper.SetColor(Colors.Blue);
                OnMarker(index, "OK", Colors.Blue);
            }
        }
    }

    #endregion

}

public class RectangleData
{
    public int Index { get; set; }
    public double RelativeX { get; set; } // 相对 X 坐标（0~1）
    public double RelativeY { get; set; } // 相对 Y 坐标（0~1）
    public double RelativeWidth { get; set; } // 相对宽度（0~1）
    public double RelativeHeight { get; set; } // 相对高度（0~1）

    [JsonIgnore]
    public Rectangle Rectangle { get; set; } // 对应的 Rectangle 对象
    [JsonIgnore]
    public TextBlock Label { get; set; }
}
public class DrawHelper
{
    private Point _startPoint; // 鼠标按下时的起始点
    private Rectangle _currentRectangle; // 当前正在绘制的矩形
    private readonly List<RectangleData> _rectangles = new(); // 保存所有矩形的相对数据

    private SolidColorBrush _solidColorBrush;
    private SolidColorBrush _borderColorBrush;
    private string _text;

    private readonly Canvas _mapCanvas;
    public DrawHelper(Canvas canvas, bool mouseEventEnabled)
    {
        _mapCanvas = canvas;
        if (!mouseEventEnabled)
        {
            _mapCanvas.MouseLeftButtonDown += MapCanvas_MouseLeftButtonDown;
            _mapCanvas.MouseLeftButtonUp += MapCanvas_MouseLeftButtonUp;
            _mapCanvas.MouseMove += MapCanvas_MouseMove;
            _mapCanvas.SizeChanged += MapCanvas_SizeChanged;
        }
    }

    #region mouse event
    // 鼠标左键按下事件：开始绘制矩形
    private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 获取鼠标按下的起始点
        _startPoint = e.GetPosition(_mapCanvas);

        // 创建一个新的矩形
        _currentRectangle = new Rectangle
        {
            Stroke = _borderColorBrush, // 边框颜色
            StrokeThickness = 2, // 边框宽度
            Fill = Brushes.LightGray, // _solidColorBrush,// new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)), // 半透明填充颜色
            Opacity = 0.86,
        };

        // 设置矩形的初始位置
        Canvas.SetLeft(_currentRectangle, _startPoint.X);
        Canvas.SetTop(_currentRectangle, _startPoint.Y);

        // 添加到 Canvas
        _mapCanvas.Children.Add(_currentRectangle);
    }

    // 鼠标移动事件：动态调整矩形大小
    private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentRectangle == null) return;

        // 获取当前鼠标位置
        var currentPoint = e.GetPosition(_mapCanvas);

        // 计算矩形的宽度和高度
        double width = currentPoint.X - _startPoint.X;
        double height = currentPoint.Y - _startPoint.Y;

        // 如果宽度或高度为负，调整起始点和宽高
        if (width < 0)
        {
            Canvas.SetLeft(_currentRectangle, currentPoint.X);
            width = -width;
        }
        if (height < 0)
        {
            Canvas.SetTop(_currentRectangle, currentPoint.Y);
            height = -height;
        }

        // 设置矩形的宽度和高度
        _currentRectangle.Width = width;
        _currentRectangle.Height = height;
    }

    // 鼠标左键松开事件：完成绘制矩形
    private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentRectangle != null)
        {
            // 获取矩形的绝对位置和大小
            double left = Canvas.GetLeft(_currentRectangle);
            double top = Canvas.GetTop(_currentRectangle);
            double width = _currentRectangle.Width;
            double height = _currentRectangle.Height;

            // 计算矩形的相对位置和大小
            var relativeData = new RectangleData
            {
                RelativeX = left / _mapCanvas.ActualWidth,
                RelativeY = top / _mapCanvas.ActualHeight,
                RelativeWidth = width / _mapCanvas.ActualWidth,
                RelativeHeight = height / _mapCanvas.ActualHeight,
                Rectangle = _currentRectangle
            };

            // 保存矩形数据
            _rectangles.Add(relativeData);

            FileHelper.Save(relativeData);

            var lb = AddLabel(_currentRectangle, $"{_text}");
            relativeData.Label = lb;

            // 清空当前矩形
            _currentRectangle = null;
        }
    }
    #endregion

    // MapCanvas 大小发生变化时，更新所有矩形的位置和大小
    private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var mapWidth = _mapCanvas.ActualWidth;
        var mapHeight = _mapCanvas.ActualHeight;
        foreach (var rectData in _rectangles)
        {
            // 根据相对位置和大小计算新的绝对位置和大小
            double newLeft = rectData.RelativeX * mapWidth;
            double newTop = rectData.RelativeY * mapHeight;
            double newWidth = rectData.RelativeWidth * mapWidth;
            double newHeight = rectData.RelativeHeight * mapHeight;

            // 更新矩形的位置和大小
            Canvas.SetLeft(rectData.Rectangle, newLeft);
            Canvas.SetTop(rectData.Rectangle, newTop);
            rectData.Rectangle.Width = newWidth;
            rectData.Rectangle.Height = newHeight;

            var w = rectData.Rectangle.Width / 2;
            var dh = rectData.Rectangle.Height / 2;
            if (rectData.Label != null)
            {
                Canvas.SetLeft(rectData.Label, Canvas.GetLeft(rectData.Rectangle) + w - rectData.Label.ActualWidth / 2);
                Canvas.SetTop(rectData.Label, Canvas.GetTop(rectData.Rectangle) + dh - 10);
            }
        }
    }

    private TextBlock AddLabel(Rectangle rectangle, string label)
    {
        var textBlock = new TextBlock
        {
            Text = label,
            Foreground = _borderColorBrush,
            Background = Brushes.Gray,
            FontSize = 18,
            FontWeight = FontWeight.FromOpenTypeWeight(4),
        };
        var w = rectangle.Width / 2;
        var dh = rectangle.Height / 2;
        Canvas.SetLeft(textBlock, Canvas.GetLeft(rectangle) + w - textBlock.ActualWidth / 2);
        Canvas.SetTop(textBlock, Canvas.GetTop(rectangle) + dh - 10);
        _mapCanvas.Children.Add(textBlock);
        return textBlock;
    }

    public void SetColor(byte r, byte b, byte g, string text)
    {
        _text = text;
        _borderColorBrush = new SolidColorBrush(Color.FromRgb(r, g, b));
        _solidColorBrush = new SolidColorBrush(Color.FromArgb(0, r, g, b));
    }
    public void SetMarker(int index, string text, Color color)
    {
        var rect = _rectangles.Find(x => x.Index == index);
        if (rect == null) return;
        _borderColorBrush = new SolidColorBrush(color);
        var tb = AddLabel(rect.Rectangle, text);
        rect.Label = tb;
    }
    public void AddRect(RectangleData rectangleData)
    {
        var rect = new Rectangle
        {
            Stroke = _borderColorBrush, // 边框颜色
            StrokeThickness = 2, // 边框宽度
            Fill = Brushes.LightGray, // _solidColorBrush,// new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)), // 半透明填充颜色
            Opacity = 0.86,
            Tag = rectangleData,
        };
        double left = _mapCanvas.ActualWidth * rectangleData.RelativeX;
        double top = _mapCanvas.ActualHeight * rectangleData.RelativeY;
        double width = _mapCanvas.ActualWidth * rectangleData.RelativeWidth;
        double height = _mapCanvas.ActualHeight * rectangleData.RelativeHeight;

        rect.Height = height;
        rect.Width = width;
        _mapCanvas.Children.Add(rect);
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, top);

        rectangleData.Rectangle = rect;
        _rectangles.Add(rectangleData);
    }
    public void RemoveAll()
    {
        _rectangles.Clear();
        _mapCanvas.Children.Clear();
    }
}
public class FileHelper
{
    private readonly static string _fileName = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Images", "RectangleData.json");
    #region save & get 
    public static void Save(RectangleData rectangleData)
    {
        var datas = GetFileRectangleDatas();
        rectangleData.Index = datas.Count + 1;
        datas.Add(rectangleData);
        var fileName = _fileName;
        System.IO.File.WriteAllText(fileName, JsonHelper.SerializeObject(datas));
    }
    public static List<RectangleData> GetFileRectangleDatas()
    {
        var fileName = _fileName;
        if (!System.IO.File.Exists(fileName)) return new List<RectangleData>();
        var json = System.IO.File.ReadAllText(fileName, System.Text.UTF8Encoding.UTF8);
        var list = JsonHelper.DeserializeObject<List<RectangleData>>(json);
        if (list == null) return new List<RectangleData>();
        return list;
    }
    #endregion

}
