#region Директивы using (подключаемые библиотеки) и точка входа приложения

using Device = CGLabPlatform.GDIDevice;
using DeviceArgs = CGLabPlatform.GDIDeviceUpdateArgs;

using System;
using Lab3;
using CGLabPlatform;

using CGApplication = AppMain;
using CGLabExtensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

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

    [DisplayNumericProperty(
        new []{1d, 1d, 1d},
        0.01, "Масштаб",
        0.1,
        500)]
    public virtual DVector3 Scale { get; set; }

    [DisplayNumericProperty(
        new []{ -2d, 0, 0.8 },
        0.01,
        "Поворот XYZ",
        -2 * Math.PI,
        2 * Math.PI)]
    public virtual DVector3 Rotation
    {
        get => Get<DVector3>();
        set
        {
            var newValue = value;
            for (int i = 0; i < 3; ++i)
            {
                while (newValue[i] >= 2 * Math.PI)
                {
                    newValue[i] -= 2 * Math.PI;
                }
                while (newValue[i] <= -2 * Math.PI)
                {
                    newValue[i] += 2 * Math.PI;
                }
            }
            Set(newValue);
        }
    }

    [DisplayNumericProperty(
        new []{ 0d, 0d, 0d },
        0.1,
        "Сдвиг",
        -1000,
        1000)]
    public virtual DVector3 Shift { get; set; }

    [DisplayCheckerProperty(false, "Рисовать нормали")]
    public virtual bool DrawNormals { get; set; }
    
    [DisplayCheckerProperty(false, "Рисовать полигональную сетку")]
    public virtual bool DrawMesh { get; set; }
    
    [DisplayCheckerProperty(false, "Рисовать невидимые полигоны")]
    public virtual bool DrawInvisiblePolygons { get; set; }
    
    [DisplayCheckerProperty(true, "Закрашивать полигоны")]
    public virtual bool DrawColor { get; set; }

    [DisplayNumericProperty(new[] {1d, 1.5d}, 0.1, "Форма", 0.1)]
    public virtual DVector2 PrismSize
    {
        get => Get<DVector2>();
        set
        {
            Set(value);
            Mesh = MakePrism();
        }
    }
    
    [DisplayNumericProperty(new[] {0.2d, 0d}, 0.1, "Сдвиг оснований")]
    public virtual DVector2 BaseShift
    {
        get => Get<DVector2>();
        set
        {
            Set(value);
            Mesh = MakePrism();
        }
    }
    
    [DisplayNumericProperty(new[] {40d, 20d, 15d}, 1, "Аппроксимация", 3)]
    public virtual DVector3 Approximation
    {
        get => Get<DVector3>();
        set
        {
            Set(value);
            Mesh = MakePrism();
        }
    }

    [DisplayEnumListProperty(Shadings.Flat, "Метод затенения")]
    public virtual Shadings Shading { get; set; }
    
    public enum Shadings
    {
        [Description("Плоская")] Flat,
        [Description("Гуро")] Gouraud,
        [Description("Закраска")] ConstColor,
    }
    
    [DisplayNumericProperty(new[]{0.6d, 0.8, 0.9}, 0.1d, 1, "Цвет материала", 0, 1)]
    public virtual DVector3 MaterialColor { get; set; }
    
    [DisplayNumericProperty(new[]{0.1d, 0.1, 0.2}, 0.1d, 1, "Ka (вос. фон. осв.)", 0, 1)]
    public virtual DVector3 Ka { get; set; }
    
    [DisplayNumericProperty(new[]{1d, 1, 0.5}, 0.1d, 1, "Kd (вос. расс. осв.)", 0, 1)]
    public virtual DVector3 Kd { get; set; }
    
    [DisplayNumericProperty(new[]{0.2d, 0.2, 1}, 0.1d, 1, "Ks (зеркальность)", 0, 1)]
    public virtual DVector3 Ks { get; set; }
    
    [DisplayNumericProperty(new[]{1d, 1, 1}, 0.1d, 1, "ia (инт. фон. осв.)", 0, 1)]
    public virtual DVector3 Ia { get; set; }
    
    [DisplayNumericProperty(new[]{1d, 0.5, 0}, 0.1d, 1, "il (инт. источ-ка)", 0, 1)]
    public virtual DVector3 Il { get; set; }

    [DisplayNumericProperty(4d, 1, 0, "p (распр. света)", 0)]
    public virtual double P { get; set; }
    
    [DisplayNumericProperty(new []{.4d, .4}, 0.1d, 1, "K1, K2", 0)]
    public virtual DVector2 K { get; set; }
    
    [DisplayCheckerProperty(true, "Рисовать источник света")]
    public virtual bool DrawLightSource { get; set; }
    
    [DisplayNumericProperty(new[]{1d, 1, 1}, 0.1, 1, "Позиция источника")]
    public virtual DVector3 LightPos { get; set; }

    #endregion

    private DMatrix4 TransformationMatrix { get; set; }
    
    private List<Polygon> Mesh;
    private Dictionary<DVector4, DVector4> VerticesNormals;
    
    private DVector2 centerPoint; // центр экрана

    private double cameraDistance = 1.5;

    private double pixelsPerUnit;

    private double fitMultiplier = 1;

    private double initialSizeMultiplier;

    private double initialWindowSize;

    private double axisLen = 80;

    private readonly DMatrix4 invertYMatrix = new DMatrix4(1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        RenderDevice.BufferBackCol = 0x10;
        ValueStorage.Font = new Font("Sergoe UI", 12f);
        ValueStorage.RowHeight = 35;
        VSPanelWidth = 380;
        MainWindow.Size = new Size(1200, 800);

        initialWindowSize = Math.Min(RenderDevice.Height, RenderDevice.Width);

        initialSizeMultiplier = Math.Min(RenderDevice.Width, RenderDevice.Height) * 0.3;

        Mesh = MakePrism();

        TransformationMatrix = DMatrix4.Identity;

        // изменение масштаба колёсиком мыши
        RenderDevice.MouseWheel += (_, e) => Scale += e.Delta * 0.001;

        RenderDevice.MouseMoveWithLeftBtnDown += (_, e) =>
        {
            var b = new DVector2(cameraDistance * pixelsPerUnit,
                0); // вектор из центра картинки в место, где сейчас курсор
            var c = new DVector2(cameraDistance * pixelsPerUnit,
                -e.MovDeltaY); // вектор из центра картинки в место, где курсор был прошлый раз

            var cos = c.DotProduct(b) / (b.GetLength() * c.GetLength()); // косинус угла поворота
            var sin = c.CrossProduct(b) / (b.GetLength() * c.GetLength()); // синус угла поворота

            var angleX = Math.Atan2(sin, cos); // вычисление угла поворота по синусу и косинусу

            b = new DVector2(cameraDistance * pixelsPerUnit, 0);
            c = new DVector2(cameraDistance * pixelsPerUnit, -e.MovDeltaX);

            cos = c.DotProduct(b) / (b.GetLength() * c.GetLength()); // косинус угла поворота
            sin = c.CrossProduct(b) / (b.GetLength() * c.GetLength()); // синус угла поворота

            var angleZ = Math.Atan2(sin, cos); // вычисление угла поворота по синусу и косинусу

            var sign = (RotationMatrix(Rotation) * DVector4.UnitZ).DotProduct(DVector4.UnitY);

            Rotation = new DVector3(Rotation.X - angleX, 0, Rotation.Z - sign * angleZ);
        };

        RenderDevice.MouseMoveWithRightBtnDown += (_, e) =>
        {
            var mouseShift = RotationMatrix(Rotation).Invert() * (e.MovDeltaX, -e.MovDeltaY, 0, 0).ToDVector4();
            Shift += mouseShift.ToDVector3() / pixelsPerUnit;
        };

        RenderDevice.SizeChanged += (_, e) =>
        {
            fitMultiplier = Math.Min(RenderDevice.Height, RenderDevice.Width) / initialWindowSize;
        };
    }

    protected override void OnDeviceUpdate(object s, DeviceArgs e)
    {
        if (Mesh == null) return;

        centerPoint = (e.Width / 2, 11 * e.Heigh / 20).ToDVector2();

        #region Рисование осей

        var x_head = new DVector4(axisLen, 0, 0, 1);  // начало оси OX
        var y_head = new DVector4(0, axisLen, 0, 1);  // начало оси OY
        var z_head = new DVector4(0, 0, axisLen, 1);  // начало оси OZ
        
        var x = RotationMatrix(Rotation) * x_head;
        var y = RotationMatrix(Rotation) * y_head;
        var z = RotationMatrix(Rotation) * z_head;
        
        var axisCornerCenter = new DVector2(RenderDevice.Width - axisLen - 20, axisLen + 20);

        e.Surface.DrawLine(Color.Red.ToArgb(), axisCornerCenter, (x.X, -x.Y).ToDVector2() + axisCornerCenter);
        e.Surface.DrawLine(Color.LimeGreen.ToArgb(), axisCornerCenter, (y.X, -y.Y).ToDVector2() + axisCornerCenter);
        e.Surface.DrawLine(Color.DodgerBlue.ToArgb(), axisCornerCenter, (z.X, -z.Y).ToDVector2() + axisCornerCenter);
        
        #endregion

        var transformMatrix = RotationMatrix(Rotation) * ShiftMatrix(Shift) * ScaleMatrix(Scale) * ProjectionMatrix();
        var viewportMatrix = ShiftMatrix(centerPoint.ToDVector3(0)) * invertYMatrix *
                             ScaleMatrix(fitMultiplier * DVector3.One * initialSizeMultiplier);
        var transformationMatrix = viewportMatrix * transformMatrix;
        if (!DMatrix4.ApproxEqual(transformationMatrix, TransformationMatrix, 0.0001))
        {
            TransformationMatrix = transformationMatrix;
        }

        pixelsPerUnit = (ScaleMatrix(fitMultiplier * DVector3.One * 100) * ScaleMatrix(Scale) * DVector4.UnitX)
            .GetLength();

        foreach (var polygon in Mesh)
        {
            var p1 = TransformationMatrix * polygon.P1;
            var p2 = TransformationMatrix * polygon.P2;
            var p3 = TransformationMatrix * polygon.P3;

            var normal = DMatrix3.NormalVecTransf(transformMatrix) * polygon.Normal;

            if (!DrawInvisiblePolygons && normal.ToDVector3().DotProduct(DVector3.UnitZ) >= 0)
            {
                continue;
            }

            var a = (p1.X / p1.W, p1.Y / p1.W).ToDVector2();
            var b = (p2.X / p2.W, p2.Y / p2.W).ToDVector2();
            var c = (p3.X / p3.W, p3.Y / p3.W).ToDVector2();

            var (red, green, blue) = MaterialColor * 255;
            var watcher = (TransformationMatrix.Invert() * (0, 0, 1, 1).ToDVector4()).ToDVector3();
            Color color;
            DVector4 L;
            DVector3 R;
            double d, cos, prod;
            if (DrawColor)
                switch (Shading)
                {
                    case Shadings.ConstColor:
                        color = Color.FromArgb((int) red, (int) green, (int) blue);
                        e.Surface.DrawTriangle(color.ToArgb(), a, b, c);
                        break;
                    case Shadings.Flat:
                        var i = DVector3.Multiply(Ia, Ka);
                        d = (LightPos - polygon.Center.ToDVector3()).GetLength();
                        L = (LightPos - polygon.Center.ToDVector3()).ToDVector4(0).Normalized();
                        R = DVector3.Reflect(L.ToDVector3(), polygon.Normal.ToDVector3());
                        cos = watcher.DotProduct(R) / (watcher.GetLength() * R.GetLength());
                        prod = DVector3.DotProduct(L.ToDVector3(), polygon.Normal.ToDVector3());
                        if (prod > 0)
                        {
                            i += DVector3.Multiply(Il, (Kd * prod + Ks * Math.Pow(cos, P)) / (d * K.X + K.Y));                        
                        }
                        color = Color.FromArgb(ColorMul(red, i.X), ColorMul(green, i.Y), ColorMul(blue, i.Z));
                        e.Surface.DrawTriangle(color.ToArgb(), a, b, c);
                        break;
                    case Shadings.Gouraud:
                        d = (LightPos - polygon.P1.ToDVector3()).GetLength();
                        L = (LightPos - polygon.P1.ToDVector3()).ToDVector4(0).Normalized();
                        R = DVector3.Reflect(L.ToDVector3(), VerticesNormals[polygon.P1].ToDVector3());
                        cos = watcher.DotProduct(R) / (watcher.GetLength() * R.GetLength());
                        prod = DVector3.DotProduct(L.ToDVector3(), VerticesNormals[polygon.P1].ToDVector3());
                        var i1 = DVector3.Multiply(Ia, Ka);
                        if (prod > 0)
                        {
                            i1 += DVector3.Multiply(Il, (Kd * prod + Ks * Math.Pow(cos, P)) / (d * K.X + K.Y));
                        }
                    
                        d = (LightPos - polygon.P2.ToDVector3()).GetLength();
                        L = (LightPos - polygon.P2.ToDVector3()).ToDVector4(0).Normalized();
                        R = DVector3.Reflect(L.ToDVector3(), VerticesNormals[polygon.P2].ToDVector3());
                        cos = watcher.DotProduct(R) / (watcher.GetLength() * R.GetLength());
                        prod = DVector3.DotProduct(L.ToDVector3(), VerticesNormals[polygon.P2].ToDVector3());
                        var i2 = DVector3.Multiply(Ia, Ka);
                        if (prod > 0)
                        {
                            i2 += DVector3.Multiply(Il, (Kd * prod + Ks * Math.Pow(cos, P)) / (d * K.X + K.Y));
                        }
                    
                        d = (LightPos - polygon.P3.ToDVector3()).GetLength();
                        L = (LightPos - polygon.P3.ToDVector3()).ToDVector4(0).Normalized();
                        R = DVector3.Reflect(L.ToDVector3(), VerticesNormals[polygon.P3].ToDVector3());
                        cos = watcher.DotProduct(R) / (watcher.GetLength() * R.GetLength());
                        prod = DVector3.DotProduct(L.ToDVector3(), VerticesNormals[polygon.P3].ToDVector3());
                        var i3 = DVector3.Multiply(Ia, Ka);
                        if (prod > 0)
                        {
                            i3 += DVector3.Multiply(Il, (Kd * prod + Ks * Math.Pow(cos, P)) / (d * K.X + K.Y));
                        }
                    
                        var c1 = Color.FromArgb(ColorMul(red, i1.X), ColorMul(green, i1.Y), ColorMul(blue, i1.Z));
                        var c2 = Color.FromArgb(ColorMul(red, i2.X), ColorMul(green, i2.Y), ColorMul(blue, i2.Z));
                        var c3 = Color.FromArgb(ColorMul(red, i3.X), ColorMul(green, i3.Y), ColorMul(blue, i3.Z));
                    
                        e.Surface.DrawTriangle(c1.ToArgb(), a.X, a.Y, c2.ToArgb(), b.X, b.Y, c3.ToArgb(), c.X, c.Y);
                    
                        break;
                }
            
            if (DrawMesh)
            {
                e.Surface.DrawLine(Color.White.ToArgb(), a, b);
                e.Surface.DrawLine(Color.White.ToArgb(), b, c);
                e.Surface.DrawLine(Color.White.ToArgb(), c, a);
            }
            
            normal = TransformationMatrix * polygon.Normal;
            if (DrawNormals && normal.GetLength() > 0)
            {
                normal = normal.Normalized();
                var m = TransformationMatrix * polygon.Center;
                var normalStart = (m.X, m.Y).ToDVector2();
                var normalEnd = 50 * (normal.X, normal.Y).ToDVector2() + normalStart;
                
                e.Graphics.DrawLine(Pens.Coral, normalStart.X, normalStart.Y, normalEnd.X, normalEnd.Y);
                e.Graphics.FillEllipse(Brushes.Coral, (int)normalStart.X - 3, (int)normalStart.Y - 3, 6, 6);
            }
        }
        
        if (DrawLightSource)
        {
            var lightPos = (TransformationMatrix * LightPos.ToDVector4(1)).ToDVector3();
            var (red, green, blue) = Il * 255;
            var c = Color.FromArgb((int) red, (int) green, (int) blue);
            var brush = new SolidBrush(c);
            e.Graphics.FillEllipse(brush, (float) (lightPos.X - 20), (float) (lightPos.Y - 20), 40f, 40f);
        }
        
        e.Graphics.DrawString("X", new Font("Sergoe UI", 10f), Brushes.Red, 10, 10);
        e.Graphics.DrawString("Y", new Font("Sergoe UI", 10f), Brushes.LimeGreen, 25, 10);
        e.Graphics.DrawString("Z", new Font("Sergoe UI", 10f), Brushes.DodgerBlue, 40, 10);
    }

    private DMatrix4 RotationMatrix(DVector3 rotation)
    {
        var rotX = DMatrix4.Identity;
        rotX.M22 = rotX.M33 = Math.Cos(rotation.X);
        rotX.M32 = Math.Sin(rotation.X);
        rotX.M23 = -rotX.M32;

        var rotY = DMatrix4.Identity;
        rotY.M11 = rotY.M33 = Math.Cos(rotation.Y);
        rotY.M13 = Math.Sin(rotation.Y);
        rotY.M31 = -rotY.M13;

        var rotZ = DMatrix4.Identity;
        rotZ.M11 = rotZ.M22 = Math.Cos(rotation.Z);
        rotZ.M21 = Math.Sin(rotation.Z);
        rotZ.M12 = -rotZ.M21;

        return rotX * rotY * rotZ;
    }

    private DMatrix4 ScaleMatrix(DVector3 scale)
    {
        var scaleMatrix = DMatrix4.Identity;
        scaleMatrix.M11 = scale.X;
        scaleMatrix.M22 = scale.Y;
        scaleMatrix.M33 = scale.Z;
        return scaleMatrix;
    }

    private DMatrix4 ShiftMatrix(DVector3 shift)
    {
        var shiftMatrix = DMatrix4.Identity;
        shiftMatrix.M14 = shift.X;
        shiftMatrix.M24 = shift.Y;
        shiftMatrix.M34 = shift.Z;
        return shiftMatrix;
    }

    private DMatrix4 ProjectionMatrix()
    {
        return DMatrix4.Identity;
    }

    // генерация меша призмы
    private List<Polygon> MakePrism()
    {
        var edges = (int)Approximation.X;
        var radius = PrismSize.X;
        var height = PrismSize.Y;
        var shift = BaseShift.ToDVector4(0, 0);

        if (edges <= 0 || radius <= 0 || height <= 0) return null;

        var layersNum = (int)Approximation.Y;
        var circlesNum = (int)Approximation.Z;

        // точки основания без привязки к верху или низу
        var prismBasePoints = new List<DVector2>();
        for (int i = 0; i < edges; ++i)
        {
            var phi = Math.PI * 2 / edges * i;
            prismBasePoints.Add(new DVector2(Math.Cos(phi), Math.Sin(phi)) * radius);
        }
        
        var mesh = new List<Polygon>();

        // верхнее основание
        for (int i = 0; i < edges; i++)
        {
            // серединка
            mesh.Add(new Polygon(
                DVector2.Zero.ToDVector4(height / 2, 1) + shift,
                (prismBasePoints[(i + 1) % edges] / circlesNum).ToDVector4(height / 2, 1) + shift,
                (prismBasePoints[i] / circlesNum).ToDVector4(height / 2, 1) + shift));
            
            // область вокруг серединки
            for (int c = 1; c < circlesNum; ++c)
            {
                mesh.Add(new Polygon(
                    (prismBasePoints[i] / circlesNum * c).ToDVector4(height / 2, 1) + shift,
                    (prismBasePoints[(i + 1) % edges] / circlesNum * c).ToDVector4(height / 2, 1) + shift,
                    (prismBasePoints[(i + 1) % edges] / circlesNum * (c + 1)).ToDVector4(height / 2, 1) + shift));
                mesh.Add(new Polygon(
                    (prismBasePoints[i] / circlesNum * c).ToDVector4(height / 2, 1) + shift,
                    (prismBasePoints[(i + 1) % edges] / circlesNum * (c + 1)).ToDVector4(height / 2, 1) + shift,
                    (prismBasePoints[i] / circlesNum * (c + 1)).ToDVector4(height / 2, 1) + shift));
            }
        }
        
        // нижнее основание
        for (int i = 0; i < edges; i++)
        {
            // серединка
            mesh.Add(new Polygon(
                DVector2.Zero.ToDVector4(-height / 2, 1) - shift,
                (prismBasePoints[i] / circlesNum).ToDVector4(-height / 2, 1) - shift,
                (prismBasePoints[(i + 1) % edges] / circlesNum).ToDVector4(-height / 2, 1) - shift));
            
            // область вокруг серединки
            for (int c = 1; c < circlesNum; ++c)
            {
                mesh.Add(new Polygon(
                    (prismBasePoints[i] / circlesNum * c).ToDVector4(-height / 2, 1) - shift,
                    (prismBasePoints[(i + 1) % edges] / circlesNum * (c + 1)).ToDVector4(-height / 2, 1) - shift,
                    (prismBasePoints[(i + 1) % edges] / circlesNum * c).ToDVector4(-height / 2, 1) - shift));
                mesh.Add(new Polygon(
                    (prismBasePoints[i] / circlesNum * c).ToDVector4(-height / 2, 1) - shift,
                    (prismBasePoints[i] / circlesNum * (c + 1)).ToDVector4(-height / 2, 1) - shift,
                    (prismBasePoints[(i + 1) % edges] / circlesNum * (c + 1)).ToDVector4(-height / 2, 1) - shift));
            }
        }
        
        var heightStep = height / layersNum; // шаг изменения высоты
        var shiftStep = shift * 2 / layersNum; // шаг изменения сдвига
        
        // полигоны боковых граней, торчащие вершиной вверх
        for (int i = 0; i < edges; ++i)
        {
            var s = -shiftStep * layersNum / 2;
            for (double h = 0; h < height - heightStep / 4; h += heightStep)
            {
                mesh.Add(new Polygon(
                    prismBasePoints[i].ToDVector4(height / 2 - h, 1) - s,
                    prismBasePoints[(i + 1) % edges].ToDVector4(height / 2 - h - heightStep, 1) - s - shiftStep,
                    prismBasePoints[i].ToDVector4(height / 2 - h - heightStep, 1) - s - shiftStep));
                s += shiftStep;
            }
        }
        
        // полигоны боковых граней, торчащие вершиной вниз
        for (int i = 0; i < edges; ++i)
        {
            var s = -shiftStep * layersNum / 2;
            for (double h = 0; h < height - heightStep / 4; h += heightStep)
            {
                mesh.Add(new Polygon(
                    prismBasePoints[i].ToDVector4(height / 2 - h, 1) - s,
                    prismBasePoints[(i + 1) % edges].ToDVector4(height / 2 - h, 1) - s,
                    prismBasePoints[(i + 1) % edges].ToDVector4(height / 2 - h - heightStep, 1) - s - shiftStep));
                s += shiftStep;
            }
        }
        
        var vertices = new Dictionary<DVector4, List<Polygon>>();
        foreach (var polygon in mesh)
        {
            if (!vertices.ContainsKey(polygon.P1))
            {
                vertices.Add(polygon.P1, new List<Polygon>());
            }
            vertices[polygon.P1].Add(polygon);
            
            if (!vertices.ContainsKey(polygon.P2))
            {
                vertices.Add(polygon.P2, new List<Polygon>());
            }
            vertices[polygon.P2].Add(polygon);
            
            if (!vertices.ContainsKey(polygon.P3))
            {
                vertices.Add(polygon.P3, new List<Polygon>());
            }
            vertices[polygon.P3].Add(polygon);
        }

        lock (RenderDevice.LockObj)
        {
            VerticesNormals = new Dictionary<DVector4, DVector4>();
            foreach (var vertex in vertices)
            {
                var normal = DVector4.Zero;
                foreach (var polygon in vertex.Value)
                {
                    normal += polygon.Normal;
                }
                normal /= vertex.Value.Count;
                VerticesNormals[vertex.Key] = normal;
            }
        }

        return mesh;
    }

    int ColorMul(double col, double mul)
    {
        var ans = (int)(col * mul);
        return ans switch
        {
            var x when x > 255 => 255,
            var x when x < 0 => 0,
            _ => ans
        };
    }
}