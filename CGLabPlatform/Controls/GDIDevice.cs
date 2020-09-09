using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GDIGraphics = System.Drawing.Graphics;

namespace CGLabPlatform
{
    public class GDIDeviceUpdateArgs : IDeviceUpdateArgs
    {
        /// <summary>
        /// Объект Graphics поверхности (Surface) буфера элемента управления GDIDevice
        /// </summary>
        public Graphics         Graphics { get; private set; }

        /// <summary>
        /// Поверхность буфера элемента управления GDIDevice
        /// </summary>
        public BitmapSurface    Surface  { get; private set; }

        /// <summary>
        /// Ширина буфера в пикселях элемента управления GDIDevice
        /// </summary>
        public double           Width { get { return Surface.Width;  } }

        /// <summary>
        /// Высота буфера в пикселях элемента управления GDIDevice
        /// </summary>
        public double           Heigh { get { return Surface.Height; } }

        /// <summary>
        /// Количество миллисекунд прошедших с предыдущего вызова обновления устройства
        /// </summary>
        public int              Delta { get; private set; }

        internal GDIDeviceUpdateArgs()
        {
        }

        internal GDIDeviceUpdateArgs Update(GDIDevice gdidevice, int delta)
        {
            Graphics = gdidevice.GFX;
            Surface  = gdidevice.DIB;
            Delta = delta;
            return this;
        }
    }

    [System.ComponentModel.DesignerCategory("")]
    public class GDIDevice : DrawDevice<GDIDeviceUpdateArgs>
    {
        #region Properties

        /// <summary>
        /// Возвращает или задает значение, показывающее, очищается ли буфер перед обновлением
        /// </summary>
        public bool BufferClearEnable {
            get { return _clearbuf;  }
            set { _clearbuf = value; }
        }

        /// <summary>
        /// Возвращает или задает значение оттенка серого цвета используемого для очистки буфера
        /// </summary>
        public byte BufferBackCol {
            get { return _clearcol; }
            set { _clearcol = value; }
        }

        /// <summary>
        /// Возвращает или задает значение, показывающее, используются ли оптимизированные для повышения<para/>
        /// производительности настройки качества отрисовки поверхностей буфера 
        /// </summary>
        public bool GraphicsHighSpeed {
            get { return _dibdmode == SurfaceDrawQuality.Low; }
            set {
                _gfxcompq = value ? CompositingQuality.HighSpeed : CompositingQuality.HighQuality;
                _gfxcompm = value ? CompositingMode.SourceCopy : CompositingMode.SourceOver;
                _gfxsmoth = value ? SmoothingMode.None : SmoothingMode.HighQuality;
                _dibdmode = value ? SurfaceDrawQuality.Low : SurfaceDrawQuality.High;         
                if (_DIB != null && !_DIB.Disposed) {
                    _DIB.DrawQuality = _dibdmode;
                    if (_GFX != null) {
                        _GFX.CompositingQuality = _gfxcompq;
                        _GFX.CompositingMode    = _gfxcompm;
                        _GFX.SmoothingMode      = _gfxsmoth;
                    }
                }
            }
        }

        #endregion

        public override event EventHandler<GDIDeviceUpdateArgs> DeviceUpdate;

        private readonly HandleRef hDCRef;
        private readonly Graphics  hDCGFX;
        private BITMAPINFO    _BI;
        private BitmapSurface _DIB;
        private GDIGraphics   _GFX;
        private bool          _Disposed;
        private Thread        _Thread;
        /// <summary>
        /// Объект для синхронизации потока отрисовки
        /// </summary>
        public readonly object LockObj = new object();
        private byte          _clearcol = 0x80;
        private bool          _clearbuf = true;
        private SurfaceDrawQuality _dibdmode = SurfaceDrawQuality.High;
        private CompositingQuality _gfxcompq = CompositingQuality.HighQuality;
        private CompositingMode    _gfxcompm = CompositingMode.SourceOver;
        private SmoothingMode      _gfxsmoth = SmoothingMode.HighQuality;

        public readonly SynchronizationContext uiContext;

        public BitmapSurface DIB { get { return _DIB; } }
        public GDIGraphics   GFX { get { return _GFX; } }
        public bool     Disposed { get { return _Disposed || DIB.Disposed; } }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "GDIDevice";
            this.ResumeLayout(false);
        }

