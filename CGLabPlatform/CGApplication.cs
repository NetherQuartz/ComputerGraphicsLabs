using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CGLabPlatform
{
    public abstract class GFXApplicationTemplate<T> : CGApplicationTemplate<T, GDIDevice, GDIDeviceUpdateArgs> 
        where T : GFXApplicationTemplate<T> { }

    public abstract class OGLApplicationTemplate<T> : CGApplicationTemplate<T, OGLDevice, OGLDeviceUpdateArgs> 
        where T : OGLApplicationTemplate<T> { }

    public abstract class CGApplicationTemplate<T, D, A> : Bindable<T>
        where T : CGApplicationTemplate<T, D, A>
        where D : DrawDevice<A>, new()
        where A : IDeviceUpdateArgs
    {
        public static void RunApplication() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(CreateInstance().MainWindow);
        }

        public Form         MainWindow   { get; private set; }
        public D            RenderDevice { get; private set; }
        public ValueStorage ValueStorage { get; private set; }
        public SplitContainer Container  { get; private set; }

        /// <summary>
        /// Ширина панели свойств
        /// </summary>
        public int VSPanelWidth {
            get { return _VSPanelLeft ? Container.SplitterDistance 
                : (Container.ClientSize.Width - Container.SplitterDistance); }
            set { Container.SplitterDistance = _VSPanelLeft ? value
                : (Container.ClientSize.Width - value);
            }
        }

        /// <summary>
        /// Ориентация панели свойств. Если true, то слева, иначе - справа
        /// </summary>
        public bool VSPanelLeft {
            get { return _VSPanelLeft; }
            set { 
                var width = VSPanelWidth;
                _VSPanelLeft = value;
                Container.Panel1.Controls.Clear();
                Container.Panel2.Controls.Clear();
                Container.Panel1.Controls.Add(value?(Control)ValueStorage:RenderDevice);
                Container.Panel2.Controls.Add(value?(Control)RenderDevice:ValueStorage);
                Container.FixedPanel = value ? FixedPanel.Panel1 : FixedPanel.Panel2;
                VSPanelWidth = width;
            }
        }
        private bool _VSPanelLeft = false;

        protected CGApplicationTemplate()
        {
            MainWindow = new Form();
            MainWindow.Text = "Computer Graphics - GFX";
            MainWindow.Size = new Size(800, 600);

            RenderDevice = (D)Activator.CreateInstance(typeof(D));
            RenderDevice.Dock = DockStyle.Fill;
            RenderDevice.BackColor = Color.LightSteelBlue;
            ValueStorage = new ValueStorage() { Dock = DockStyle.Fill, Storage = this, Padding = new Padding(8) };
            Container = new SplitContainer() { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0),
                                                   FixedPanel = FixedPanel.Panel2, SplitterWidth = 4 };
            MainWindow.Controls.Add(Container);
            Container.Panel1.Controls.Add(RenderDevice);
            Container.Panel2.Controls.Add(ValueStorage);
            Container.SplitterDistance = Container.ClientSize.Width - 200;
            Container.Panel2.BackColor = Color.WhiteSmoke;

            MainWindow.Load += (s, e) => { OnMainWindowLoad(s, e); ValueStorage.UpdateLayout(); };
            RenderDevice.DeviceUpdate += OnDeviceUpdate;

            IntPtr large, small;
            shell32.ExtractIconEx(Application.ExecutablePath, 0, out large, out small, 1);
            MainWindow.Icon = large != IntPtr.Zero ? Icon.FromHandle(large) : null;
        }

        

        protected virtual void OnMainWindowLoad(object sender, EventArgs e)
        {
        }

        protected virtual void OnDeviceUpdate(object s, A e)
        {   
        }
    }

    internal static class shell32
    {
        [DllImport("shell32.dll")]
        public static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion,
                                                out IntPtr piSmallVersion, int amountIcons);
    }
}
