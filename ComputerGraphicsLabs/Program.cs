#region Директивы using (подключаемые библиотеки) и точка входа приложения

using Device = CGLabPlatform.GDIDevice;
using DeviceArgs = CGLabPlatform.GDIDeviceUpdateArgs;

using System;
using CGLabPlatform;

using CGApplication = AppMain;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

public abstract class AppMain : CGApplicationTemplate<Application, Device, DeviceArgs>
{
    [STAThread]
    static void Main()
    {
        RunApplication();
    }
}
#endregion

public abstract class Application : CGApplication
{
    #region элементы GUI
    [DisplayNumericProperty(0, 0.1, "a", 0.1, 100)]
    public virtual double a
    {
        get => Get<double>();
        set
        {
            Set(value);
            CalculatePoints();
        }
    }

    [DisplayNumericProperty(10, 0.1, "B", 0, 100)]
    public virtual double B
    {
        get => Get<double>();
        set
        {
            Set(value);
            CalculatePoints();
        }
    }

    [DisplayNumericProperty(0.1, 0.1, "Шаг", 0.1, 10)]
    public virtual double step
    {
        get => Get<double>();
        set
        {
            Set(value);
            CalculatePoints();
        }
    }

    [DisplayNumericProperty(1, 0.01, "Зум", 1, 500)]
    public virtual double zoom
    {
        get => Get<double>();
        set
        {
            Set(value);
            CalculateBounds();
        }
    }

    [DisplayNumericProperty(0, 0.01, "Поворот", -2 * Math.PI, 2 * Math.PI)]
    public virtual double angle
    {
        get => Get<double>();
        set
        {
            var new_value = value;
            while (new_value >= 2 * Math.PI)
            {
                new_value -= 2 * Math.PI;
            }
            while (new_value <= -2 * Math.PI)
            {
                new_value += 2 * Math.PI;
            }
            Set(new_value);
            CalculateBounds();
        }
    }

    [DisplayNumericProperty(new[] { 0d, 0d }, 1, "Центр", -1000, 1000)]
    public virtual DVector2 centerPoint
    {
        get => Get<DVector2>();
        set
        {
            Set(value);
            CalculateBounds();
        }
    }

    [DisplayCheckerProperty(false, "Границы")]
    public virtual bool DrawBounds { get; set; }
    #endregion

    DVector2 prevSize; // нужно в обработчике ихменения размеров рабочей области

    List<DVector2> points; // точки графика

    DVector2? prevLocation = null; // нужно в обработчике движения мышки с зажатой ПКМ

    double axisLen; // длина оси

    // левая, правая, нижняя, верхняя границы картинки
    double left_bound, right_bound, lower_bound, upper_bound;

    bool firstFrame = true; // флаг для вызова метода Init

    // отступ между границей картинки и её реальной границей
    const double Margin = 5;

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        RenderDevice.BufferBackCol = 0xFF; // белый цвет фона

        // изменение масштаба колёсиком мыши
        RenderDevice.MouseWheel += (_, e) => zoom += e.Delta / 100;

        // двигание графика с зажатой ЛКМ
        RenderDevice.MouseMoveWithLeftBtnDown += (_, e) => centerPoint += new DVector2(e.MovDeltaX, e.MovDeltaY);

        // сохранение размеров рабочей области (понадобится при изменении размеров рабочей области)
        prevSize = new DVector2(RenderDevice.Width, RenderDevice.Height);

        points = new List<DVector2>(); // список точек графика
        CalculatePoints();
        CalculateBounds();

        // событие изменения размеров рабочей области
        RenderDevice.SizeChanged += SizeChangedHandler;

