#region Директивы using (подключаемые библиотеки)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CGLabExtensions;
using CGLabPlatform;
using SharpGL;

#endregion

namespace CourseWork
{
    public abstract class AppMain : MyApp
    {
        [STAThread]
        private static void Main()
        {
            RunApplication();
        }
    }

    public abstract class MyApp : CGApplicationTemplate<MyApp, OGLDevice, OGLDeviceUpdateArgs>
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
    
        [DisplayNumericProperty(20, 1, "Аппроксимация", 5)]
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

        [DisplayCheckerProperty(true, "Закрашивать полигоны")]
        public virtual bool DrawColor { get; set; }
    
        [DisplayCheckerProperty(true, "Рисовать опорные точки")]
        public virtual bool DrawPoints { get; set; }
    
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

        private static readonly uint[] VBO = new uint[6]; // массив указателей на буферы вершин и индексов
        private static Vertex[] Vertices; // вершины поверхности
        private static uint[] Indices;    // индексы вершин поверхности

        private static Vertex[] AxesVertices;   // вершины осей в углу
        private static uint[] AxesIndices;      // индексы вершин осей в углу
        private static Vertex[] PointsVertices; // вершины - опорные точки
        private static uint[] PointsIndices;    // индексы вершин - опорных точек
    
        private DMatrix4 ModelViewMatrix, ProjectionMatrix; // матрицы преобразования и проекции

        private bool started; // флаг, чтобы не обратиться к ещё не инициализированным свойствам
    
        private uint ProgShader; // указатель на программу шейдера
        private uint VertShader; // указатель на вершинный шейдер
        private uint FragShader; // указатель на фрагментный шейдер

        // переменные для передачи в шейдер
        private int uniformPMatrix, uniformMVMatrix; // матрицы преобразования и проекции
        private int attribVPosition, attribVNormal, attribVCol; // положение, нормаль и цвет вершины

        protected override void OnMainWindowLoad(object sender, EventArgs args)
        {
            ValueStorage.RightColWidth = 50;
            RenderDevice.VSync = 1;

            var font = new Font("Sergoe UI", 12f);
            ValueStorage.Font = font;
            ValueStorage.RowHeight = 35;
            VSPanelWidth = 380;
            MainWindow.Size = new Size(1200, 800);
        
            // установка культуры, чтобы дробные числа форматировались с точкой, а не запятой
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            #region Кнопки
        
            // кнопка загрузки из файла
            MainWindow.Shown += (s, e) =>
            {
                var btnLoad = new Button {Text = @"Загрузить из файла", Font = font};
                btnLoad.Click += (cs, ce) =>
                {
                    var dialog = new OpenFileDialog
                    {
                        Filter = @"Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                        Title = @"Выберите файл поверхности"
                    };
                    var result = dialog.ShowDialog(); // показ диалога пользователю
                    if (result != DialogResult.OK) return; // если файл не выбран, то выходим из метода
                
                    var path = dialog.FileName; // путь к выбранному файлу
                    var lines = File.ReadAllLines(path); // читаем список строк из файла
                    
                    // парсим список строк, устанавливаем значения точек или показываем сообщение об ошибке в случае неудачи
                    try
                    {
                        LoadPoints(lines);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, @"Произошла ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                };
                AddControl(btnLoad, 40, nameof(Point1)); // добавляем кнопку перед полями выбора точек
            
                // кнопка сохранения в файл
                var btnSave = new Button {Text = @"Сохранить в файл", Font = font};
                btnSave.Click += (cs, ce) =>
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = @"Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                        Title = @"Выберите место сохранения",
                        OverwritePrompt = true
                    };
                    var result = dialog.ShowDialog(); // показ диалога пользователю
                    if (result != DialogResult.OK) return; // если файл не указан, то выходим из метода
                
                    var path = dialog.FileName; // путь к указанному файлу
                    
                    // функция форматирования вектора для записи в файл
                    var vec2Str = new Func<DVector3, string>(v => $"{v.X, 7} {v.Y, 7} {v.Z, 7}");

                    // создание и заполнение массива строк выходного файла
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

                    // запись строк в файл
                    File.WriteAllLines(path, strings);
                };
                AddControl(btnSave, 40); // добавляем кнопку в конец
            };
            #endregion
        
            #region Параметры для отрисовки осей

            const float axisLen = 0.2f; // длина оси

            AxesVertices = new[]
            {
                new Vertex(0, 0, 0, 0, 0, 0, 1, 1, 1),          // начало координат
                new Vertex(axisLen, 0, 0, 0, 0, 0, 1, 0.2f, 0), // Ox
                new Vertex(0, axisLen, 0, 0, 0, 0, 0, 1, 0),    // Oy
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
                gl.Enable(OpenGL.GL_DEPTH_TEST);
                gl.ClearColor(0, 0, 0, 0);
                gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
                gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
            });
            #endregion
        
