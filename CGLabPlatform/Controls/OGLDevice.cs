using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SharpGL;
using SharpGL.RenderContextProviders;
using SharpGL.Version;

namespace CGLabPlatform
{
    [System.ComponentModel.DesignerCategory("")]
    public class OGLDevice : DrawDevice<OGLDeviceUpdateArgs>
    {
        private readonly OpenGL gl = new OpenGL();
        private OpenGLVersion     _glVersion;
        private RenderContextType _glRenderContextType;
        private bool          _Disposed;
        private Thread        _Thread;
        
        /// <summary>
        /// Объект для синхронизации потока отрисовки
        /// </summary>
        public readonly object LockObj = new object();

        public readonly SynchronizationContext uiContext;
        public readonly SynchronizationContext glContext;

        #region Events

        public override event EventHandler<OGLDeviceUpdateArgs> DeviceUpdate;

        /// <summary>
        /// Возбуждается при изменении размера устройства OGL. Может быть использованно для замены проецирования 
        /// </summary>
        [Category("CGLabPlatform"), Description("Возбуждается при изменении размера устройства OGL. Может быть использованно для замены проецирования.")]
        public event OGLResizedEventHandler Resized;

        /// <summary>
        /// Возбуждается при создании устройства OGL. Может быть использованно для предварительной настройки
        /// </summary>
        public event OGLEventHandler Inited;

        /// <summary>
        /// Возбуждается перед завершением работы устройства OGL. Может быть использованно для высвобождения ресурсов OGL
        /// </summary>
        public event OGLEventHandler Closed;

        #endregion

        /// <summary>
        /// Добавляет задание в очередь FIFO, обратываемую перед обновлением устройства<para/>
        /// в потоке связанным с OGL. (Сам метод является асинхронным, т.е.<para/>
        /// приостановления вызывающего потока до обратки задания не происходит)
        /// </summary>
        /// <param name="callback">Делегат обратного вызова задания</param>
        /// <param name="state">Пользовательский объект передаваемый вызываемому методу</param>
        public void AddScheduleTask(OGLScheduleTask callback, object state = null) {
            glContext.Post(d => callback(gl, d), state);
        }

        #region Properties

        /// <summary>
        /// Возвращает объект OpenGL.
        /// </summary>
        public OpenGL OpenGL { get { return gl; } }

        public bool Disposed { get { return _Disposed; } }

        /// <summary>
        /// Возвращает или задает тип контекста рендера.<para/>
        /// Не рекомендуется изменять значение по умолчанию (NativeWindow).
        /// </summary>
        public RenderContextType RenderContextType {
            get { return _glRenderContextType; }
            set {
                if (gl.RenderContextProvider != null)
                    throw new Exception("Изменение значения после инициализации OpenGL не предусмотренно");
                else _glRenderContextType = value; 
                
            }
        }

        /// <summary>
        /// Возвращает или задает версию OpenGL.
        /// </summary>
        public OpenGLVersion OpenGLVersion {
            get { return _glVersion; }
            set {
                if (gl.RenderContextProvider != null)
                    throw new Exception("Изменение значения после инициализации OpenGL не предусмотренно");
                else _glVersion = value;
            }
        }



        /// <summary>
        /// Вертикальная синхронизация — синхронизация частоты кадров с частотой вертикальной развёртки монитора.<para/>
        /// При этом максимальный FPS с вертикальной синхронизацией приравнивается к частоте обновления монитора.<para/>
        /// Значение 0 - выкл, 1 - вкл, 2 - 1/2 частоты обновления монитора (30 FPS при 60Гц), 3 - 1/3 ..., и тд.<para/>
        /// Если расширение не поддерживается возвращается значение меньше нуля.<para/>
        /// </summary>
        public int VSync {
            get {
                return glContext.Send(() => gl.IsExtensionFunctionSupported
                    ("wglGetSwapIntervalEXT") ? gl.GetSwapIntervalEXT() : -1);
            }
            set {
                glContext.Post(() => { if (gl.IsExtensionFunctionSupported
                    ("wglSwapIntervalEXT")) gl.SwapIntervalEXT(value); } );
            }
        }

        /// <summary>
        /// Сглаживание точек (резульатат зависит от смешивания цветов, задаваемым glBlend)
        /// </summary>
        public bool SmoothPoint {
            get {
                return glContext.Send(() => { var r = new byte[1]; 
                    gl.GetBooleanv(OpenGL.GL_POINT_SMOOTH, r);
                    return r[0] == 1; } );
            }
            set {
                glContext.Post(enable => { if ((bool)enable) {
                        gl.Enable(OpenGL.GL_POINT_SMOOTH);
                        gl.Hint(OpenGL.GL_POINT_SMOOTH_HINT, OpenGL.GL_NICEST);
                    } else gl.Disable(OpenGL.GL_POINT_SMOOTH); }, value );
            }
        }