        // поворот графика с зажатой ПКМ
        RenderDevice.MouseMoveWithRightBtnDown += (_, e) =>
        {
            // сохранение текущего положения как прошлого, если ПКМ была зажата только что
            if (prevLocation == null)
            {
                prevLocation = new DVector2(e.PressedLocation);
            }
            var b = centerPoint - new DVector2(e.Location); // вектор из центра картинки в место, где сейчас курсор
            var c = centerPoint - (DVector2)prevLocation;   // вектор из центра картинки в место, где курсор был прошлый раз
            
            var cos = c.DotProduct(b) / (b.GetLength() * c.GetLength());   // косинус угла поворота
            var sin = c.CrossProduct(b) / (b.GetLength() * c.GetLength()); // синус угла поворота

            prevLocation = new DVector2(e.Location); // сохранение текущего положения курсора как прошлого

            angle += Math.Atan2(sin, cos); // вычисление угла поворота по синусу и косинусу

            // вычитание или прибавление 2*pi во избежание переполнения переменной
            while (angle > 2 * Math.PI)
            {
                angle -= 2 * Math.PI;
            }
            while (angle < 2 * -Math.PI)
            {
                angle += 2 * Math.PI;
            }
            CalculateBounds();
        };

        // обнуление прошлого положения, если ПКМ отжата
        RenderDevice.MouseUp += (_, e) => prevLocation = null;
    }

    protected override void OnDeviceUpdate(object s, DeviceArgs e)
    {
        if (points.Count == 0) return;
        if (firstFrame) Init(); // инициализация некоторых свойств, если это первый вызов OnDeviceUpdate

        #region Рисование осей

        var x_head = new DVector2(axisLen, 0);  // начало оси OX
        var x_tail = new DVector2(-axisLen, 0); // конец оси OX
        var y_head = new DVector2(0, axisLen);  // начало оси OY
        var y_tail = new DVector2(0, -axisLen); // конец оси OY

        e.Surface.DrawLine(0, ToScreenSpace(x_head), ToScreenSpace(x_tail)); // отрисовка оси OX
        e.Surface.DrawLine(0, ToScreenSpace(y_head), ToScreenSpace(y_tail)); // отрисовка оси OY

        // стрелка оси OY
        e.Surface.DrawTriangle(0,
            ToScreenSpace(y_head),
            ToScreenSpace(y_head + new DVector2(8, -20) / zoom),
            ToScreenSpace(y_head + new DVector2(-8, -20) / zoom));
        
        // стрелка оси OX
        e.Surface.DrawTriangle(0,
            ToScreenSpace(x_head),
            ToScreenSpace(x_head + new DVector2(-20, 8) / zoom),
            ToScreenSpace(x_head + new DVector2(-20, -8) / zoom));

        // подпись оси OY
        var (text_x, text_y) = ToScreenSpace(y_head + new DVector2(10, 0) / zoom);
        e.Graphics.DrawString("Y", new Font("Arial", 10f), Brushes.Black, text_x, text_y);

        // подпись оси OX
        (text_x, text_y) = ToScreenSpace(x_head + new DVector2(-15, -10) / zoom);
        e.Graphics.DrawString("X", new Font("Arial", 10f), Brushes.Black, text_x, text_y);

        // подпись точки O
        (text_x, text_y) = ToScreenSpace(new DVector2(3, 20) / zoom);
        e.Graphics.DrawString("O", new Font("Arial", 10f), Brushes.Black, text_x, text_y);

        #endregion

        #region Штрихи и числа на осях
        for (double x = 0; x < x_head.X - 50 / zoom; x += 50 / zoom)
        {
            e.Surface.DrawLine(0, ToScreenSpace(new DVector2(x, 5 / zoom)), ToScreenSpace(new DVector2(x, -5 / zoom)));
            (text_x, text_y) = ToScreenSpace(new DVector2(x - 5 / zoom, -7 / zoom));
            e.Graphics.DrawString($"{x:F2}", new Font("Arial", 7), Brushes.Black, text_x, text_y);
        }
        for (double x = 0; x > x_tail.X + 10 / zoom; x -= 50 / zoom)
        {
            if (x == 0) continue;
            e.Surface.DrawLine(0, ToScreenSpace(new DVector2(x, 5 / zoom)), ToScreenSpace(new DVector2(x, -5 / zoom)));
            (text_x, text_y) = ToScreenSpace(new DVector2(x - 5 / zoom, -7 / zoom));
            e.Graphics.DrawString($"{x:F2}", new Font("Arial", 7), Brushes.Black, text_x, text_y);
        }
        for (double y = 0; y < y_head.Y - 50 / zoom; y += 50 / zoom)
        {
            if (y == 0) continue;
            e.Surface.DrawLine(0, ToScreenSpace(new DVector2(5 / zoom, y)), ToScreenSpace(new DVector2(-5 / zoom, y)));
            (text_x, text_y) = ToScreenSpace(new DVector2(6 / zoom, y + 6 / zoom));
            e.Graphics.DrawString($"{y:F2}", new Font("Arial", 7), Brushes.Black, text_x, text_y);
        }
        for (double y = 0; y > y_tail.Y + 10 / zoom; y -= 50 / zoom)
        {
            if (y == 0) continue;
            e.Surface.DrawLine(0, ToScreenSpace(new DVector2(5 / zoom, y)), ToScreenSpace(new DVector2(-5 / zoom, y)));
            (text_x, text_y) = ToScreenSpace(new DVector2(6 / zoom, y + 6 / zoom));
            e.Graphics.DrawString($"{y:F2}", new Font("Arial", 7), Brushes.Black, text_x, text_y);
        }
        #endregion

        // отрисовка точек графика
        var previousPoint = points[0];
        for (int i = 1; i < points.Count; ++i)
        {
            var currentPoint = points[i];
            e.Surface.DrawLine(Color.Red.ToArgb(), ToScreenSpace(previousPoint), ToScreenSpace(currentPoint));

            previousPoint = currentPoint;
        }
        
        // номер варианта и задание
        e.Graphics.DrawString("Вариант 8\ny=ax^(3/2), 0<=x<=B", new Font("Arial", 10f), Brushes.Black, 10f, 10f);

        // отрисовка границ, если стоит флаг
        if (DrawBounds)
        {
            e.Surface.DrawLine(Color.Green.ToArgb(), new DVector2(left_bound, lower_bound), new DVector2(right_bound, lower_bound));
            e.Surface.DrawLine(Color.Green.ToArgb(), new DVector2(left_bound, upper_bound), new DVector2(right_bound, upper_bound));
            e.Surface.DrawLine(Color.Green.ToArgb(), new DVector2(right_bound, upper_bound), new DVector2(right_bound, lower_bound));
            e.Surface.DrawLine(Color.Green.ToArgb(), new DVector2(left_bound, upper_bound), new DVector2(left_bound, lower_bound));
        }
    }

    // вычисление точек графика
    void CalculatePoints()
    {
        if (points == null) return;

        lock (RenderDevice.LockObj) // чтобы не изменить список во время обращения к нему из другого потока
        {
            points.Clear();
            for (double x = 0; x <= B; x += step)
            {
                points.Add(new DVector2(x, a * Math.Pow(x, 1.5)));
            }

            // вычисление длины оси и корректировка масштаба, чтобы сохранить исходный размер картинки
            var prevAxisLen = axisLen;
            axisLen = Math.Max(points.Max(p => Math.Abs(p.X)), points.Max(p => Math.Abs(p.Y)));
            if (prevAxisLen > 0)
            {
                zoom /= axisLen / prevAxisLen;
            }
        }

        CalculateBounds();
    }

    // вычисление границ картинки
    void CalculateBounds()
    {
        if (axisLen == 0) return;

        var corners = new List<DVector2>()
        {
            ToScreenSpace(new DVector2(-axisLen, axisLen)),
            ToScreenSpace(new DVector2(axisLen, axisLen)),
            ToScreenSpace(new DVector2(-axisLen, -axisLen)),
            ToScreenSpace(new DVector2(axisLen, -axisLen))
        };

        var max_horizontal = corners.Max(c => Math.Abs(c.X));
        var max_vertical = corners.Max(c => Math.Abs(c.Y));

        left_bound = -(max_horizontal - centerPoint.X) + centerPoint.X - Margin;
        right_bound = max_horizontal + Margin;
        upper_bound = -(max_vertical - centerPoint.Y) + centerPoint.Y - Margin;
        lower_bound = max_vertical + Margin;
    }

    // перевод вектора в экранные координаты
    DVector2 ToScreenSpace(DVector2 vector) => Rotated(Zoomed(vector.Multiply(new DVector2(1, -1)))) + centerPoint;

    // масштабирование
    DVector2 Zoomed(DVector2 vector) => vector * zoom;

    // поворот
    DVector2 Rotated(DVector2 vector)
    {
        return new DVector2(
            vector.X * Math.Cos(angle) - vector.Y * Math.Sin(angle),
            vector.X * Math.Sin(angle) + vector.Y * Math.Cos(angle));
    }

    // инициализация некоторых свойств, вызывается один раз между OnMainWindowLoad и OnDeviceUpdate
    void Init()
    {
        firstFrame = false;

        centerPoint = new DVector2(RenderDevice.Width / 2, RenderDevice.Height / 2);
        zoom = (Math.Min(RenderDevice.Height, RenderDevice.Width) / 2 - Margin) / axisLen;
    }

    // обработчик события изменения размеров рабочей области
    void SizeChangedHandler(object sender, EventArgs e)
    {
        var newSize = new DVector2(RenderDevice.Width, RenderDevice.Height);
        var deltaSize = newSize - prevSize;
        centerPoint += deltaSize / 2;

        double extra_zoom;

        if (newSize.X < newSize.Y)
        {
            extra_zoom = newSize.X / prevSize.X;
        }
        else
        {
            extra_zoom = newSize.Y / prevSize.Y;
        }

        zoom *= extra_zoom;

        CalculateBounds();

        var physicalCenter = new DVector2(RenderDevice.Width / 2, RenderDevice.Height / 2);

        if (left_bound < 0)
        {
            if (centerPoint.X <= physicalCenter.X && physicalCenter.X - centerPoint.X >= -left_bound)
            {
                centerPoint += new DVector2(-left_bound, 0);
            }
        }
        if (right_bound > RenderDevice.Width)
        {
            if (centerPoint.X >= physicalCenter.X && centerPoint.X - physicalCenter.X >= right_bound - RenderDevice.Width)
            {
                centerPoint -= new DVector2(right_bound - RenderDevice.Width, 0);
            }
        }
        if (upper_bound < 0)
        {
            if (centerPoint.Y <= physicalCenter.Y && physicalCenter.Y - centerPoint.Y >= -upper_bound)
            {
                centerPoint += new DVector2(0, -upper_bound);
            }
        }
        if (lower_bound > RenderDevice.Height)
        {
            if (centerPoint.Y >= physicalCenter.Y && centerPoint.Y - physicalCenter.Y >= lower_bound - RenderDevice.Height)
            {
                centerPoint -= new DVector2(0, lower_bound - RenderDevice.Height);
            }
        }

        prevSize = newSize;
    }
}

// расширение класса DVector2
public static class DVectorExtensions
{
    // косое произведение с одним аргументом
    public static double CrossProduct(this DVector2 this_vec, DVector2 vector)
    {
        return this_vec.X* vector.Y - vector.X * this_vec.Y;
    }

    // косое произведение с двумя аргументами
    public static double CrossProduct(this DVector2 _, DVector2 left, DVector2 right)
    {
        return left.X* right.Y - right.X * left.Y;
    }

    // деконструкция в отдельные переменные
    public static void Deconstruct(this DVector2 vector, out double x, out double y)
    {
        x = vector.X;
        y = vector.Y;
    }
}