            #region Загрузка и компиляция шейдера
            RenderDevice.AddScheduleTask((gl, s) =>
            {
                var shaderCompileParameters = new int[1];
            
                var loadAndCompileShader = new Func<uint, string, uint>((shaderType, shaderFile) =>
                {
                    var shader = gl.CreateShader(shaderType);
                    if (shader == 0)
                    {
                        throw new Exception("OpenGL error: не удалось создать объект шейдера.");
                    }

                    var source = HelpUtils.GetTextFileFromRes(shaderFile);
                    gl.ShaderSource(shader, source);
                    gl.CompileShader(shader);
                
                    gl.GetShader(shader, OpenGL.GL_COMPILE_STATUS, shaderCompileParameters);
                    if (shaderCompileParameters[0] != OpenGL.GL_TRUE)
                    {
                        gl.GetShader(shader, OpenGL.GL_INFO_LOG_LENGTH, shaderCompileParameters);
                        var strBuilder = new StringBuilder(shaderCompileParameters[0]);
                        gl.GetShaderInfoLog(shader, shaderCompileParameters[0], IntPtr.Zero, strBuilder);
                        Trace.WriteLine(strBuilder);
                        throw new Exception(@$"OpenGL error: ошибка компиляции {
                            shaderType switch
                            {
                                OpenGL.GL_VERTEX_SHADER => "вершинного",
                                OpenGL.GL_FRAGMENT_SHADER => "фрагментного",
                                _ => "неизвестного"
                            }} шейдера");
                    }
                
                    gl.AttachShader(ProgShader, shader);
                    return shader;
                });
            
                if ((ProgShader = gl.CreateProgram()) == 0)
                {
                    throw new Exception("OpenGL error: не удалось создать программу шейдера.");
                }

                VertShader = loadAndCompileShader(OpenGL.GL_VERTEX_SHADER, "shader.vert");
                FragShader = loadAndCompileShader(OpenGL.GL_FRAGMENT_SHADER, "shader.frag");
                gl.LinkProgram(ProgShader);
                gl.GetProgram(ProgShader, OpenGL.GL_LINK_STATUS, shaderCompileParameters);
                if (shaderCompileParameters[0] != OpenGL.GL_TRUE)
                {
                    gl.GetProgram(ProgShader, OpenGL.GL_INFO_LOG_LENGTH, shaderCompileParameters);
                    var strBuilder = new StringBuilder(shaderCompileParameters[0]);
                    gl.GetProgramInfoLog(ProgShader, shaderCompileParameters[0], IntPtr.Zero, strBuilder);
                    Trace.WriteLine(strBuilder);
                    throw new Exception("OpenGL error: не удалось слинковать программу шейдера.");
                }
            
                // получение указателей на переменные, используемые шейдерами 
                if ((uniformPMatrix = gl.GetUniformLocation(ProgShader, "PMatrix")) < 0)
                {
                    throw new Exception("OpenGL error: не удалось найти переменную PMatrix");
                }
                if ((uniformMVMatrix = gl.GetUniformLocation(ProgShader, "MVMatrix")) < 0)
                {
                    throw new Exception("OpenGL error: не удалось найти переменную MVMatrix");
                }

                if ((attribVPosition = gl.GetAttribLocation(ProgShader, "vPosition")) < 0)
                {
                    throw new Exception("OpenGL error: не удалось найти переменную vPosition");
                }
                if ((attribVNormal = gl.GetAttribLocation(ProgShader, "vNormal")) < 0)
                {
                    throw new Exception("OpenGL error: не удалось найти переменную vNormal");
                }
                if ((attribVCol = gl.GetAttribLocation(ProgShader, "vColor")) < 0)
                {
                    throw new Exception("OpenGL error: не удалось найти переменную vColor");
                }
            });
            #endregion

