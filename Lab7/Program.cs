#region Директивы using (подключаемые библиотеки) и точка входа приложения

using Device = CGLabPlatform.OGLDevice;
using DeviceArgs = CGLabPlatform.OGLDeviceUpdateArgs;
using SharpGL;

using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CGLabExtensions;
using CGLabPlatform;
using Lab7;
using CGApplication = MyApp;

public abstract class AppMain : CGApplication
{
    [STAThread]
    static void Main()
    {
        RunApplication();
    }
}

#endregion

public abstract class MyApp : CGApplicationTemplate<CGApplication, Device, DeviceArgs>
{
    #region Элементы GUI

    [DisplayNumericProperty(new[] {0d, 0d}, .01, 2, "Камера")]
    public virtual DVector2 CameraPosition {
        get => Get<DVector2>();
        set
        {
            if (Set(value))
                UpdateModelViewMatrix();
        }
    }
    
    [DisplayNumericProperty(1d, .01, 2, "Масштаб", .5, 10)]
    public virtual double PicScale {
        get => Get<double>();
        set
        {
            if (Set(value))
                UpdateModelViewMatrix();
        }
    }

    [DisplayNumericProperty(0.5d, 0.1, 1, "τ")]
    public virtual double Tau
    {
        get => Get<double>();
        set
        {
            if (Set(value))
                CalculateSplineAndLoadBuffers();
        }
    }
    
    [DisplayNumericProperty(100d, 1, 0, "Аппроксимация", 1)]
    public virtual double Approximation
    {
        get => Get<double>();
        set
        {
            if (Set(value))
                CalculateSplineAndLoadBuffers();
        }
    }
    
    [DisplayCheckerProperty(true, "Рисовать ломаную")]
    public virtual bool DrawBrokenLine { get; set; }
    
    [DisplayCheckerProperty(true, "Рисовать точки")]
    public virtual bool DrawPoints { get; set; }

    #endregion

    private static uint[] vbo = new uint[4];

    private static List<Vertex> Vertices = new List<Vertex>();
    private static List<uint> Indices = new List<uint>();
    
    private static List<Vertex> SplineVertices = new List<Vertex>();
    private static List<uint> SplineIndices = new List<uint>();

    private DMatrix4 PMatrix, MVMatrix;

    private bool IsDrag;

    private const double DragRadius = .04;
    
    private readonly DVector3 BrokenLineColor = new DVector3(0.57, 0.57, 0.57);
    private readonly DVector3 SelectedVertexColor = new DVector3(1, 0.31, 0.27);
    private readonly DVector3 SplineColor = new DVector3(0.12, 0.31, 1);

    private Vertex dragVertex;

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        ValueStorage.RightColWidth = 50;
        RenderDevice.VSync = 1;
        
        ValueStorage.Font = new Font("Sergoe UI", 12f);
        ValueStorage.RowHeight = 35;
        VSPanelWidth = 340;
        MainWindow.Size = new Size(1200, 800);

        #region  Инициализация OGL и параметров рендера
        RenderDevice.AddScheduleTask((gl, s) =>
        {
            gl.Disable(OpenGL.GL_DEPTH_TEST);
            gl.ClearColor(1, 1, 1, 1);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
        });
        #endregion

        #region Инициализация буфера вершин
        RenderDevice.AddScheduleTask((gl, s) => 
        {
            gl.GenBuffers(4, vbo);
            CalculateSplineAndLoadBuffers();
        }, this);
        #endregion