        private void Initialize(int width, int height)
        {
            if (width == _BI.biHeader.bihWidth && height == -_BI.biHeader.bihHeight)
                return;
            
            lock (LockObj)
            {
                if (_GFX != null) _GFX.Dispose();
                if (_DIB != null) _DIB.Dispose();

                _DIB = new BitmapSurface(width, height);
                _GFX = GDIGraphics.FromImage(_DIB.BmpSource);
                _GFX.CompositingQuality = _gfxcompq;
                _GFX.CompositingMode = _gfxcompm;
                _GFX.SmoothingMode = _gfxsmoth;
                _DIB.DrawQuality = _dibdmode;

                _BI  = new BITMAPINFO {
				    biHeader = {
					    bihBitCount = 32,
					    bihPlanes   = 1,
					    bihSize     = 40,
					    bihWidth    = +width,
					    bihHeight   = -height,
                        bihSizeImage = (width * height) << 2
				    }
			    };
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (this) {
                _Disposed = true;
                if (_GFX != null) _GFX.Dispose();
                if (_DIB != null) _DIB.Dispose();
                if (hDCGFX != null) hDCGFX.Dispose();
            }
            base.Dispose(disposing);
        }


        public void ClearBuffer()
        {
            MemSet._memset(DIB.ImageData, _clearcol, DIB.lpsize);
        }

        private unsafe void RenderLoop()
        {
            int fps_count = 0;
            int fps_ticks = 0;
            int pre_ticks = Environment.TickCount & Int32.MaxValue;
            var updatearg = new GDIDeviceUpdateArgs();

            while (true) 
            {
                var cur_ticks = Environment.TickCount & Int32.MaxValue;
                if (cur_ticks - fps_ticks > 1000) {
                    uiContext.Send(d => {
                        if (ParentForm != null)
                            ParentForm.Text = String.Format("FPS: {0}", fps_count);
                    }, null);
                    fps_count = 1;
                    fps_ticks = cur_ticks;      
                } else
                    ++fps_count;

                lock (LockObj) {
                    if (DeviceUpdate != null) {
                        if (_clearbuf)
                            MemSet._memset(DIB.ImageData, _clearcol, DIB.lpsize);
                        DeviceUpdate(this, updatearg.Update(this, cur_ticks - pre_ticks));
                        pre_ticks = cur_ticks;
                    }

                    SetDIBitsToDevice(hDCRef, 0, 0, Width, Height, 0, 0, 0, Height, _DIB.ImageBits, ref _BI, 0);
                }

                Thread.Sleep(1);
            } 
        }


        [StructLayout(LayoutKind.Sequential)]
		private struct BITMAPINFO
		{
			public BITMAPINFOHEADER biHeader;
			public int              biColors;
		}

        [StructLayout(LayoutKind.Sequential)]
		private struct BITMAPINFOHEADER
		{
			public int		bihSize;
			public int		bihWidth;
			public int		bihHeight;
			public short	bihPlanes;
			public short	bihBitCount;
			public int		bihCompression;
			public int		bihSizeImage;
			public double	bihXPelsPerMeter;
			public double	bihClrUsed;
		}

        [DllImport("gdi32")]
        private extern static unsafe int SetDIBitsToDevice(HandleRef hDC, int xDest, int yDest, int dwWidth, int dwHeight, 
            int XSrc, int YSrc, int uStartScan, int cScanLines, byte* lpvBits, ref BITMAPINFO lpbmi, uint fuColorUse);

        private static class MemSet
        {
            public static Action<IntPtr, byte, int> _memset;
            static MemSet()
            {
                var method = new DynamicMethod("Memset", MethodAttributes.Public | MethodAttributes.Static, 
                    CallingConventions.Standard, null, new[] { typeof(IntPtr), typeof(byte), typeof(int) }, 
                    typeof(MemSet), true);

                var il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Initblk);
                    il.Emit(OpCodes.Ret);
                _memset = (Action<IntPtr, byte, int>)method.CreateDelegate(typeof(Action<IntPtr, byte, int>));
            }
        }

   
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0005) {  // WM_SIZE
                var param = m.LParam.ToInt32();
                var width = param & 0xFFFF;
                var heigh = param >> 16;
                Initialize(width, heigh);
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            Initialize(Width, Height);
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        public GDIDevice()
        {
            InitializeComponent();

            ResizeRedraw = false;
            MinimumSize = new Size(1, 1);
            SetStyle( ControlStyles.DoubleBuffer,           false );
            SetStyle( ControlStyles.UserPaint,              true  );
            SetStyle( ControlStyles.AllPaintingInWmPaint,   true  );
            SetStyle( ControlStyles.Opaque,                 true  );
            SetStyle( ControlStyles.ResizeRedraw,           false );

            hDCGFX = CreateGraphics();
            hDCRef = new HandleRef(hDCGFX, hDCGFX.GetHdc());
            
            uiContext = SynchronizationContext.Current; // NOTE: Сделанно допущение, что объект всегда создается из потока UI 
            _Thread = new Thread(RenderLoop) { Name = "GDI Render Loop" };
            _Thread.Start(); 
            HandleDestroyed += (sender, args) => {
                _Thread.Abort();
                lock (LockObj) ;
            };
        }

    }


    public unsafe class BitmapSurface : IDisposable
    {
        public readonly int       stride;
        public readonly int       lpsize;
        private  IntPtr           _section = IntPtr.Zero;
        internal byte*            ImageBits;
        internal IntPtr           ImageData { get; private set; }
        internal Bitmap           BmpSource { get; private set; }
        internal readonly object  LockObject = new object();

        public bool Disposed { get; private set; }
        public int Width  { get; private set; }
        public int Height { get; private set; }
        internal readonly Drawing.Clipper Clipper;

        void Lock()
        {
            Monitor.Enter(LockObject);
        }

        void Unlock()
        {
            Monitor.Exit(LockObject);
        }

        public BitmapSurface(int width, int height)
        {
            DrawQuality = SurfaceDrawQuality.High;
            if (height <= 0 || width <= 0)
                throw new ArgumentOutOfRangeException();
            Disposed = false; 
            Height = height;
            Width  = width;
            Clipper = new Drawing.Clipper(this);

            stride = width << 2;
            lpsize = stride * height;

            var lplen = lpsize + stride + 4; // небольшой запас для ву, чтобы не возиться с граничными условиями
            _section  = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, lplen, null);
            ImageBits = (byte*)(ImageData = MapViewOfFile(_section, 0xF001F, 0, 0, lplen));
            BmpSource = new Bitmap(width, height, stride, PixelFormat.Format32bppRgb, ImageData);
        }

        public BitmapSurface(BitmapSurface surface, int width, int height) : this(width, height)
        {
            if (BmpSource.PixelFormat != surface.BmpSource.PixelFormat)
                throw new ArgumentException("У исходной поверхности должен быть тот же формат пикселя");
            lock (surface.LockObject) {
                lock (LockObject) {
                    var count = Math.Min(stride, surface.stride);
                    var lines = Math.Min(Height, surface.Height);
                    var block = lines / Environment.ProcessorCount;
                    Parallel.For(0, Environment.ProcessorCount, (int i) => {
                        var srcptr = surface.ImageBits + surface.stride * block * i - surface.stride;
                        var dstptr = ImageBits + stride * block * i - stride;
                        for (int j = block; j > 0; --j)
                            #if NETFX_46
                            Buffer.MemoryCopy(srcptr += surface.stride, dstptr += stride, stride, count);
                            #else
                            MemCpy._memcpy_uu(dstptr += stride, srcptr += surface.stride, count);
                            #endif       
                    });
                    lines -= block * Environment.ProcessorCount;
                    block *= Environment.ProcessorCount;
                    var srclpi = surface.ImageBits + surface.stride * (block - 1);
                    var dstlpi = ImageBits + stride * (block - 1);
                    for (int j = lines; j > 0; --j)
                        #if NETFX_46
                        Buffer.MemoryCopy(srclpi += surface.stride, dstlpi += stride, stride, count);
                        #else
                        MemCpy._memcpy_uu(dstlpi += stride, srclpi += surface.stride, count);
                        #endif    
                }
            }   
        }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, int dwNumberOfBytesToMap);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr hMap);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        private static class MemCpy
        {
            public static Action<IntPtr, byte, int> _memset;
            public unsafe delegate void _memcpy_uu_Delegate(void* dst, void* src, int bytes);

            public static readonly _memcpy_uu_Delegate _memcpy_uu;

            static MemCpy()
            {
                var method = new DynamicMethod("Memset", MethodAttributes.Public | MethodAttributes.Static, 
                    CallingConventions.Standard, null, new[] { typeof(IntPtr), typeof(byte), typeof(int) },
                    typeof(MemCpy), true);

                var il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Cpblk);
                    il.Emit(OpCodes.Ret);
                    _memcpy_uu = (_memcpy_uu_Delegate)method.CreateDelegate(typeof(_memcpy_uu_Delegate));
            }
        }

        ~BitmapSurface()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected void Dispose(bool disposing)
        {
            Disposed = true;
            lock (LockObject) {
                if (disposing) {
                    // mng ...
                }
                if (ImageData != IntPtr.Zero) {
                    UnmapViewOfFile(ImageData);
                    ImageData = IntPtr.Zero;
                }
                if (_section != IntPtr.Zero) {
                    CloseHandle(_section);
                    _section = IntPtr.Zero;
                }
            }
        }

        public SurfaceDrawQuality DrawQuality { get; set; }

        public int this[int x, int y] {
            get { return *(int*) (ImageBits + y*stride + (x << 2)); }
            set { *(int*)(ImageBits + y * stride + (x << 2)) = value; }
        }

    }

    public enum SurfaceDrawQuality
    {
        Low, High
    }
}