            #region Удаление шейдера по завершении работы программы
            RenderDevice.Closed += (s, e) =>
            {
                RenderDevice.AddScheduleTask((gl, _) =>
                {
                    gl.DeleteProgram(ProgShader);
                    ProgShader = 0;
                    gl.DeleteShader(FragShader);
                    FragShader = 0;
                    gl.DeleteShader(VertShader);
                    VertShader = 0;
                });
            };
            #endregion

            #region Инициализация буфера вершин
            RenderDevice.AddScheduleTask((gl, s) => 
            {
                gl.GenBuffers(6, VBO);
            }, this);
            #endregion

            #region Уничтожение буфера вершин по завершении работы OGL
            // Событие выполняется в контексте потока OGL при завершении работы
            RenderDevice.Closed += (s, e) =>
            {
                var gl = e.gl;
                gl.UnmapBuffer(OpenGL.GL_ARRAY_BUFFER);
                gl.UnmapBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER);
                gl.DeleteBuffers(6, VBO);
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
                ProjectionMatrix = pMatrix;
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
                const double deg2Rad = Math.PI / 180; // Вращается камера, а не сам объект
                var cameraTransform = (DMatrix3)RotationMatrix(deg2Rad * CameraAngle.X, deg2Rad * CameraAngle.Y, deg2Rad * CameraAngle.Z);
                var cameraPosition = cameraTransform * new DVector3(0, 0, CameraDistance);
                var cameraUpDirection = cameraTransform * new DVector3(0, 1, 0);
                // Мировая матрица (преобразование локальной системы координат в мировую)
                var mMatrix = DMatrix4.Identity; // нет никаких преобразований над объекта
                // Видовая матрица (переход из мировой системы координат к системе координат камеры)
                var vMatrix = LookAt(DMatrix4.Identity, cameraPosition, DVector3.Zero, cameraUpDirection);
                // матрица ModelView
                var mvMatrix = vMatrix * mMatrix;
                gl.LoadMatrix(mvMatrix.ToArray(true));
                ModelViewMatrix = mvMatrix;
            });
            #endregion
        }
    
        protected override void OnDeviceUpdate(object s, OGLDeviceUpdateArgs e)
        {
            var gl = e.gl;
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
        
            gl.UseProgram(ProgShader);

            gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, DrawColor ? OpenGL.GL_FILL : OpenGL.GL_LINE);
        
            gl.Enable(OpenGL.GL_CULL_FACE);

            gl.EnableVertexAttribArray((uint)attribVPosition);
            gl.EnableVertexAttribArray((uint)attribVNormal);
            gl.EnableVertexAttribArray((uint)attribVCol);
            
            gl.UniformMatrix4(uniformPMatrix, 1, true, ProjectionMatrix.ToFloatArray());
            gl.UniformMatrix4(uniformMVMatrix, 1, true, ModelViewMatrix.ToFloatArray());

            #region Рендеринг сцены методом VBO (Vertex Buffer Object)
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[0]);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, VBO[1]);

            unsafe
            {
                var shiftVx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Vx));
                var shiftNx = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.Nx));
                var shiftR = Marshal.OffsetOf(typeof(Vertex), nameof(Vertex.R));
                
                fixed (Vertex* _ = Vertices)
                {
                    gl.VertexAttribPointer((uint)attribVPosition, 3, OpenGL.GL_FLOAT, false, sizeof(Vertex), shiftVx);
                    gl.VertexAttribPointer((uint)attribVNormal, 3, OpenGL.GL_FLOAT, false, sizeof(Vertex), shiftNx);
                    gl.VertexAttribPointer((uint)attribVCol, 3, OpenGL.GL_FLOAT, false, sizeof(Vertex), shiftR);
                    
                    gl.DrawElements(OpenGL.GL_TRIANGLES, Indices.Length, OpenGL.GL_UNSIGNED_INT, (IntPtr)0);
                }

                #region Рисование осей
                if (DrawAxes)
                {
                    gl.UseProgram(0);
                    gl.DisableVertexAttribArray((uint)attribVPosition);
                    gl.DisableVertexAttribArray((uint)attribVNormal);
                    gl.DisableVertexAttribArray((uint)attribVCol);
            
                    gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
                    gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
                    gl.EnableClientState(OpenGL.GL_COLOR_ARRAY);
                    
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[2]);
                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, VBO[3]);
                    
                    fixed (Vertex* _ = AxesVertices)
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
            
                #region Рисование опорных точек
                if (DrawPoints)
                {
                    gl.UseProgram(0);
                    gl.DisableVertexAttribArray((uint)attribVPosition);
                    gl.DisableVertexAttribArray((uint)attribVNormal);
                    gl.DisableVertexAttribArray((uint)attribVCol);
            
                    gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
                    gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
                    gl.EnableClientState(OpenGL.GL_COLOR_ARRAY);
                    
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[4]);
                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, VBO[5]);
                    
                    fixed (Vertex* _ = PointsVertices)
                    {
                        gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftVx);
                        gl.NormalPointer(OpenGL.GL_FLOAT, sizeof(Vertex), shiftNx);
                        gl.ColorPointer(3, OpenGL.GL_FLOAT, sizeof(Vertex), shiftR);
                    
                        gl.PointSize(10);
                        gl.DrawElements(OpenGL.GL_POINTS, PointsIndices.Length, OpenGL.GL_UNSIGNED_INT, (IntPtr)0);
                        gl.PointSize(1);
                    }
                }
                #endregion
            }
            #endregion
        }

        // установка матриц, чтобы отображать оси
        private void SetAxesMatrices()
        {
            RenderDevice.AddScheduleTask((gl, s) =>
            {
                gl.MatrixMode(OpenGL.GL_MODELVIEW);
                const double deg2Rad = Math.PI / 180;

                var axesCameraTransform = (DMatrix3) RotationMatrix(CameraAngle * deg2Rad);
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
        private void SetSceneMatrices()
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
        private List<DVector3> Bezier(DVector3 p0, DVector3 p1, DVector3 p2, DVector3 p3)
        {
            var vertices = new List<DVector3>();
            for (int i = 0; i <= Approximation; i++)
            {
                var t = i / Approximation;
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

            // углы поверхности
            var q00 = Point1;
            var q01 = Point10;
            var q10 = Point4;
            var q11 = Point7;

            // гроницы поверхности
            var s1 = Bezier(Point1, Point2, Point3, Point4);    // Q(u, 0)
            var s2 = Bezier(Point10, Point9, Point8, Point7);   // Q(u, 1)
            var s3 = Bezier(Point1, Point12, Point11, Point10); // Q(0, v)
            var s4 = Bezier(Point4, Point5, Point6, Point7);    // Q(1, v)
        
            var indices = new List<uint>();
            var polygons = new List<Polygon>();
            var surfacePoints = new List<List<DVector3>>();
        
            // заполнение внутренних точек поверхности
            for (int ui = 0; ui <= Approximation; ui++)
            {
                var u = ui / Approximation;
                surfacePoints.Add(new List<DVector3>());
                for (int vi = 0; vi <= Approximation; vi++)
                {
                    var v = vi / Approximation;
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
                }
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
                var normal = vertex.Value.Aggregate(DVector3.Zero, (current, polygon) => current + polygon.Normal);
                verticesNormals[vertex.Key] = normal;
            }

            const float red = 1;
            const float green = 1;
            const float blue = 1;
        
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
        
            // добавляем те же самые вершины, но с противоположными нормалями, чтобы поверхность было видно с обеих сторон
            foreach (var polygon in polygons)
            {
                var v1 = polygon.P1;
                var v2 = polygon.P2;
                var v3 = polygon.P3;

                var n1 = -verticesNormals[v1];
                var n2 = -verticesNormals[v2];
                var n3 = -verticesNormals[v3];

                vertices.Add(new Vertex(
                    (float)v3.X, (float)v3.Y, (float)v3.Z,
                    (float)n3.X, (float)n3.Y, (float)n3.Z,
                    red, green, blue));
            
                vertices.Add(new Vertex(
                    (float)v1.X, (float)v1.Y, (float)v1.Z,
                    (float)n1.X, (float)n1.Y, (float)n1.Z,
                    red, green, blue));
            
                vertices.Add(new Vertex(
                    (float)v2.X, (float)v2.Y, (float)v2.Z,
                    (float)n2.X, (float)n2.Y, (float)n2.Z,
                    red, green, blue));
            }

            // массив индексов
            for (uint i = 0; i < vertices.Count; i++)
            {
                indices.Add(i);
            }

            Vertices = vertices.ToArray();
            Indices = indices.ToArray();
        
            #region Заполнение опорных точек

            PointsVertices = new []
            {
                new Vertex(
                    (float) Point1.X, (float) Point1.Y, (float) Point1.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point2.X, (float) Point2.Y, (float) Point2.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point3.X, (float) Point3.Y, (float) Point3.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point4.X, (float) Point4.Y, (float) Point4.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point5.X, (float) Point5.Y, (float) Point5.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point6.X, (float) Point6.Y, (float) Point6.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point7.X, (float) Point7.Y, (float) Point7.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point8.X, (float) Point8.Y, (float) Point8.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point9.X, (float) Point9.Y, (float) Point9.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point10.X, (float) Point10.Y, (float) Point10.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point11.X, (float) Point11.Y, (float) Point11.Z,
                    0, 0, 0),
                new Vertex(
                    (float) Point12.X, (float) Point12.Y, (float) Point12.Z,
                    0, 0, 0),
            };

            PointsIndices = new uint[12];
            for (uint i = 0; i < 12; i++)
            {
                PointsIndices[(int) i] = i;
            }

            #endregion
        
            LoadBuffers();
        }

        // загрузка буферов
        private void LoadBuffers()
        {
            RenderDevice.AddScheduleTask((gl, e) =>
            {
                if (Vertices == null || Indices == null || Vertices.Length == 0 || Indices.Length == 0) return;
        
                // структура массива VBO: {
                //      вершины поверхности, индексы вершин поверхности,
                //      вершины осей, индексы вершин осей,
                //      вершины опорных точек, индексы вершин опорных точек
                // }
                
                unsafe
                {
                    #region Меш

                    fixed (Vertex* ptr = &Vertices[0])
                    {
                        gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[0]);
                        gl.BufferData(OpenGL.GL_ARRAY_BUFFER,
                            Vertices.Length * sizeof(Vertex),
                            (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                    }
                    fixed (uint* ptr = &Indices[0])
                    {
                        gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, VBO[1]);
                        gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
                            Indices.Length * sizeof(uint),
                            (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                    }

                    #endregion

                    #region Оси

                    fixed (Vertex* ptr = &AxesVertices[0])
                    {
                        gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[2]);
                        gl.BufferData(OpenGL.GL_ARRAY_BUFFER,
                            AxesVertices.Length * sizeof(Vertex),
                            (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                    }
                    fixed (uint* ptr = &AxesIndices[0])
                    {
                        gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, VBO[3]);
                        gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
                            AxesIndices.Length * sizeof(uint),
                            (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                    }

                    #endregion
                    
                    #region Опорные точки

                    fixed (Vertex* ptr = &PointsVertices[0])
                    {
                        gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[4]);
                        gl.BufferData(OpenGL.GL_ARRAY_BUFFER,
                            PointsVertices.Length * sizeof(Vertex),
                            (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                    }
                    fixed (uint* ptr = &PointsIndices[0])
                    {
                        gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, VBO[5]);
                        gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
                            PointsIndices.Length * sizeof(uint),
                            (IntPtr)ptr, OpenGL.GL_STATIC_DRAW);
                    }

                    #endregion
                }
            });
        }
    
        // матрица поворота
        private static DMatrix4 RotationMatrix(double x, double y, double z)
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
        
        private static DMatrix4 RotationMatrix(DVector3 rotation)
        {
            return RotationMatrix(rotation.X, rotation.Y, rotation.Z);
        }
    
        // матрица сдвига
        private static DMatrix4 ShiftMatrix(DVector3 shift)
        {
            var shiftMatrix = DMatrix4.Identity;
            shiftMatrix.M14 = shift.X;
            shiftMatrix.M24 = shift.Y;
            shiftMatrix.M34 = shift.Z;
            return shiftMatrix;
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
            if (Math.Abs(nearPlane - farPlane) < .000001 || aspectRatio == 0 || sine == 0)
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
    
        // добавление элемента управления перед элементом insertBeforeProperty
        private void AddControl(Control ctrl, int height, string insertBeforeProperty)
        {
            if (!(ValueStorage.Controls[0].Controls[0] is TableLayoutPanel layout)) return;
            layout.SuspendLayout();
            ctrl.Dock = DockStyle.Fill;
            layout.Parent.Height += height;
            var beforectrl = ValueStorage.GetControlForProperty(insertBeforeProperty);
            var position = layout.GetPositionFromControl(beforectrl).Row + 1;
            for (int r = layout.RowCount; position <= r--;)
            {
                for (int c = layout.ColumnCount; 0 != c--;)
                {
                    var control = layout.GetControlFromPosition(c, r);
                    if (control != null) layout.SetRow(control, r + 1);
                }
            }

            layout.RowStyles.Insert(position - 1, new RowStyle(SizeType.Absolute, height));
            layout.Controls.Add(ctrl, 0, position - 1);
            layout.SetColumnSpan(ctrl, 2);
            layout.RowCount++;
            layout.ResumeLayout(true);
        }
    
        // добавление элемента управления в конец
        private void AddControl(Control ctrl, int height)
        {
            if (!(ValueStorage.Controls[0].Controls[0] is TableLayoutPanel layout)) return;
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
        private void LoadPoints(IEnumerable<string> lines)
        {
            // регулярное выражение строки с координатами точки
            var pattern = new Regex(@"^(\s*[\-\+]?\d+(\.\d+)?){3}\s*(#.*)?$");
            var points = new List<DVector3>();
            var i = 0;
            foreach (var line in lines)
            {
                i++;
                if (line == "" || line[0] == '#') // пустые строки и комментарии игнорируем
                    continue;
                if (!pattern.IsMatch(line)) // выдаём ошибку, если строка не удовлетворяет синтаксису
                    throw new Exception($"Ошибка парсинга: неверный синтаксис в строке #{i}");

                // вытаскиваем три числа из строки
                var match = pattern.Match(line).Groups[1].Captures;
                var result = new double[3]; // массив координат точки
                for (int j = 0; j < 3; j++)
                {
                    // заполняем координаты точки, если они являются валидными числами, иначе выдаём ошибку
                    if (!double.TryParse(match[j].Value, out result[j]))
                        throw new Exception($"Ошибка парсинга: неверный формат числа в строке #{i}");
                }

                // добавляем точку в список
                points.Add(new DVector3(result[0], result[1], result[2]));
            }

            // ошибка, если точек оказалось меньше или больше 12
            if (points.Count != 12)
                throw new Exception($"Ошибка парсинга: неверное число точек ({points.Count}/12)");

            // устанавливаем значения свойств
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
        
            // генерируем поверхность с новыми опорными точками
            lock (RenderDevice.LockObj)
            {
                MakeSurface();
            }
        }

    }
}