        #region Уничтожение буфера вершин по завершении работы OGL
        RenderDevice.Closed += (s, e) => // Событие выполняется в контексте потока OGL при завершении работы
        {
            var gl = e.gl;
            gl.UnmapBuffer(OpenGL.GL_ARRAY_BUFFER);
            gl.UnmapBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER);
            gl.DeleteBuffers(4, vbo);
        };
        #endregion

        #region Обновление матрицы проекции при изменении размеров окна и запуске приложения
        RenderDevice.Resized += (s, e) =>
        {
            var gl = e.gl;
            CameraPosition = (CameraPosition.X, CameraPosition.Y).ToDVector2();
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            var pMatrix = Projection(e.Width, e.Height, 0.1, 100);
            gl.LoadMatrix(pMatrix.ToArray(true));
            PMatrix = pMatrix;
        };
        #endregion

        #region Управление мышью

        // изменение масштаба колёсиком мыши
        RenderDevice.MouseWheel += (s, e) =>
        {
            PicScale -= e.Delta * 0.005;
        };
        
        // перемещение камеры
        RenderDevice.MouseMoveWithRightBtnDown += (s, e) =>
        {
            var tMatrix = PMatrix * MVMatrix;
            var d = tMatrix.Invert() * new DVector4(-e.MovDeltaX, e.MovDeltaY, 0, 0); 
            CameraPosition += new DVector2(d.X / RenderDevice.Width, d.Y / RenderDevice.Height) * 4 / PicScale;
        };

        RenderDevice.MouseMoveWithLeftBtnDown += (s, e) =>
        {
            if (IsDrag)
            {
                var tMatrix = PMatrix * MVMatrix;
                var d = tMatrix.Invert() * new DVector4(-e.MovDeltaX, e.MovDeltaY, 0, 0); 
                
                var x = (double) e.X / RenderDevice.Width * 2 - 1;
                var y = (double) e.Y / RenderDevice.Height * -2 + 1;
                (x, y, _) = (tMatrix.Invert() * new DVector4(x * 2, y * 2, 0, 1)).ToDVector3();
                var prevCurPos = new DVector2(x, y);
                
                x = (double) (e.X + e.MovDeltaX) / RenderDevice.Width * 2 - 1;
                y = (double) (e.Y + e.MovDeltaY) / RenderDevice.Height * -2 + 1;
                (x, y, _) = (tMatrix.Invert() * new DVector4(x * 2, y * 2, 0, 1)).ToDVector3();
                var curCurPos = new DVector2(x, y);
                
                var movement = prevCurPos - curCurPos;
                
                var i = Vertices.IndexOf(dragVertex);
                var pos = new DVector2(dragVertex.Vx, dragVertex.Vy);
                var newPos = pos - movement;
                dragVertex.Vx = (float)newPos.X;
                dragVertex.Vy = (float)newPos.Y;
                Vertices[i] = dragVertex;
                CalculateSplineAndLoadBuffers();
            }
        };

        RenderDevice.MouseClick += (s, e) =>
        {
            Trace.WriteLine("Click");
            if (e.Button == MouseButtons.Left && !IsDrag)
            {
                var tMatrix = PMatrix * MVMatrix;
                var x = (double) e.X / RenderDevice.Width * 2 - 1;
                var y = (double) e.Y / RenderDevice.Height * -2 + 1;
                (x, y, _) = (tMatrix.Invert() * new DVector4(x * 2, y * 2, 0, 1)).ToDVector3();
                lock (RenderDevice.LockObj)
                {
                    Vertices.Add(new Vertex((float) x, (float) y, 0, 0, 0, 0, (float) BrokenLineColor.X,
                        (float) BrokenLineColor.Y, (float) BrokenLineColor.Z));
                    Indices.Add(Indices.Count > 0 ? Indices.Last() + 1 : 0);
                    CalculateSplineAndLoadBuffers();
                }
            }
        };

        RenderDevice.MouseDoubleClick += (s, e) =>
        {
            Trace.WriteLine("Double Click");
            if (e.Button == MouseButtons.Right && DrawPoints)
            {
                if (Vertices.Count == 0 || Indices.Count == 0) return;
                var tMatrix = PMatrix * MVMatrix;
                var x = (double) e.X / RenderDevice.Width * 2 - 1;
                var y = (double) e.Y / RenderDevice.Height * -2 + 1;
                var prevDist = new DVector2(x, y).GetLength();
                (x, y, _) = (tMatrix.Invert() * new DVector4(x * 2, y * 2, 0, 1)).ToDVector3();
                var newDist = new DVector2(x, y).GetLength();
                var multiplier = prevDist / newDist;
                lock (RenderDevice.LockObj)
                {
                     var minLen = Vertices.Min(v => Math.Pow(v.Vx - x, 2) + Math.Pow(v.Vy - y, 2));
                     var toDel = Vertices.First(v => Math.Pow(v.Vx - x, 2) + Math.Pow(v.Vy - y, 2) == minLen);
                     var dist = new DVector2(x - toDel.Vx, y - toDel.Vy).GetLength() * multiplier;
                     if (dist > DragRadius) return;
                     var i = Vertices.IndexOf(toDel);
                     var j = Indices.IndexOf((uint) i);
                     Vertices.RemoveAt(i);
                     Indices.RemoveAt(j);
                     for (int t = 0; t < Indices.Count; t++)
                     {
                         if (Indices[t] > i)
                         {
                             Indices[t]--;
                         }
                     }
                     CalculateSplineAndLoadBuffers();
                }
            }
        };

        RenderDevice.MouseDown += (s, e) =>
        {
            Trace.WriteLine("Down");
            if (e.Button == MouseButtons.Left && DrawPoints)
            {
                if (Vertices.Count == 0 || Indices.Count == 0) return;
                var tMatrix = PMatrix * MVMatrix;
                var x = (double) e.X / RenderDevice.Width * 2 - 1;
                var y = (double) e.Y / RenderDevice.Height * -2 + 1;
                var prevDist = new DVector2(x, y).GetLength();
                (x, y, _) = (tMatrix.Invert() * new DVector4(x * 2, y * 2, 0, 1)).ToDVector3();
                var newDist = new DVector2(x, y).GetLength();
                var multiplier = prevDist / newDist;
                lock (RenderDevice.LockObj)
                {
                    var minLen = Vertices.Min(v => Math.Pow(v.Vx - x, 2) + Math.Pow(v.Vy - y, 2));
                    var toDel = Vertices.First(v => Math.Pow(v.Vx - x, 2) + Math.Pow(v.Vy - y, 2) == minLen);
                    var dist = new DVector2(x - toDel.Vx, y - toDel.Vy).GetLength() * multiplier;
                    if (dist > DragRadius) return;
                    var i = Vertices.IndexOf(toDel);
                    var t = Vertices[i];
                    t.R = (float) SelectedVertexColor.X;
                    t.G = (float) SelectedVertexColor.Y;
                    t.B = (float) SelectedVertexColor.Z;
                    Vertices[i] = t;
                    IsDrag = true;
                    dragVertex = t;
                    
                    CalculateSplineAndLoadBuffers();
                }
            }
        };

        RenderDevice.MouseUp += (s, e) =>
        {
            Trace.WriteLine("Up");
            if (e.Button == MouseButtons.Left)
            {
                if (IsDrag)
                {
                    IsDrag = false;
                    var i = Vertices.IndexOf(dragVertex);
                    dragVertex.R = (float) BrokenLineColor.X;
                    dragVertex.G = (float) BrokenLineColor.Y;
                    dragVertex.B = (float) BrokenLineColor.Z;
                    Vertices[i] = dragVertex;
                    dragVertex = new Vertex();
                    CalculateSplineAndLoadBuffers();
                }
            }
        };

        #endregion
    }

    private void UpdateModelViewMatrix()
    {
        #region Обновление объектно-видовой матрицы
        RenderDevice.AddScheduleTask((gl, s) =>
        {
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            var cameraTransform = ShiftMatrix((CameraPosition.X, CameraPosition.Y, 0).ToDVector3());
            var cameraPosition = cameraTransform * new DVector4(0, 0, 1, 1);
            var cameraUpDirection = cameraTransform * new DVector4(0, 1, 0, 0);
            // Мировая матрица (преобразование локальной системы координат в мировую)
            var mMatrix = ScaleMatrix((1 / PicScale, 1 / PicScale, 1).ToDVector3());
            // Видовая матрица (переход из мировой системы координат к системе координат камеры)
            var center = new DVector3(cameraPosition.X, cameraPosition.Y, 0);
            var vMatrix = LookAt(DMatrix4.Identity, cameraPosition.ToDVector3(), center, cameraUpDirection.ToDVector3());
            // матрица ModelView
            var mvMatrix = vMatrix * mMatrix;
            gl.LoadMatrix(mvMatrix.ToArray(true));
            MVMatrix = mvMatrix;
        });
        #endregion
    }
    
    protected override void OnDeviceUpdate(object s, DeviceArgs e)
    {
        var gl = e.gl;
        gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);

        if (Vertices.Count == 0 || Indices.Count == 0) return;

        gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_FILL);

        gl.Disable(OpenGL.GL_CULL_FACE);

        gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
        gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
        gl.EnableClientState(OpenGL.GL_COLOR_ARRAY);
        
        #region Рендеринг сцены методом VBO (Vertex Buffer Object)
        
        var shiftVx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Vx));
        var shiftNx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Nx));
        var shiftR = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.R));
        
        gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[0]);
        gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[1]);

        unsafe
        {
            fixed (Vertex* ptr = Vertices.ToArray())
            {
                gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftVx);
                gl.NormalPointer(OpenGL.GL_FLOAT, sizeof(Vertex), shiftNx);
                gl.ColorPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftR);
                
                gl.PointSize(15);
                gl.LineWidth(1);
                if (DrawBrokenLine)
                {
                    gl.DrawElements(OpenGL.GL_LINE_STRIP, Indices.Count, OpenGL.GL_UNSIGNED_INT, (IntPtr) 0);
                }
                if (DrawPoints)
                {
                    gl.DrawElements(OpenGL.GL_POINTS, Indices.Count, OpenGL.GL_UNSIGNED_INT, (IntPtr) 0);
                }
            }
        }
        
        gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[2]);
        gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[3]);

        unsafe
        {
            fixed (Vertex* ptr = SplineVertices.ToArray())
            {
                gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftVx);
                gl.NormalPointer(OpenGL.GL_FLOAT, sizeof(Vertex), shiftNx);
                gl.ColorPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftR);
                
                gl.LineWidth(2);
                gl.DrawElements(OpenGL.GL_LINE_STRIP, SplineIndices.Count, OpenGL.GL_UNSIGNED_INT, (IntPtr)0);
            }
        }
        #endregion
        
        gl.DisableClientState(OpenGL.GL_COLOR_ARRAY);
        gl.DisableClientState(OpenGL.GL_NORMAL_ARRAY);
        gl.DisableClientState(OpenGL.GL_VERTEX_ARRAY);
    }

    // расчёт точек сплайна загрузка буферов
    private void CalculateSplineAndLoadBuffers()
    {
        RenderDevice.AddScheduleTask((gl, e) =>
        {
            if (Vertices == null || Indices == null || Vertices.Count == 0 || Indices.Count == 0) return;

            #region Вычисление сплайна

            if (SplineVertices == null) SplineVertices = new List<Vertex>();
            else SplineVertices.Clear();
            
            if (SplineIndices == null) SplineIndices = new List<uint>();
            else SplineIndices.Clear();

            for (int i = 0; i < Indices.Count - 4 + 1; i++)
            {
                var p0 = new DVector2(Vertices[(int) Indices[i]].Vx, Vertices[(int) Indices[i]].Vy);
                var p1 = new DVector2(Vertices[(int) Indices[i + 1]].Vx, Vertices[(int) Indices[i + 1]].Vy);
                var p2 = new DVector2(Vertices[(int) Indices[i + 2]].Vx, Vertices[(int) Indices[i + 2]].Vy);
                var p3 = new DVector2(Vertices[(int) Indices[i + 3]].Vx, Vertices[(int) Indices[i + 3]].Vy);

                for (double t = 0; t <= 1; t += 1d / Approximation)
                {
                    var p = CatmullRomCurvePoint(t, p0, p1, p2, p3);
                    SplineVertices.Add(new Vertex((float) p.X, (float) p.Y, 0, 0, 0, 0, (float) SplineColor.X,
                        (float) SplineColor.Y, (float) SplineColor.Z));
                    SplineIndices.Add(SplineIndices.Count > 0 ? SplineIndices.Last() + 1 : 0);
                }
            }

            #endregion
            
            #region Загрузка буферов
            unsafe
            {
                // ломаная
                fixed (Vertex* ptr = &Vertices.ToArray()[0])
                {
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[0]);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER,
                        Vertices.Count * sizeof(Vertex),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }
                fixed (uint* ptr = &Indices.ToArray()[0])
                {
                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[1]);
                    gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
                        Indices.Count * sizeof(uint),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }

                if (SplineVertices == null || SplineIndices == null || SplineVertices.Count == 0 || SplineIndices.Count == 0) return;

                // сплайн
                fixed (Vertex* ptr = &SplineVertices.ToArray()[0])
                {
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[2]);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER,
                        SplineVertices.Count * sizeof(Vertex),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }
                fixed (uint* ptr = &SplineIndices.ToArray()[0])
                {
                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[3]);
                    gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
                        SplineIndices.Count * sizeof(uint),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }
            }
            #endregion
        });
    }

    // вычисление точки из отрезка [p1, p2] на сплайне Катмулла-Рома, t из отрезка [0, 1]
    DVector2 CatmullRomCurvePoint(double t, DVector2 p0, DVector2 p1, DVector2 p2, DVector2 p3)
    {
        var v = new DVector4(1, t, t * t, t * t * t);
        var m = new DMatrix4(0, 1, 0, 0, -Tau, 0, Tau, 0, 2 * Tau, Tau - 3, 3 - 2 * Tau, -Tau, -Tau, 2 - Tau, Tau - 2,
            Tau);
        m.Transpose();
        var w = m * v;
        var x = new DVector4(p0.X, p1.X, p2.X, p3.X);
        var y = new DVector4(p0.Y, p1.Y, p2.Y, p3.Y);
        return new DVector2(w.DotProduct(x), w.DotProduct(y));
    }
    
    // матрица сдвига
    private DMatrix4 ShiftMatrix(DVector3 shift)
    {
        var shiftMatrix = DMatrix4.Identity;
        shiftMatrix.M14 = shift.X;
        shiftMatrix.M24 = shift.Y;
        shiftMatrix.M34 = shift.Z;
        return shiftMatrix;
    }
    
    // матрица масштабирования
    private DMatrix4 ScaleMatrix(DVector3 scale)
    {
        var scaleMatrix = DMatrix4.Identity;
        scaleMatrix.M11 = scale.X;
        scaleMatrix.M22 = scale.Y;
        scaleMatrix.M33 = scale.Z;
        return scaleMatrix;
    }

    private static DMatrix4 Projection(double width, double height, double nearPlane, double farPlane)
    {
        var aspectRatio = width / height;
        if (nearPlane == farPlane || aspectRatio == 0)
            return DMatrix4.Zero;
        var clip = farPlane - nearPlane;
        return new DMatrix4(
            1 / aspectRatio, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,//-(nearPlane + farPlane) / clip, -(2.0 * nearPlane * farPlane) / clip,
            0, 0, -1.0, 1.0
        );
    }

    /// <summary>
    /// Умножение матрицы на видовую матрицу, полученную из точки наблюдения.<para/>
    /// Вектор up не должен быть параллелен линии зрения от глаза к центру.
    /// </summary>
    /// <param name="matrix">Проекционная матрица</param>
    /// <param name="eye">Положение камеры в мировых координатах</param>
    /// <param name="center">Направление взгляда в мировом пространстве</param>
    /// <param name="up">Направление вверх, которое следует рассматривать по отношению к глазу.</param >
    /// <returns>Произведение матрицы и видовой матрицы</returns>
    private static DMatrix4 LookAt(DMatrix4 matrix, DVector3 eye, DVector3 center, DVector3 up)
    {
        var forward = (center - eye).Normalized();
        if (forward.ApproxEqual(DVector3.Zero, 0.00001))
            return matrix;
        var side = (forward * up).Normalized();
        var upVector = side * forward;
        var result = matrix * new DMatrix4(
            +side.X, +side.Y, +side.Z, 0,
            +upVector.X, +upVector.Y, +upVector.Z, 0,
            -forward.X, -forward.Y, -forward.Z, 0,
            0, 0, 0, 1
        );
        result.M14 -= result.M11 * eye.X + result.M12 * eye.Y + result.M13 * eye.Z;
        result.M24 -= result.M21 * eye.X + result.M22 * eye.Y + result.M23 * eye.Z;
        result.M34 -= result.M31 * eye.X + result.M32 * eye.Y + result.M33 * eye.Z;
        result.M44 -= result.M41 * eye.X + result.M42 * eye.Y + result.M43 * eye.Z;
        return result;
    }
}