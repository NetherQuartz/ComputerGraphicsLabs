#region Директивы using (подключаемые библиотеки) и точка входа приложения

using Device = CGLabPlatform.OGLDevice;
using DeviceArgs = CGLabPlatform.OGLDeviceUpdateArgs;
using SharpGL;

using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CGLabExtensions;
using CGLabPlatform;
using CourseWork;
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

    [DisplayNumericProperty(new[] {0d, 0d, 0d}, 1, 0, "Положение камеры (X/Y/Z)")]
    public virtual DVector3 CameraAngle {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                UpdateModelViewMatrix();
        }
    }

    [DisplayNumericProperty(2d, 0.01, 2, "Удаленность камеры", 0.01)]
    public virtual double CameraDistance {
        get => Get<double>();
        set
        {
            if (Set(value))
                UpdateModelViewMatrix();
        }
    }
    
    [DisplayNumericProperty(10, 1, "Аппроксимация", 5)]
    public virtual double Approximation
    {
        get => Get<double>();
        set
        {
            if (Set(value))
            {
                MakeSurface();
                LoadBuffers();
            }
        }
    }

    [DisplayCheckerProperty(false, "Закрашивать полигоны")]
    public virtual bool DrawColor { get; set; }
    
    [DisplayCheckerProperty(true, "Рисовать оси")]
    public virtual bool DrawAxes { get; set; }

    #region Определяющие точки кривых Безье

    [DisplayNumericProperty(new []{-1d, -.1, 1}, .01, 2, "Точка #1")]
    public virtual DVector3 Point1
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{-.5d, .1, 1}, .01, 2, "Точка #2")]
    public virtual DVector3 Point2
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{.3d, .5, 1}, .01, 2, "Точка #3")]
    public virtual DVector3 Point3
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{1d, .2, 1}, .01, 2, "Точка #4")]
    public virtual DVector3 Point4
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{1d, .2, .4}, .01, 2, "Точка #5")]
    public virtual DVector3 Point5
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{1d, -.2, -.2}, .01, 2, "Точка #6")]
    public virtual DVector3 Point6
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{1d, 0, -1}, .01, 2, "Точка #7")]
    public virtual DVector3 Point7
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{.5d, -.1, -1}, .01, 2, "Точка #8")]
    public virtual DVector3 Point8
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{.2d, .2, -1}, .01, 2, "Точка #9")]
    public virtual DVector3 Point9
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{-1d, 0, -1}, .01, 2, "Точка #10")]
    public virtual DVector3 Point10
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{-1d, .1, -.3}, .01, 2, "Точка #11")]
    public virtual DVector3 Point11
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    [DisplayNumericProperty(new []{-1d, .5, 0}, .01, 2, "Точка #12")]
    public virtual DVector3 Point12
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakeSurface();
        }
    }
    
    #endregion

    #endregion

    private static uint[] vbo = new uint[4];
    private static Vertex[] Vertices;
    private static uint[] Indices;

    private static Vertex[] AxesVertices;
    private static uint[] AxesIndices;

    private bool started; // чтобы не обратиться к ещё не инициализированным свойствам

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        ValueStorage.RightColWidth = 50;
        RenderDevice.VSync = 1;

        var font = new Font("Sergoe UI", 12f);
        ValueStorage.Font = font;
        ValueStorage.RowHeight = 35;
        VSPanelWidth = 380;
        MainWindow.Size = new Size(1200, 800);
        
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        #region Кнопки
        
        // кнопка загрузки из файла
        MainWindow.Shown += (s, e) =>
        {
            var btnLoad = new Button {Text = "Загрузить из файла", Font = font};
            btnLoad.Click += (cs, ce) =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    Title = "Выберите файл поверхности"
                };
                var result = dialog.ShowDialog();
                if (result != DialogResult.OK) return;
                
                var path = dialog.FileName;
                var lines = File.ReadAllLines(path);
                try
                {
                    LoadPoints(lines);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Произошла ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            };
            AddControl(btnLoad, 40, nameof(Point1));
            
            // кнопка сохранения в файл
            var btnSave = new Button {Text = "Сохранить в файл", Font = font};
            btnSave.Click += (cs, ce) =>
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    Title = "Выберите место сохранения",
                    OverwritePrompt = true
                };
                var result = dialog.ShowDialog();
                if (result != DialogResult.OK) return;
                
                var path = dialog.FileName;

                var vec2Str = new Func<DVector3, string>(v => $"{v.X, 7} {v.Y, 7} {v.Z, 7}");

                var strings = new string[13];
                strings[0] = $"#{"X",6} {"Y",7} {"Z",7}";
                strings[1] = vec2Str(Point1);
                strings[2] = vec2Str(Point2);
                strings[3] = vec2Str(Point3);
                strings[4] = vec2Str(Point4);
                strings[5] = vec2Str(Point5);
                strings[6] = vec2Str(Point6);
                strings[7] = vec2Str(Point7);
                strings[8] = vec2Str(Point8);
                strings[9] = vec2Str(Point9);
                strings[10] = vec2Str(Point10);
                strings[11] = vec2Str(Point11);
                strings[12] = vec2Str(Point12);

                File.WriteAllLines(path, strings);
            };
            AddControl(btnSave, 40);
        };
        #endregion
        
        #region Параметры для отрисовки осей

        var axisLen = 0.2f;

        AxesVertices = new[]
        {
            new Vertex(0, 0, 0, 0, 0, 0, 1, 1, 1), // центр
            new Vertex(axisLen, 0, 0, 0, 0, 0, 1, 0.2f, 0), // Ox
            new Vertex(0, axisLen, 0, 0, 0, 0, 0, 1, 0), // Oy
            new Vertex(0, 0, axisLen, 0, 0, 0, 0, 0.7f, 1), // Oz
        };
        
        AxesIndices = new uint[]
        {
            0, 1,
            0, 2,
            0, 3
        };

        #endregion

        #region  Инициализация OGL и параметров рендера
        RenderDevice.AddScheduleTask((gl, s) =>
        {
            gl.Disable(OpenGL.GL_DEPTH_TEST);
            gl.ClearColor(0, 0, 0, 0);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
        });
        #endregion

        #region Инициализация буфера вершин
        RenderDevice.AddScheduleTask((gl, s) => 
        {
            gl.GenBuffers(4, vbo);
            LoadBuffers();
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
            CameraDistance = 2.5;
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            var pMatrix = Perspective(60, (double)e.Width / e.Height, 0.1, 100);
            gl.LoadMatrix(pMatrix.ToArray(true));
        };
        #endregion

        #region Управление мышью

        // изменение масштаба колёсиком мыши
        RenderDevice.MouseWheel += (_, e) => CameraDistance -= e.Delta * 0.002;
        
        // вращение камеры
        RenderDevice.MouseMoveWithLeftBtnDown += (_, e) =>
        {
            var angle = CameraAngle.Y;
            while (angle > 360)
            {
                angle -= 360;
            }
            while (angle < 0)
            {
                angle += 360;
            }

            var sign = 1;
            if (angle > 90 && angle < 270)
            {
                sign = -1;
            }
            CameraAngle -= new DVector3(sign * e.MovDeltaY, e.MovDeltaX, 0) * 0.5;
        };

        #endregion

        started = true;
        MakeSurface();
    }

    private void UpdateModelViewMatrix()
    {
        #region Обновление объектно-видовой матрицы
        RenderDevice.AddScheduleTask((gl, s) =>
        {
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            var deg2rad = Math.PI / 180; // Вращается камера, а не сам объект
            var cameraTransform = (DMatrix3)RotationMatrix(deg2rad * CameraAngle.X, deg2rad * CameraAngle.Y, deg2rad * CameraAngle.Z);
            var cameraPosition = cameraTransform * new DVector3(0, 0, CameraDistance);
            var cameraUpDirection = cameraTransform * new DVector3(0, 1, 0);
            // Мировая матрица (преобразование локальной системы координат в мировую)
            var mMatrix = DMatrix4.Identity; // нет никаких преобразований над объекта
            // Видовая матрица (переход из мировой системы координат к системе координат камеры)
            var vMatrix = LookAt(DMatrix4.Identity, cameraPosition, DVector3.Zero, cameraUpDirection);
            // матрица ModelView
            var mvMatrix = vMatrix * mMatrix;
            gl.LoadMatrix(mvMatrix.ToArray(true));
        });
        #endregion
    }
    
    protected override void OnDeviceUpdate(object s, DeviceArgs e)
    {
        var gl = e.gl;
        gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);

        gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, DrawColor ? OpenGL.GL_FILL : OpenGL.GL_LINE);

        gl.Disable(OpenGL.GL_CULL_FACE);

        gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
        gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
        gl.EnableClientState(OpenGL.GL_COLOR_ARRAY);
        
        #region Рендеринг сцены методом VBO (Vertex Buffer Object)
        gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[0]);
        gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[1]);

        unsafe
        {
            var shiftVx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Vx));
            var shiftNx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Nx));
            var shiftR = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.R));
                
            fixed (Vertex* ptr = Vertices)
            {
                gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftVx);
                gl.NormalPointer(OpenGL.GL_FLOAT, sizeof(Vertex), shiftNx);
                gl.ColorPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftR);
                    
                gl.DrawElements(OpenGL.GL_TRIANGLES, Indices.Length, OpenGL.GL_UNSIGNED_INT, (IntPtr)0);
            }

            #region Рисование осей
            if (DrawAxes)
            {
                gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[2]);
                gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[3]);
                    
                fixed (Vertex* ptr = AxesVertices)
                {
                    gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftVx);
                    gl.NormalPointer(OpenGL.GL_FLOAT, sizeof(Vertex), shiftNx);
                    gl.ColorPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftR);
                    
                    SetAxesMatrices();
                    gl.DrawElements(OpenGL.GL_LINES, AxesIndices.Length, OpenGL.GL_UNSIGNED_INT, (IntPtr)0);
                    SetSceneMatrices();
                }
            }
            #endregion
        }
        #endregion
        
        gl.DisableClientState(OpenGL.GL_COLOR_ARRAY);
        gl.DisableClientState(OpenGL.GL_NORMAL_ARRAY);
        gl.DisableClientState(OpenGL.GL_VERTEX_ARRAY);
    }

    // установка матриц, чтобы отображать оси
    void SetAxesMatrices()
    {
        RenderDevice.AddScheduleTask((gl, s) =>
        {
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            var deg2rad = Math.PI / 180;

            var axesCameraTransform = (DMatrix3) RotationMatrix(CameraAngle * deg2rad);
            var axesCameraPosition = axesCameraTransform * DVector3.UnitZ;
            var axesCameraUpDirection = axesCameraTransform * DVector3.UnitY;
            var axesCenter = (-0.7, -0.75, -3.2).ToDVector3();
            var axesVMatrix = LookAt(DMatrix4.Identity, axesCameraPosition, DVector3.Zero,
                axesCameraUpDirection);

            var axesMatrix = ShiftMatrix(axesCenter) * axesVMatrix;
            gl.LoadMatrix(axesMatrix.ToArray(true));

            gl.MatrixMode(OpenGL.GL_PROJECTION);
            var axesScaleMatrix = Perspective(20, (double)RenderDevice.Width / RenderDevice.Height,
                0.1, 100);
            gl.LoadMatrix(axesScaleMatrix.ToArray(true));
        });
    }
    
    // установка матриц, чтобы отображать сцену
    void SetSceneMatrices()
    {
        UpdateModelViewMatrix();
        RenderDevice.AddScheduleTask((gl, s) =>
        {
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            var pMatrix = Perspective(60, (double)RenderDevice.Width / RenderDevice.Height,
                0.1, 100);
            gl.LoadMatrix(pMatrix.ToArray(true));
        });
    }

    // список точек кубической кривой Безье с определяющим многоугольником p0, p1, p2, p3
    List<DVector3> Bezier(DVector3 p0, DVector3 p1, DVector3 p2, DVector3 p3)
    {
        var vertices = new List<DVector3>();
        for (double t = 0; t <= 1; t += 1 / Approximation)
        {
            var p = p0 * Math.Pow(1 - t, 3) + 3 * p1 * t * Math.Pow(1 - t, 2) + 3 * p2 * t * t * (1 - t) +
                    p3 * t * t * t;
            vertices.Add(p);
        }
        return vertices;
    }

    // генерация меша поверхности
    private void MakeSurface()
    {
        if (!started) return;

        var vertices = new List<Vertex>();

        var q00 = Point1;
        var q10 = Point4;
        var q11 = Point7;
        var q01 = Point10;

        var s1 = Bezier(Point1, Point2, Point3, Point4);    // Q(u, 0)
        var s2 = Bezier(Point10, Point9, Point8, Point7);   // Q(u, 1)
        var s3 = Bezier(Point1, Point12, Point11, Point10); // Q(0, v)
        var s4 = Bezier(Point4, Point5, Point6, Point7);    // Q(1, v)
        
        var indices = new List<uint>();
        var polygons = new List<Polygon>();
        var surfacePoints = new List<List<DVector3>>();
        
        int ui = 0;
        for (double u = 0; u <= 1; u += 1 / Approximation)
        {
            int vi = 0;
            surfacePoints.Add(new List<DVector3>());
            for (double v = 0; v <= 1; v += 1 / Approximation)
            {
                double x = new DVector2(1 - u, u).DotProduct(new DVector2(s3[vi].X, s4[vi].X)) +
                        new DVector2(s1[ui].X, s2[ui].X).DotProduct(new DVector2(1 - v, v)) - 
                        new DVector2(
                            new DVector2(1 - u, u).DotProduct(new DVector2(q00.X, q10.X)),
                            new DVector2(1 - u, u).DotProduct(new DVector2(q01.X, q11.X))
                            ).DotProduct(new DVector2(1 - v, v));
                
                double y = new DVector2(1 - u, u).DotProduct(new DVector2(s3[vi].Y, s4[vi].Y)) +
                           new DVector2(s1[ui].Y, s2[ui].Y).DotProduct(new DVector2(1 - v, v)) - 
                           new DVector2(
                               new DVector2(1 - u, u).DotProduct(new DVector2(q00.Y, q10.Y)),
                               new DVector2(1 - u, u).DotProduct(new DVector2(q01.Y, q11.Y))
                           ).DotProduct(new DVector2(1 - v, v));
                
                double z = new DVector2(1 - u, u).DotProduct(new DVector2(s3[vi].Z, s4[vi].Z)) +
                           new DVector2(s1[ui].Z, s2[ui].Z).DotProduct(new DVector2(1 - v, v)) - 
                           new DVector2(
                               new DVector2(1 - u, u).DotProduct(new DVector2(q00.Z, q10.Z)),
                               new DVector2(1 - u, u).DotProduct(new DVector2(q01.Z, q11.Z))
                           ).DotProduct(new DVector2(1 - v, v));
                
                surfacePoints[ui].Add(new DVector3(x, y, z));
                vi++;
            }
            ui++;
        }

        for (int i = 1; i < surfacePoints.Count; i++)
        {
            for (int j = 1; j < surfacePoints[i].Count; j++)
            {
                polygons.Add(new Polygon(
                    surfacePoints[i - 1][j - 1],
                    surfacePoints[i][j - 1],
                    surfacePoints[i][j]));
                polygons.Add(new Polygon(
                    surfacePoints[i - 1][j - 1],
                    surfacePoints[i][j],
                    surfacePoints[i - 1][j]));
            }
        }
        
        // для каждой вершины поверхности выясняем, какие полигоны её содержат
        var verticesDict = new Dictionary<DVector3, List<Polygon>>();
        foreach (var polygon in polygons)
        {
            if (!verticesDict.ContainsKey(polygon.P1))
            {
                verticesDict.Add(polygon.P1, new List<Polygon>());
            }
            verticesDict[polygon.P1].Add(polygon);
            
            if (!verticesDict.ContainsKey(polygon.P2))
            {
                verticesDict.Add(polygon.P2, new List<Polygon>());
            }
            verticesDict[polygon.P2].Add(polygon);
            
            if (!verticesDict.ContainsKey(polygon.P3))
            {
                verticesDict.Add(polygon.P3, new List<Polygon>());
            }
            verticesDict[polygon.P3].Add(polygon);
        }

        // рассчитываем нормали для вершин поверхности как среднюю нормаль смежных полигонов
        var verticesNormals = new Dictionary<DVector3, DVector3>();
        foreach (var vertex in verticesDict)
        {
            var normal = DVector3.Zero;
            foreach (var polygon in vertex.Value)
            {
                normal += polygon.Normal;
            }
            normal.Normalize();
            verticesNormals[vertex.Key] = normal;
        }

        float red = 1;
        float green = 1;
        float blue = 1;
        
        // добавляем вершины поверхности в массив вершин
        foreach (var polygon in polygons)
        {
            var v1 = polygon.P1;
            var v2 = polygon.P2;
            var v3 = polygon.P3;

            var n1 = verticesNormals[v1]; 
            var n2 = verticesNormals[v2]; 
            var n3 = verticesNormals[v3];

            vertices.Add(new Vertex(
                (float)v3.X, (float)v3.Y, (float)v3.Z,
                (float)n3.X, (float)n3.Y, (float)n3.Z,
                red, green, blue));
            
            vertices.Add(new Vertex(
                (float)v2.X, (float)v2.Y, (float)v2.Z,
                (float)n2.X, (float)n2.Y, (float)n2.Z,
                red, green, blue));
            
            vertices.Add(new Vertex(
                (float)v1.X, (float)v1.Y, (float)v1.Z,
                (float)n1.X, (float)n1.Y, (float)n1.Z,
                red, green, blue));
        }

        // массив индексов
        for (uint i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }

        Vertices = vertices.ToArray();
        Indices = indices.ToArray();
        LoadBuffers();
    }

    // загрузка буферов
    private void LoadBuffers()
    {
        RenderDevice.AddScheduleTask((gl, e) =>
        {
            if (Vertices == null || Indices == null || Vertices.Length == 0 || Indices.Length == 0) return;
        
            unsafe
            {
                #region Меш

                fixed (Vertex* ptr = &Vertices[0])
                {
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[0]);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER,
                        Vertices.Length * sizeof(Vertex),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }
                fixed (uint* ptr = &Indices[0])
                {
                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[1]);
                    gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
                        Indices.Length * sizeof(uint),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }

                #endregion

                #region Оси

                fixed (Vertex* ptr = &AxesVertices[0])
                {
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[2]);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER,
                        AxesVertices.Length * sizeof(Vertex),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }
                fixed (uint* ptr = &AxesIndices[0])
                {
                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[3]);
                    gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
                        AxesIndices.Length * sizeof(uint),
                        (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                }

                #endregion
            }
        });
    }
    
    // матрица поворота
    private DMatrix4 RotationMatrix(double x, double y, double z)
    {
        var rotX = DMatrix4.Identity;
        rotX.M22 = rotX.M33 = Math.Cos(x);
        rotX.M32 = Math.Sin(x);
        rotX.M23 = -rotX.M32;

        var rotY = DMatrix4.Identity;
        rotY.M11 = rotY.M33 = Math.Cos(y);
        rotY.M13 = Math.Sin(y);
        rotY.M31 = -rotY.M13;

        var rotZ = DMatrix4.Identity;
        rotZ.M11 = rotZ.M22 = Math.Cos(z);
        rotZ.M21 = Math.Sin(z);
        rotZ.M12 = -rotZ.M21;

        return rotX * rotY * rotZ;
    }

    private DMatrix4 RotationMatrix(DVector3 rotation)
    {
        return RotationMatrix(rotation.X, rotation.Y, rotation.Z);
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

    /// <summary>
    /// Матрица перспективной проекции
    /// </summary>
    /// <param name="verticalAngle">Вертикальное поле зрения в градусах. Обычно между 90 (очень широкое) и 30(узкое) </param >
    /// <param name="aspectRatio">Отношение сторон. Зависит от размеров устройства вывода(окна) </param >
    /// <param name="nearPlane">Ближняя плоскость отсечения. Должна быть больше 0</param>
    /// <param name="farPlane">Дальняя плоскость отсечения</param>
    private static DMatrix4 Perspective(double verticalAngle, double aspectRatio,
        double nearPlane, double farPlane)
    {
        var radians = verticalAngle / 2 * Math.PI / 180;
        var sine = Math.Sin(radians);
        if (nearPlane == farPlane || aspectRatio == 0 || sine == 0)
            return DMatrix4.Zero;
        var cotan = Math.Cos(radians) / sine;
        var clip = farPlane - nearPlane;
        return new DMatrix4(
            cotan / aspectRatio, 0, 0, 0,
            0, cotan, 0, 0,
            0, 0, -(nearPlane + farPlane) / clip, -(2.0 * nearPlane * farPlane) / clip,
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
    
    // добавление элемента управления перед элементом InsertBeforeProperty
    private void AddControl(Control ctrl, int height, string InsertBeforeProperty)
    {
        var layout = ValueStorage.Controls[0].Controls[0] as TableLayoutPanel;
        layout.SuspendLayout();
        ctrl.Dock = DockStyle.Fill;
        layout.Parent.Height += height;
        var beforectrl = ValueStorage.GetControlForProperty(InsertBeforeProperty);
        var position = layout.GetPositionFromControl(beforectrl).Row + 1;
        for (int r = layout.RowCount; position <= r--; ) {
            for (int c = layout.ColumnCount; 0 != c--; ) {
                var control = layout.GetControlFromPosition(c, r);
                if (control != null) layout.SetRow(control, r + 1);
            }
        }
        layout.RowStyles.Insert(position-1, new RowStyle(SizeType.Absolute, height));
        layout.Controls.Add(ctrl, 0, position-1);
        layout.SetColumnSpan(ctrl, 2);
        layout.RowCount++;
        layout.ResumeLayout(true);

    }
    
    // добавление элемента управления в конец
    private void AddControl(Control ctrl, int height)
    {
        var layout = ValueStorage.Controls[0].Controls[0] as TableLayoutPanel;

        layout.SuspendLayout();
        ctrl.Dock = DockStyle.Fill;
        layout.Parent.Height += height;
        layout.RowStyles.Insert(layout.RowCount - 1, new RowStyle(SizeType.Absolute, height));
        layout.Controls.Add(ctrl, 0, layout.RowCount - 1);
        layout.SetColumnSpan(ctrl, 2);
        layout.RowCount++;
        layout.ResumeLayout(true);
    }

    // парсинг массива строк с координатами точек и установка этих значений в Point1..12
    void LoadPoints(string[] lines)
    {
        var pattern = new Regex(@"^(\s*[\-\+]?\d+(\.\d+)?){3}\s*(#.*)?$");
        var points = new List<DVector3>();
        var i = 0;
        foreach (var line in lines)
        {
            i++;
            if (line == "" || line[0] == '#')
                continue;
            if (!pattern.IsMatch(line))
                throw new Exception($"Ошибка парсинга: неверный синтаксис в строке #{i}");

            var match = pattern.Match(line).Groups[1].Captures;
            var result = new double[3];
            for (int j = 0; j < 3; j++)
            {
                if (!double.TryParse(match[j].Value, out result[j]))
                    throw new Exception($"Ошибка парсинга: неверный формат числа в строке #{i}");
            }

            points.Add(new DVector3(result[0], result[1], result[2]));
        }

        if (points.Count != 12)
            throw new Exception($"Ошибка парсинга: неверное число точек ({points.Count}/12)");

        Point1 = points[0];
        Point2 = points[1];
        Point3 = points[2];
        Point4 = points[3];
        Point5 = points[4];
        Point6 = points[5];
        Point7 = points[6];
        Point8 = points[7];
        Point9 = points[8];
        Point10 = points[9];
        Point11 = points[10];
        Point12 = points[11];
        
        lock (RenderDevice.LockObj)
        {
            MakeSurface();
        }
    }

}