        /// <summary>
        /// Сглаживание линий (резульатат зависит от смешивания цветов, задаваемым glBlend)
        /// </summary>
        public bool SmoothLine {
            get {
                return glContext.Send(() => { var r = new byte[1]; 
                    gl.GetBooleanv(OpenGL.GL_LINE_SMOOTH, r);
                    return r[0] == 1; } );
            }
            set {
                glContext.Post(enable => { if ((bool)enable) {
                        gl.Enable(OpenGL.GL_LINE_SMOOTH);
                        gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
                    } else gl.Disable(OpenGL.GL_LINE_SMOOTH); }, value );
            }
        }

        /// <summary>
        /// Сглаживание полигонов (резульатат зависит от смешивания цветов, задаваемым glBlend)<para/>
        /// Данный метод сглаживания не является рекомендуемым к применению
        /// </summary>
        public bool SmoothPolygon {
            get {
                return glContext.Send(() => { var r = new byte[1]; 
                    gl.GetBooleanv(OpenGL.GL_POLYGON_SMOOTH, r);
                    return r[0] == 1; } );
            }
            set {
                glContext.Post(enable => { if ((bool)enable) {
                        gl.Enable(OpenGL.GL_POLYGON_SMOOTH);
                        gl.Hint(OpenGL.GL_POLYGON_SMOOTH_HINT, OpenGL.GL_NICEST);
                    } else gl.Disable(OpenGL.GL_POLYGON_SMOOTH); }, value );
            }
        }

        /// <summary>
        /// Сглаживание Multisample
        /// </summary>
        public bool Multisample {
            // TODO: Для работы сглаживания требуется пересоздать с новым форматом пикселя.
            get {
                return glContext.Send(() => { byte[] r1 = new byte[1], r2 = new byte[1] {0}; 
                    gl.GetBooleanv(OpenGL.GL_MULTISAMPLE_ARB, r1);
                    return r1[0] == 1 || r2[0] == 1; } );
            }
            set {
                glContext.Post(enable => { if ((bool)enable) {
                        gl.Enable(OpenGL.GL_MULTISAMPLE_ARB);
                    } else  {
                        gl.Disable(OpenGL.GL_MULTISAMPLE_ARB);
                    } }, value);
            }
        }

        #endregion


