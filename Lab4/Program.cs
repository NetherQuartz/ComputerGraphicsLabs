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
using System.Runtime.InteropServices;
using CGLabExtensions;
using CGLabPlatform;
using Lab4;
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

    [DisplayCheckerProperty(false, "Использовать буфер вершин")]
    public virtual bool UseVBO { get; set; }

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
    
    [DisplayNumericProperty(new[] {1d, 1.5d}, 0.1, "Форма", 0.1)]
    public virtual DVector2 PrismSize
    {
        get => Get<DVector2>();
        set
        {
            if (Set(value))
                MakePrism();
        }
    }
    
    [DisplayNumericProperty(new[] {0.1d, 0d}, 0.1, "Сдвиг оснований")]
    public virtual DVector2 BaseShift
    {
        get => Get<DVector2>();
        set
        {
            if (Set(value))
                MakePrism();
        }
    }

    [DisplayNumericProperty(new[] {20d, 10d, 5d}, 1, "Аппроксимация", 3)]
    public virtual DVector3 Approximation
    {
        get => Get<DVector3>();
        set
        {
            if (Set(value))
                MakePrism();
        }
    }

    [DisplayCheckerProperty(false, "Рисовать невидимые полигоны")]
    public virtual bool DrawInvisiblePolygons { get; set; }
    
    [DisplayCheckerProperty(false, "Закрашивать полигоны")]
    public virtual bool DrawColor { get; set; }
    
    [DisplayCheckerProperty(true, "Рисовать оси")]
    public virtual bool DrawAxes { get; set; }
    
    #endregion

    private static uint[] vbo = new uint[4];
    private static Vertex[] Vertices;
    private static uint[] Indices;

    private static Vertex[] AxesVertices;
    private static uint[] AxesIndices;

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        VSPanelWidth = 260;
        ValueStorage.RightColWidth = 60;
        RenderDevice.VSync = 1;

        MakePrism();

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
            LoadBuffers(gl);
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

        if (DrawInvisiblePolygons)
        {
            gl.Disable(OpenGL.GL_CULL_FACE);
        }
        else
        {
            gl.Enable(OpenGL.GL_CULL_FACE);
        }

        gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
        gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
        gl.EnableClientState(OpenGL.GL_COLOR_ARRAY);
        
        if (!UseVBO)
        #region Рендеринг сцены методом VA (Vertex Array)
        {
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);

            unsafe
            {
                fixed (Vertex* ptr = Vertices)
                {
                    gl.VertexPointer(3, sizeof(Vertex), &ptr->Vx);
                    gl.NormalPointer(sizeof(Vertex), &ptr->Nx);
                    gl.ColorPointer(3, sizeof(Vertex), &ptr->R);

                    fixed (uint* i = Indices)
                    {
                        gl.DrawElements(OpenGL.GL_TRIANGLES, Indices.Length, &i[0]);
                    }
                }

                if (DrawAxes)
                {
                    fixed (Vertex* ptr = AxesVertices)
                    {
                        gl.VertexPointer(3, sizeof(Vertex), &ptr->Vx);
                        gl.NormalPointer(sizeof(Vertex), &ptr->Nx);
                        gl.ColorPointer(3, sizeof(Vertex), &ptr->R);
                    
                        fixed (uint* i = AxesIndices)
                        {
                            SetAxesMatrices();
                            gl.DrawElements(OpenGL.GL_LINES, AxesIndices.Length, &i[0]);
                            SetSceneMatrices();
                        }
                    }
                }
            }
        }
        #endregion
        else
        #region Рендеринг сцены методом VBO (Vertex Buffer Object)
        {
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
            }
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
    
    // генерация меша призмы
    private void MakePrism()
    {
        var edges = (int)Approximation.X;
        var radius = PrismSize.X;
        var height = PrismSize.Y;
        var shift = BaseShift.ToDVector4(0, 0);

        if (edges <= 0 || radius <= 0 || height <= 0) return;

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
        
        // для каждой вершины выясняем, какие полигоны её содержат
        var verticesDict = new Dictionary<DVector4, List<Polygon>>();
        foreach (var polygon in mesh)
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

        // рассчитываем нормали для вершин как среднюю нормаль смежных полигонов
        var verticesNormals = new Dictionary<DVector4, DVector4>();
        foreach (var vertex in verticesDict)
        {
            var normal = DVector4.Zero;
            foreach (var polygon in vertex.Value)
            {
                normal += polygon.Normal;
            }
            normal /= vertex.Value.Count;
            verticesNormals[vertex.Key] = normal;
        }
        
        // создаём массив вершин
        var vertices = new List<Vertex>();
        foreach (var polygon in mesh)
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
                1, 1, 1));
            
            vertices.Add(new Vertex(
                (float)v2.X, (float)v2.Y, (float)v2.Z,
                (float)n2.X, (float)n2.Y, (float)n2.Z,
                1, 1, 1));
            
            vertices.Add(new Vertex(
                (float)v1.X, (float)v1.Y, (float)v1.Z,
                (float)n1.X, (float)n1.Y, (float)n1.Z,
                1, 1, 1));
        }

        // массив индексов
        var indices = new List<uint>();
        for (uint i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }

        Vertices = vertices.ToArray();
        Indices = indices.ToArray();
    }

    // загрузка буферов
    private void LoadBuffers(OpenGL gl)
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

}