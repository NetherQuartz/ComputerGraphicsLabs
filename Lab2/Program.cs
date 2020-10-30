#region Директивы using (подключаемые библиотеки) и точка входа приложения

using Device = CGLabPlatform.GDIDevice;
using DeviceArgs = CGLabPlatform.GDIDeviceUpdateArgs;

using System;
using CGLabPlatform;

using CGApplication = AppMain;
using CGLabExtensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SharpGL.Enumerations;

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
        Default: new[] { 0d, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        Increment: 0.01,
        Name: null,
        Decimals: 4)]
    public virtual DMatrix4 TransformationMatrix { get; set; }

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
    
    [DisplayCheckerProperty(true, "Рисовать полигональную сетку")]
    public virtual bool DrawMesh { get; set; }
    
    [DisplayCheckerProperty(false, "Рисовать невидимые полигоны")]
    public virtual bool DrawInvisiblePolygons { get; set; }
    
    [DisplayCheckerProperty(false, "Закрашивать полигоны")]
    public virtual bool DrawColor { get; set; }

    [DisplayNumericProperty(5, 1, "Грани", 3)]
    public virtual int PrismEdges
    {
        get => Get<int>();
        set
        {
            Mesh = MakePrism(value, PrismSize.X, PrismSize.Y);
            Set(value);
        }
    }

    [DisplayNumericProperty(new[] {1d, 1d}, 0.1, "Размер", 0.1)]
    public virtual DVector2 PrismSize
    {
        get => Get<DVector2>();
        set
        {
            Mesh = MakePrism(PrismEdges, value.X, value.Y);
            Set(value);
        }
    }
    
    [DisplayEnumListProperty(
        Projections.Default,
        "Проекция")]
    public virtual Projections Projection { get; set; }
    
    public enum Projections
    {
        [Description("Стандартная")] Default,
        [Description("Сбоку")] Side,
        [Description("Сверху")] Above,
        [Description("Спереди")] Front,
        [Description("Изометрическая")] Isometric
    }
    #endregion

    private List<Polygon> Mesh;

    private DVector3 cameraPosition;
    private DVector2 centerPoint; // центр экрана

    private double cameraDistance = 1.5;

    private double pixelsPerUnit;

    private double fitMultiplier = 1;

    private double initialWindowSize;

    private readonly DMatrix4 invertYMatrix = new DMatrix4(1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

    private Projections prevProjection = Projections.Default;

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        RenderDevice.BufferBackCol = 0x10;
        ValueStorage.Font = new Font("Sergoe UI", 12f);
        ValueStorage.RowHeight = 35;
        VSPanelWidth = 380;
        MainWindow.Size = new Size(1200, 800);

        initialWindowSize = Math.Min(RenderDevice.Height, RenderDevice.Width);

        Mesh = MakePrism(PrismEdges, PrismSize.X, PrismSize.Y);
        cameraPosition = (RenderDevice.Width / 2, RenderDevice.Height / 2, cameraDistance).ToDVector3();

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
        
        if (Projection == Projections.Isometric && prevProjection != Projections.Isometric)
        {
            Rotation = new DVector3(Math.Asin(Math.Sqrt(1d / 3)), -Math.PI / 4, 0);
        }
        prevProjection = Projection;

        centerPoint = (e.Width / 2, 11 * e.Heigh / 20).ToDVector2();
        cameraPosition = centerPoint.ToDVector3(cameraDistance);

        #region Рисование осей
        
        var axisLen = Math.Min(e.Heigh, e.Width) / 2;

        var x_head = new DVector4(axisLen, 0, 0, 1);  // начало оси OX
        var y_head = new DVector4(0, axisLen, 0, 1);  // начало оси OY
        var z_head = new DVector4(0, 0, axisLen, 1);  // начало оси OZ
        
        var x = RotationMatrix(Rotation) * x_head;
        var y = RotationMatrix(Rotation) * y_head;
        var z = RotationMatrix(Rotation) * z_head;

        e.Surface.DrawLine(Color.Red.ToArgb(), centerPoint, (x.X, -x.Y).ToDVector2() + centerPoint);
        e.Surface.DrawLine(Color.LimeGreen.ToArgb(), centerPoint, (y.X, -y.Y).ToDVector2() + centerPoint);
        e.Surface.DrawLine(Color.DodgerBlue.ToArgb(), centerPoint, (z.X, -z.Y).ToDVector2() + centerPoint);
        
        #endregion

        var transformMatrix = RotationMatrix(Rotation) * ShiftMatrix(Shift) * ScaleMatrix(Scale) * ProjectionMatrix();
        var viewportMatrix = ShiftMatrix(centerPoint.ToDVector3(0)) * invertYMatrix *
                             ScaleMatrix(fitMultiplier * DVector3.One * 100);
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

            if (DrawColor)
            {
                e.Surface.DrawTriangle(polygon.Color, a, b, c);
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
                var normalEnd = 100 * (normal.X, normal.Y).ToDVector2() + normalStart;

                e.Surface.DrawLine(
                    Color.Coral.ToArgb(),
                    normalStart,
                    normalEnd);
                e.Surface.FillRectangle(Color.Aqua.ToArgb(), (int)normalEnd.X, (int)normalEnd.Y, 10, 10);
            }
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

    private DMatrix4 PerspectiveMatrix(DVector3 distortion)
    {
        var perspectiveMatrix = DMatrix4.Identity;
        perspectiveMatrix.M41 = -1f / distortion.X;
        perspectiveMatrix.M42 = -1f / distortion.Y;
        perspectiveMatrix.M43 = -1f / distortion.Z;
        return perspectiveMatrix;
    }
    
    private DMatrix4 ProjectionMatrix()
    {
        switch (Projection)
        {
            case Projections.Front:
                var frontMat = DMatrix4.Identity;
                frontMat.M33 = 0;
                return frontMat;
            case Projections.Above:
                var aboveMat = DMatrix4.Identity;
                aboveMat.M22 = 0;
                return aboveMat;
            case Projections.Side:
                var sideMat = DMatrix4.Identity;
                sideMat.M11 = 0;
                return sideMat;
            case Projections.Default:
                var centralMat = DMatrix4.Identity;
                return centralMat;
            case Projections.Isometric:
                return DMatrix4.Identity;
        }
        return DMatrix4.Identity;
    }

    private static List<Polygon> MakePrism(int edges, double radius, double height)
    {
        if (edges <= 0 || radius <= 0 || height <= 0) return null;

        var prismBasePoints = new List<DVector2>();
        for (int i = 0; i < edges; ++i)
        {
            var phi = Math.PI * 2 / edges * i;
            prismBasePoints.Add(new DVector2(Math.Cos(phi), Math.Sin(phi)) * radius);
        }
        
        var mesh = new List<Polygon>();
        
        for (int i = 0; i < edges; i++)
        {
            mesh.Add(new Polygon(
                DVector2.Zero.ToDVector4(height / 2, 1),
                prismBasePoints[(i + 1) % edges].ToDVector4(height / 2, 1),
                prismBasePoints[i].ToDVector4(height / 2, 1)));
        }
        
        for (int i = 0; i < edges; i++)
        {
            mesh.Add(new Polygon(
                DVector2.Zero.ToDVector4(-height / 2, 1),
                prismBasePoints[i].ToDVector4(-height / 2, 1),
                prismBasePoints[(i + 1) % edges].ToDVector4(-height / 2, 1)));
        }

        for (int i = 0; i < edges; ++i)
        {
            mesh.Add(new Polygon(
                prismBasePoints[i].ToDVector4(height / 2, 1),
                prismBasePoints[(i + 1) % edges].ToDVector4(-height / 2, 1),
                prismBasePoints[i].ToDVector4(-height / 2, 1)));
        }
        
        for (int i = 0; i < edges; ++i)
        {
            mesh.Add(new Polygon(
                prismBasePoints[i].ToDVector4(height / 2, 1),
                prismBasePoints[(i + 1) % edges].ToDVector4(height / 2, 1),
                prismBasePoints[(i + 1) % edges].ToDVector4(-height / 2, 1)));
        }

        return mesh;
    }
}

struct Polygon
{
    public readonly DVector4 P1, P2, P3;
    public readonly DVector4 Normal;
    public int Color;

    // вершины по часовой стрелке
    public Polygon(DVector4 p1, DVector4 p2, DVector4 p3, int color = 0xAEAEAE)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        Color = color;
        Normal = DVector3.CrossProduct(P3 - P1, P2 - P1)
            .Normalized();
    }

    public DVector4 Center => (P1 + P2 + P3) / 3;
}