        protected override void Dispose(bool disposing)
        {
            lock (this) {
                _Disposed = true;
                _Thread.Abort();
                OpenGL.RenderContextProvider.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "OGLDevice";
            this.ResumeLayout(false);
        }

        private int ogl_resize_request_param = -1;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_SIZE) {
                var param = m.LParam.ToInt32();
                var width = param & 0xFFFF;
                var heigh = param >> 16;
                var rendc = gl.RenderContextProvider;
                if (rendc != null && (rendc.Width != width || rendc.Height != heigh))
                    ogl_resize_request_param = param;
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        private void Initialize(int width, int height, bool initialized = true)
        {
            if (initialized)
                uiContext.Send(d => initialized = width == Width && height == Height, null);
            if (initialized || _Disposed || OpenGL.RenderContextProvider == null)
                return;

            OpenGL.SetDimensions(width, height);           
            gl.Viewport(0, 0, width, height);
            if (width == -1 || height == -1)
                return;

            if (Resized != null)
                Resized(this, new OGLResizedEventArgs(gl, width, height));
            else { // Если нет собственного события Resized используем проекцию по умолчанию:
                OpenGL.MakeCurrent();
                gl.MatrixMode(OpenGL.GL_PROJECTION);
                gl.LoadIdentity();

                gl.Perspective(45.0f, (float)width / (float)height, 0.1f, 100.0f);

                gl.MatrixMode(OpenGL.GL_MODELVIEW);
                gl.LoadIdentity();
            }
        }


        public OGLDevice()
        {
            InitializeComponent();
            _glVersion = OpenGLVersion.OpenGL2_1;
            _glRenderContextType = RenderContextType.NativeWindow;

            ResizeRedraw = false;
            MinimumSize = new Size(1, 1);
            SetStyle( ControlStyles.DoubleBuffer,           false );
            SetStyle( ControlStyles.OptimizedDoubleBuffer,  false ); // а надо ли
            SetStyle( ControlStyles.UserPaint,              true  );
            SetStyle( ControlStyles.AllPaintingInWmPaint,   true  );
            SetStyle( ControlStyles.Opaque,                 true  );
            SetStyle( ControlStyles.ResizeRedraw,           false );

            _Thread = new Thread(RenderLoop) { Name = "OGL Render Loop" };
            glContext = new QueueSynchronizationContext(_Thread);
            uiContext = SynchronizationContext.Current; // NOTE: Сделанно допущение, что объект всегда создается из потока UI   
            HandleDestroyed += (sender, args) => {
                if (Closed != null) {
                    lock (LockObj) DeviceUpdate = null; // Нет смысла вызывать обновление устройства после обратки события
                    glContext.Send(d => Closed(sender, new OpenGLEventArgs(OpenGL)), null);
                }
                _Thread.Abort();
                lock (LockObj) ;
            };
            _Thread.Start();
        }

        private void RenderLoop()
        {
            var queuecontext = glContext as QueueSynchronizationContext;
            SynchronizationContext.SetSynchronizationContext(glContext);

            int fps_count = 0;
            int fps_ticks = 0;
            int pre_ticks = Environment.TickCount & Int32.MaxValue;
            var updatearg = new OGLDeviceUpdateArgs(gl);

            int width = -1, height = -1; IntPtr handle = IntPtr.Zero;
            uiContext.Send(d => {   // WindowsFormsSynchronizationContext обрабатывает синхронные сообщения в 
                width  = Width;     // общем обработчике сообщений окна (WndProc). Это означает, что главный
                height = Height;    // поток обратает синхронное сообщение лишь после того как завершится
                handle = Handle;    // выполнение текущего кода. Таким образом в неявном виде тут происходит
            }, null);               // синхронизация потоков - поток ожидаяет выполнения OnMainWindowLoad()

            // Создание и работа с устройством OpenGL должны осуществляться в одном потоке
            gl.Create(_glVersion, _glRenderContextType, width, height, 32, handle);
            _glVersion = ((RenderContextProvider)gl.RenderContextProvider).CreatedOpenGLVersion;
            
            // Установка наиболее распространенных стилей
            gl.ShadeModel(OpenGL.GL_SMOOTH);

            gl.ClearColor(0.5f, 0.5f, 0.5f, 0.0f);
            gl.ClearDepth(1.0f);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.DepthFunc(OpenGL.GL_LEQUAL);

            gl.Enable(OpenGL.GL_BLEND); // Правило наложения (смешивания) цветов
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA); // result_color = src * src.a + dst * (1 - src.a)

            Initialize(Width, Height, false);   // задаем проецирование 
            OpenGL.MakeCurrent();

            if (Inited != null)
                Inited(this, new OpenGLEventArgs(gl));

            while (true)
            {
                queuecontext.Run();
                if (ogl_resize_request_param != -1) {
                    Initialize(ogl_resize_request_param & 0xFFFF, ogl_resize_request_param >> 16, false);
                    ogl_resize_request_param = -1;
                }

                var cur_ticks = Environment.TickCount & Int32.MaxValue;
                if (cur_ticks - fps_ticks > 1000) {
                    uiContext.Post(s => {
                        if (ParentForm != null)
                            ParentForm.Text = String.Format("FPS: {0}", (int)s);
                    }, fps_count);
                    fps_count = 1;
                    fps_ticks = cur_ticks;
                } else
                    ++fps_count;

                lock (LockObj) {
                    if (DeviceUpdate != null) {
                        DeviceUpdate(this, updatearg.Update(cur_ticks - pre_ticks));

                        gl.Flush();
                        OpenGL.Blit(handle);    // Для ContextType.NativeWindow тут выполняется переключение буфферов,                                                 
                        pre_ticks = cur_ticks;  // сам же параметр handle при этом вообще никак и ни где не используется.
                    }
                }

                Thread.Sleep(1);
            }
        }


        
    }


    public delegate void OGLResizedEventHandler(object sender, OGLResizedEventArgs args);
    public delegate void OGLEventHandler(object sender, OpenGLEventArgs args);
    public delegate void OGLScheduleTask(OpenGL gl, object state);

    public class OpenGLEventArgs
    {
        public readonly OpenGL gl;
        internal OpenGLEventArgs(OpenGL gl) {
            this.gl = gl;
        }
    }

    public class OGLDeviceUpdateArgs : OpenGLEventArgs, IDeviceUpdateArgs
    {
        public double           Width { get { return gl.RenderContextProvider.Width;  } }
        public double           Heigh { get { return gl.RenderContextProvider.Height; } }
        public int              Delta { get; private set; }

        internal OGLDeviceUpdateArgs(OpenGL gl) : base(gl) {
        }

        internal OGLDeviceUpdateArgs Update(int delta) {
            Delta = delta;
            return this;
        }
    }

    public class OGLResizedEventArgs : OpenGLEventArgs
    {
        public int Width  { get; private set; }
        public int Height { get; private set; }

        public OGLResizedEventArgs(OpenGL gl, int width, int height) :  base(gl) {
            Width  = width;
            Height = height;
        }
    }

}
