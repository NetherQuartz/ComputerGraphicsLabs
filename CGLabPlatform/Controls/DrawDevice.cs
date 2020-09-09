using System;
using System.Drawing;
using System.Windows.Forms;

namespace CGLabPlatform
{
    public class MouseExEventArgs
    {
        /// <summary>
        /// Возвращает константу, определяющуюу, зажатую кнопку мыши, связанную с этим событием.
        /// </summary>
        public MouseButtons PressedButton { get; private set; }

        /// <summary>
        /// Возвращает расположение указателя мыши в момент зажатия кнопки мыши, определяемой свойством PressedButton.
        /// </summary>
        public Point PressedLocation { get; private set; }

        /// <summary>
        /// Возвращает расположение указателя мыши в момент создания события мыши.
        /// </summary>
        public Point Location { get; private set; }

        /// <summary>
        /// Возвращает смещение указателя мыши по оси X в момент создания события мыши от расположения в котором была зажата кнопка мыши.
        /// </summary>
        public int DistanceX { get { return Location.X - PressedLocation.X; } }

        /// <summary>
        /// Возвращает смещение указателя мыши по оси Y в момент создания события мыши от расположения в котором была зажата кнопка мыши.
        /// </summary>
        public int DistanceY { get { return Location.Y - PressedLocation.Y; } }

        /// <summary>
        /// Возвращает изменение положения указателя мыши по оси X в момент создания события мыши.
        /// </summary>
        public int MovDeltaX { get { return Location.X - PrevLocation.X; } }

        /// <summary>
        /// Возвращает изменение положения указателя мыши по оси Y в момент создания события мыши.
        /// </summary>
        public int MovDeltaY { get { return Location.Y - PrevLocation.Y; } }

        /// <summary>
        /// Возвращает расстояние между указателем мыши в момент создания события мыши и расположением в котором была зажата кнопка мыши.
        /// </summary>
        public double Distance  { get { return Math.Sqrt(DistanceX * DistanceX + DistanceY * DistanceY); } }

        /// <summary>
        /// Возвращает расстояние на которое изменилось положение указателя мыши в момент создания события мыши
        /// </summary>
        public double MovDelta  { get { return Math.Sqrt(MovDeltaX * MovDeltaX + MovDeltaY * MovDeltaY); } }

        public double RotAngle  { get { 
            return 180.0/Math.PI*(Math.Atan2(X - PressedLocation.X, Y - PressedLocation.Y) -
                                  Math.Atan2(PrevLocation.X - PressedLocation.X, PrevLocation.Y - PressedLocation.Y));
        } }

        /// <summary>
        /// Возвращает координату X указателя мыши в момент создания события мыши.
        /// </summary>
        public int X { get { return Location.X; } }

        /// <summary>
        /// Возвращает координату Y указателя мыши в момент создания события мыши.
        /// </summary>
        public int Y { get { return Location.Y; } }

        public double GetRotAngle(int cx, int cy)
        {
            return  180.0 / Math.PI * (Math.Atan2(X - cx, Y - cy) -
                Math.Atan2(PrevLocation.X - cx, PrevLocation.Y - cy));
        }

        private Point PrevLocation;

        internal bool _IsPressed = false;

        internal MouseExEventArgs(MouseButtons btn)
        {
            PressedButton = btn;
        }

        internal void Init(MouseEventArgs args)
        {
            PressedLocation = PrevLocation = Location = args.Location;
            _IsPressed = true;
        }

        internal MouseExEventArgs Update(MouseEventArgs args)
        {
            PrevLocation = Location;
            Location  = args.Location;
            return this;
        }
    }

    public interface IDeviceUpdateArgs { }

    [System.ComponentModel.DesignerCategory("")]
    public abstract class DrawDevice<A> : UserControl where A : IDeviceUpdateArgs
    {
        public virtual event EventHandler<A> DeviceUpdate;

        //public event EventHandler<Progress> Progress;

        /// <summary>
        /// Происходит при перемещении указателя мыши без зажатых кнопкок мыши по элементу управления.
        /// </summary>
        //public event EventHandler<MouseExEventArgs> MouseMoveNoBtnDown;

        /// <summary>
        /// Происходит при перемещении указателя мыши с зажатой левой кнопкой мыши по элементу управления.
        /// </summary>
        public event EventHandler<MouseExEventArgs> MouseMoveWithLeftBtnDown;

        /// <summary>
        /// Происходит при перемещении указателя мыши с зажатой средней кнопкой мыши по элементу управления.
        /// </summary>
        public event EventHandler<MouseExEventArgs> MouseMoveWithMiddleBtnDown;

        /// <summary>
        /// Происходит при перемещении указателя мыши с зажатой правой кнопкой мыши по элементу управления.
        /// </summary>
        public event EventHandler<MouseExEventArgs> MouseMoveWithRightBtnDown;

        private MouseExEventArgs _argsNON = new MouseExEventArgs(MouseButtons.None);
        private MouseExEventArgs _argsLBM = new MouseExEventArgs(MouseButtons.Left);
        private MouseExEventArgs _argsMMB = new MouseExEventArgs(MouseButtons.Middle);
        private MouseExEventArgs _argsRMB = new MouseExEventArgs(MouseButtons.Right);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            switch (e.Button) {
                case MouseButtons.Left:    _argsLBM.Init(e);  break;
                case MouseButtons.Middle:  _argsMMB.Init(e);  break;
                case MouseButtons.Right:   _argsRMB.Init(e);  break;
                default:               base.OnMouseDown(e);  return;
            }
            _argsNON._IsPressed = false;
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            switch (e.Button) {
                case MouseButtons.Left:    _argsLBM._IsPressed = false;  break;
                case MouseButtons.Middle:  _argsMMB._IsPressed = false;  break;
                case MouseButtons.Right:   _argsRMB._IsPressed = false;  break;
                default:               base.OnMouseUp(e);               return;
            }
            _argsNON.Init(e);
            base.OnMouseUp(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
           // var location = PointToClient(Cursor.Position);
           // _argsNON.Init(new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
           // _argsNON._IsPressed = _argsLBM._IsPressed = 
           // _argsMMB._IsPressed = _argsRMB._IsPressed = false;
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            //if (_argsNON._IsPressed && null != MouseMoveNoBtnDown)
            //    MouseMoveNoBtnDown(this, _argsNON.Update(e));
            //else {
                if (_argsLBM._IsPressed && null != MouseMoveWithLeftBtnDown)
                    MouseMoveWithLeftBtnDown(this, _argsLBM.Update(e));
                if (_argsMMB._IsPressed && null != MouseMoveWithMiddleBtnDown)
                    MouseMoveWithMiddleBtnDown(this, _argsMMB.Update(e));
                if (_argsRMB._IsPressed && null != MouseMoveWithRightBtnDown)
                    MouseMoveWithRightBtnDown(this, _argsRMB.Update(e));
            //}
            base.OnMouseMove(e);
        }
    }
}
