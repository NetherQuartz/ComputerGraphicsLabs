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
using System.Runtime.InteropServices;
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
    #region Свойства

    [DisplayCheckerProperty(true, "Использовать буфер вершин")]
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

    [DisplayNumericProperty(1.7d, 0.1, 2, "Удаленность камеры")]
    public virtual double CameraDistance {
        get => Get<double>();
        set
        {
            if (Set(value))
                UpdateModelViewMatrix();
        }
    }

    #endregion
    
    static uint[] vbo = new uint[2];
    static Vertex[] Vertices;
    static uint[] Indices;

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        VSPanelWidth = 260;
        ValueStorage.RightColWidth = 60;
        RenderDevice.VSync = 2;
        
        float nf = (float)(1 / Math.Sqrt(3));
        
        Vertices = new [] {
            // vx vy vz nx ny nz r g b
            new Vertex( .5f, .5f, .5f, nf, nf, nf, 1f, 1f, 1f), // v0
            new Vertex(-.5f, .5f, .5f, -nf, nf, nf, 1f, 1f, 0f), // v1
            new Vertex(-.5f, -.5f, .5f, -nf, -nf, nf, 1f, 0f, 0f), // v2
            new Vertex( .5f, -.5f, .5f, nf, -nf, nf, 1f, 0f, 1f), // v3
            new Vertex( .5f, -.5f, -.5f, nf, -nf, -nf, 0f, 0f, 1f), // v4
            new Vertex( .5f, .5f, -.5f, nf, nf, -nf, 0f, 1f, 1f), // v5
            new Vertex(-.5f, .5f, -.5f, -nf, nf, -nf, 0f, 1f, 0f), // v6
            new Vertex(-.5f, -.5f, -.5f, -nf, -nf, -nf, 0f, 0f, 0f), // v7
        };
        
        Indices = new uint[] {
            // Первая последовательность
            5, 6, 0, 1, // {v0,v5,v6,v1} - верхня грань
            /* 0, 1 */ 3, 2, // {v0,v1,v2,v3} - передняя грань
            /* 3, 2 */ 4, 7, // {v7,v4,v3,v2} - нижняя грань
            // Вторая последовательность
            2, 1, 7, 6, // {v1,v6,v7,v2} - левая грань
            /* 7, 6 */ 4, 5, // {v4,v7,v6,v5} - задняя грань
            /* 4, 5 */ 3, 0 // {v0,v3,v4,v5} - правая грань
        };

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
            gl.GenBuffers(2, vbo);
            LoadBuffers(gl);
        }, this);
        #endregion

        #region Уничтожение буфера вершин по завершении работы OGL
        RenderDevice.Closed += (s, e) => // Событие выполняется в контексте потока OGL при завершении работы
        {
            var gl = e.gl;
            gl.UnmapBuffer(OpenGL.GL_ARRAY_BUFFER);
            gl.UnmapBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER);
            gl.DeleteBuffers(2, vbo);
        };
        #endregion

        #region Обновление матрицы проекции при изменении размеров окна и запуске приложения
        RenderDevice.Resized += (s, e) =>
        {
            var gl = e.gl;
            CameraDistance = 1.7;
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            var pMatrix = Perspective(60, (double)e.Width / e.Height, 0.1, 100);
            gl.LoadMatrix(pMatrix.ToArray(true));
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

        if (!UseVBO)
        #region Рендеринг сцены методом VA (Vertex Array)
        {
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
            gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
            gl.EnableClientState(OpenGL.GL_COLOR_ARRAY);

            unsafe
            {
                fixed (Vertex* ptr = Vertices)
                {
                    gl.VertexPointer(3, sizeof(Vertex), &ptr->Vx);
                    gl.NormalPointer(sizeof(Vertex), &ptr->Nx);
                    gl.ColorPointer(3, sizeof(Vertex), &ptr->R);
                    
                    gl.PolygonMode(OpenGL.GL_FRONT, OpenGL.GL_LINE);
                    // gl.DrawElements(OpenGL.GL_TRIANGLES, indices.Length, indices);
                    
                    fixed (uint* i = Indices)
                    {
                        gl.DrawElements(OpenGL.GL_QUAD_STRIP, 8, &i[0]);
                        gl.DrawElements(OpenGL.GL_QUAD_STRIP, 8, &i[8]);
                    }
                }
            }
            
            gl.DisableClientState(OpenGL.GL_COLOR_ARRAY);
            gl.DisableClientState(OpenGL.GL_NORMAL_ARRAY);
            gl.DisableClientState(OpenGL.GL_VERTEX_ARRAY);
        }
        #endregion
        else
        #region Рендеринг сцены методом VBO (Vertex Buffer Object)
        {
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[0]);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vbo[1]);
            LoadBuffers(gl);
            
            // gl.Enable(OpenGL.GL_CULL_FACE);
            gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
            gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
            gl.EnableClientState(OpenGL.GL_COLOR_ARRAY);

            unsafe
            {
                fixed (Vertex* ptr = Vertices)
                {
                    var shiftVx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Vx));
                    var shiftNx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Nx));
                    var shiftR = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.R));
                    
                    gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftVx);
                    gl.NormalPointer(OpenGL.GL_FLOAT, sizeof(Vertex), shiftNx);
                    gl.ColorPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftR);

                    gl.PolygonMode(OpenGL.GL_FRONT, OpenGL.GL_FILL);
                    gl.PolygonMode(OpenGL.GL_BACK, OpenGL.GL_LINE);
                    // gl.DrawElements(OpenGL.GL_QUAD_STRIP, 8, OpenGL.GL_UNSIGNED_INT, (IntPtr)0);
                    gl.DrawElements(OpenGL.GL_QUAD_STRIP, 8, OpenGL.GL_UNSIGNED_INT, (IntPtr)8);
                }
            }
            
            gl.DisableClientState(OpenGL.GL_COLOR_ARRAY);
            gl.DisableClientState(OpenGL.GL_NORMAL_ARRAY);
            gl.DisableClientState(OpenGL.GL_VERTEX_ARRAY);
        }
        #endregion
    }

    private void LoadBuffers(OpenGL gl)
    {
        if (Vertices == null || Indices == null || Vertices.Length == 0 || Indices.Length == 0) return;
        
        unsafe
        {
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
        }
    }
    
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