using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Label = System.Windows.Forms.Label;

namespace CGLabPlatform
{
    /// <summary>
    /// Представляет элемент управления, состоящий из таблицы названий и значений свойств связанного объекта.<para/> 
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class ValueStorage : UserControl
    {
        /// <summary>
        /// Объект привязки со свойствами для отображения и связывания с элементами редактирования <para/>
        /// Свойства не помеченые аттрибутом DisplayProperty или производным от него будут проигнорированы
        /// </summary>
        public  object  Storage {
            get { return _Storage; }
            set {
                if (_Storage == value)
                    return;
                Debug.Assert(!(value is IBindable) || value.GetType().GetInterfaces().Intersect(IBindable)
                    .SingleOrDefault() != null, String.Format("Привязываемый к элементу управления ValueStorage " +
                    "объект производного класса {0} унаследованного от Bindable<T>, должен быть получен посредством " +
                    "вызова статического метода Bindable<{1}>.CreateInstance(), а не непосредственным созданием " +
                    "объекта при помощи оператора new.", value.GetType().FullName.Replace('+', '.'), value.GetType().Name));
                _Storage = value;
                if (value is IBindable) // NOTE: Сделанно допущение, что объект всегда задается из потока UI 
                    (_Storage as IBindable).uiContext = SynchronizationContext.Current; 
                UpdateLayout();  
            }
        }
        private object _Storage;

        /// <summary>
        /// Ширина в процентах левой колонки с названием свойств
        /// </summary>
        public  float  LeftColWidth {
            get { return _LeftColWidth; }
            set {
                value = Math.Max(0, Math.Min(value, 100f));
                if (_LeftColWidth == value)
                    return;
                _LeftColWidth  = value;
                _RightColWidth = 100f - value;
                UpdateLayout();  
            }
        }
        private float _LeftColWidth = 60f;

        /// <summary>
        /// Ширина в процентах правой колонки с значением свойств
        /// </summary>
        public  float  RightColWidth {
            get { return _RightColWidth; }
            set {
                value = Math.Max(0, Math.Min(value, 100f));
                if (_RightColWidth == value)
                    return;
                _RightColWidth = value;
                _LeftColWidth  = 100f - value;
                UpdateLayout();  
            }
        }
        private float _RightColWidth = 40f;

        /// <summary>
        /// Высота строки в пикселях
        /// </summary>
        public  int    RowHeight {
            get { return _RowHeight; }
            set {
                if (_RowHeight == value)
                    return;
                _RowHeight = value;
                UpdateLayout();  
            }
        }
        private int _RowHeight = 40;

        /// <summary>
        /// Возвращает значение, отображающее было ли обновленно содержание элемента. Значение false свидетельствует
        /// о том, что идет перестроение макета
        /// </summary>
        public bool LayoutUpdated { get; private set; }

        /// <summary>
        /// Меняет значения всех связаных свойства на значения по умолчанию (задаваемые аттрибутами DisplayProperty)
        /// </summary>
        public void ResetAllToDefault() {
            foreach (var prop in _PropControls.Keys)
                SetDataSourceValue(prop);
        }

        /// <summary>
        /// Находит и возвращает элемент управления связанный с указанным свойством
        /// </summary>
        /// <param name="name">Имя свойства в классе объекта привязки</param>
        /// <returns>Элемент управления или null если он не был найден</returns>
        public Control GetControlForProperty(string name) {
            var key = _PropControls.Keys.FirstOrDefault(k => String.Compare(k.Name, name, false, CultureInfo.InvariantCulture) == 0);
            return key != null ? _PropControls[key] : null; 
        }

        public ValueStorage()
        {
            Font = base.Font;
            ForeColor = base.ForeColor;
            SetVisibleScrollbars = (SetVisibleScrollbarsDelegate)typeof(ScrollableControl).GetMethod("SetVisibleScrollbars", 
                BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(SetVisibleScrollbarsDelegate), this);

            this.HorizontalScroll.Enabled = false;
            this.HorizontalScroll.Visible = false;
            this.HorizontalScroll.Minimum = 0;
            this.HorizontalScroll.Maximum = 0;
            AutoScroll = true;
            UpdateLayout();
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x0005) {  // WM_SIZE
                HorizontalScroll.Minimum = 0;
                HorizontalScroll.Maximum = 0;
                base.WndProc(ref m);
                if (HorizontalScroll.Visible)
                    SetVisibleScrollbars(false, VerticalScroll.Visible);
                else return;
            }
            base.WndProc(ref m);
        }

        protected override void OnClientSizeChanged(EventArgs e) {
            bindings.ContentPanelWidth = ClientSize.Width;
            base.OnClientSizeChanged(e);   
        }

        private delegate bool SetVisibleScrollbarsDelegate(bool horiz, bool vert);
        private SetVisibleScrollbarsDelegate SetVisibleScrollbars;

        internal void UpdateLayout()
        {
            LayoutUpdated = false;
            Controls.Clear();
            _PropControls.Clear();
            if (_Storage == null)
                return;

            Panel _Panel; TableLayoutPanel _Layout; Control _Control;
            Controls.Add(_Panel = new Panel { BackColor = Color.Transparent });
            _Panel.DataBindings.Add("Width", bindings, "ContentPanelWidth", false, DataSourceUpdateMode.Never);
            _Panel.Controls.Add(_Layout = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0), Padding = new Padding(4,4,8,4) });
            _Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, _LeftColWidth));
            _Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, _RightColWidth));
            _Layout.Resize += (s, a) => ResizeLayout();

            foreach (var prop in _Storage.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                var attr = prop.GetCustomAttribute<DisplayPropertyAttribute>();
                if (attr == null) continue;
                Debug.Assert(attr.SupportType(prop.PropertyType), String.Format("Невозможно осуществить связывание свойства {0} класса {1} " +
                    "с элементом управления, так как данное свойство помеченное аттрибутом {2} имеет не доступимый тип {3}.", prop.Name,
                    (_Storage.GetType().GetInterfaces().Any(i => i == typeof(IBindable)) ? _Storage.GetType().BaseType : _Storage.GetType())
                    .FullName.Replace('+', '.'), attr.GetType().Name, prop.PropertyType.Name));

                _Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, (int)(_RowHeight * attr.LineHeightFactor)));
                if (attr.Name != null) {
                    _Layout.Controls.Add(_Control = new Label() { Text = attr.Name, Font = Font, ForeColor = ForeColor, Dock = DockStyle.Fill, 
                            AutoSize = true, Margin = new Padding(0, (_RowHeight - Font.Height) / 2, 0, 0) }, 0, _Layout.RowCount);
                    Binding bind = new Binding("Font", bindings, "FontUpdate", false, DataSourceUpdateMode.Never);
                    //bind.Format += (s, e) => e.Value = ((s as Binding).DataSource as Bindings).Font ?? e.Value;
                    bind.Format += (s, e) => {
                        e.Value = ((s as Binding).DataSource as Bindings).Font ?? e.Value;
                        Margin = new Padding(0, (_RowHeight - Font.Height) / 2, 0, 0);
                    };
                    _Control.DataBindings.Add(bind);
                    _Control.DataBindings.Add("ForeColor", bindings, "ForeColor", false, DataSourceUpdateMode.Never);
                }
                _Layout.Controls.Add(_Control = attr.CreateNewControl(), attr.Name == null ? 0 : 1, _Layout.RowCount);
                _Layout.SetColumnSpan(_Control, (attr.Name == null) ? 2 : 1);
                attr.SetControlStyle(this, _Control);

                _PropControls.Add(prop, _Control);
                SetDataSourceValue(prop, attr.Default);
                _Control.DataBindings.Add(attr.Property, _Storage, prop.Name, false, attr.UpdateMode);
                _Layout.RowCount++;
            }

            _Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _Panel.Height = _Layout.Padding.Vertical;
            for (int r = 0; r < _Layout.RowCount; ++r)
                if (_Layout.RowStyles[r].SizeType == SizeType.Absolute)
                    _Panel.Height += (int)_Layout.RowStyles[r].Height;

            _Layout.RowCount++;
            LayoutUpdated = true;
            ResizeLayoutInvalidate();
        }

        private int _MaxLeftColWidth;

        private void ResizeLayoutInvalidate()
        {
            if (Controls.Count == 0)
                return;
            var maxwidth = 0;
            var graphics = CreateGraphics();
            var ctlayout = Controls[0].Controls[0] as TableLayoutPanel;
            var iterator = ctlayout.Controls.GetEnumerator();
            while (iterator.MoveNext()) {
                var label = iterator.Current as Label;
                if (label == null)
                    continue;
                var lblwidth = (int)graphics.MeasureString(label.Text, label.Font)
                    .Width + label.Padding.Horizontal + label.Margin.Horizontal
                    + ctlayout.Padding.Left + ctlayout.Margin.Left;
                maxwidth = Math.Max(lblwidth, maxwidth);
            }
            graphics.Dispose();
            if (_MaxLeftColWidth != maxwidth) {
                _MaxLeftColWidth = maxwidth;
                ResizeLayout();
            } else 
                _MaxLeftColWidth = maxwidth;
        }

        private void ResizeLayout()
        {
            var ctlayout = Controls[0].Controls[0] as TableLayoutPanel;
            var colwidth = ctlayout.GetColumnWidths()[0];
            if (colwidth > _MaxLeftColWidth) {
                ctlayout.ColumnStyles[0].SizeType = SizeType.Absolute;
                ctlayout.ColumnStyles[0].Width = _MaxLeftColWidth;
                ctlayout.ColumnStyles[1].Width = 100f;
            } else if (100f - 100f * colwidth / ctlayout.Width < _RightColWidth) {
                ctlayout.ColumnStyles[0].SizeType = SizeType.Percent;
                ctlayout.ColumnStyles[0].Width = _LeftColWidth;
                ctlayout.ColumnStyles[1].Width = _RightColWidth;
            }
            
        }

        public Bindings bindings = Bindings.CreateInstance(); 
        public abstract class Bindings : Bindable<Bindings> {
            public abstract int ContentPanelWidth { get; set; }
            public abstract Color ForeColor { get; set; }
            public abstract Font Font { get; set; }
            public abstract bool FontUpdate { get; set; }
        }
        public new Color ForeColor { get { return bindings.ForeColor; } set { bindings.ForeColor = value; } }
        public new Font Font { get { return bindings.Font; } set {
            bindings.Font = value; bindings.FontUpdate = !bindings.FontUpdate;
            ResizeLayoutInvalidate();
        }}

        private void SetDataSourceValue(PropertyInfo prop, object value = null) {
            if (value == null)
                value = prop.GetCustomAttribute<DisplayPropertyAttribute>().Default;
            if (value.GetType() == prop.PropertyType)
                prop.SetValue(_Storage, value);
            else if (typeof(IConvertible).IsInstanceOfType(value))
                prop.SetValue(_Storage, Convert.ChangeType(value, prop.PropertyType));
            else
                throw new Exception(String.Format("Невозможно инициализировать свойство {0} типа {1} " +
                    "значением {2} типа {3}", prop.Name, prop.PropertyType.Name, value, value.GetType().Name));
        }

        private Dictionary<PropertyInfo, Control> _PropControls = new Dictionary<PropertyInfo, Control>();  
        
        internal static List<Type> IBindable = new List<Type>(2); 
    }

    /// <summary>
    /// Базовый класс контейнера для свойств, применяемого для привязки к элементу управления ValueStorage<para/>
    /// Для создания экземпляра использовать CreateInstance
    /// </summary>
    public abstract class Bindable<T> : INotifyPropertyChanged, IBindable where T : Bindable<T>
    {
        /// <summary>
        /// Создает новый экзэмпляр прокси объекта производного класса Bindable&lt;T&gt;, перегружающего<para/>
        /// виртуальные и абстрактные свойства для реализации их привязки в обе стороны (two way binding).<para/>
        /// </summary>
        public static T CreateInstance()
        {
            if (_ProxyType != null)
                return (T)Activator.CreateInstance(_ProxyType);

            var typeofobj = typeof(T); ILGenerator il;
            Debug.Assert(!typeofobj.IsSealed, String.Format("Производный класс {0} унаследованный от Bindable<T> не может " +
                        "являться запечатенным типом (объявленным с спецификатором sealed).", typeofobj.FullName.Replace('+', '.')));
            Debug.Assert(typeofobj.IsVisible, String.Format("Производный класс {0} унаследованный от Bindable<T> должен " +
                        "являться открытым типом или открытым вложенным типом.{1}Убедитесь, что класс {0} и все классы, " +
                        "в которые он вложен, объявленны с спецификатором доступа public.", typeofobj.FullName.Replace('+', '.'), Environment.NewLine));
            var nameofasm = new AssemblyName(String.Format("Bindable<{0}>Proxy", typeofobj.Name));
            var dasmbuild = AppDomain.CurrentDomain.DefineDynamicAssembly(nameofasm, AssemblyBuilderAccess.Run);
            var dmodbuild = dasmbuild.DefineDynamicModule(nameofasm.Name);
            var dintbuild = dmodbuild.DefineType("IBindable", TypeAttributes.Public|TypeAttributes.Abstract|TypeAttributes.Interface);
            ValueStorage.IBindable.Add(dintbuild.CreateType());
            var typebuild = dmodbuild.DefineType("Proxy_" + typeofobj.Name, TypeAttributes.Public | TypeAttributes.Sealed
                                                    | TypeAttributes.BeforeFieldInit, typeofobj, new[]{ValueStorage.IBindable.Last()});

            var onPropertyChanged = typeofobj.GetMethod("OnPropertyChanged", BindingFlags.NonPublic | BindingFlags.Instance);       
            var properties = typeofobj.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var property in properties.Where(p => p.SetMethod != null && p.SetMethod.IsVirtual && !p.SetMethod.IsFinal
                                                        || p.GetMethod != null && p.GetMethod.IsVirtual && !p.GetMethod.IsFinal))
            {
                Debug.Assert(property.SetMethod != null && property.GetMethod != null, String.Format("Свойство {0} " +
                        "производного класса {1} унаследованного от Bindable<T> объявленно {2}, но не содержит обоих " +
                        "методов доступа получения и записи данных (get и set).", property.Name, typeofobj.FullName
                        .Replace('+', '.'), (property.SetMethod ?? property.GetMethod).IsAbstract ? "абстрактным (abstract)" 
                        : "виртулаьным (virtual)"));
                Debug.Assert(property.SetMethod.IsPublic && property.GetMethod.IsPublic, String.Format("Свойство {0} " +
                        "производного класса {1} унаследованного от Bindable<T> объявленно {2}, но не содержит открытого " +
                        "метода доступа {3}. Убедитесь, что он не объявлен с спецификатором доступа private, protected " +
                        "или internal, а само свойство объявленно как public.", property.Name, typeofobj.FullName
                        .Replace('+', '.'), property.SetMethod.IsAbstract ? "абстрактным (abstract)" : "виртуальным " +
                        "(virtual)", property.SetMethod.IsPublic ? "получения данных (get)" : "записи данных (set)"));

                var fieldname = String.Format("<{0}>k__BackingField", property.Name);
                var attribute = property.SetMethod.Attributes & ~(MethodAttributes.NewSlot | MethodAttributes.Abstract);
                var backfield = (property.SetMethod.IsAbstract || null !=
                                typeofobj.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance))
                              ? typebuild.DefineField(fieldname, property.PropertyType, FieldAttributes.Private)
                              : null;
                var setmethod = typebuild.DefineMethod(property.SetMethod.Name, attribute,
                                property.SetMethod.CallingConvention, typeof(void), new[] { property.PropertyType });
                var getmethod = typebuild.DefineMethod(property.GetMethod.Name, attribute,
                                property.GetMethod.CallingConvention, property.PropertyType, new Type[] { });
                typebuild.DefineMethodOverride(getmethod, property.GetMethod);
                typebuild.DefineMethodOverride(setmethod, property.SetMethod);

                il = getmethod.GetILGenerator();
                var vars = il.DeclareLocal(property.PropertyType);
                il.Emit(OpCodes.Ldarg_0);
                if (backfield == null)
                    il.Emit(OpCodes.Call, property.GetMethod);
                else
                    il.Emit(OpCodes.Ldfld, backfield);          
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Br_S, vars);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);

                il = setmethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                if (backfield == null)
                    il.Emit(OpCodes.Call, property.SetMethod);
                else
                    il.Emit(OpCodes.Stfld, backfield);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, property.Name);
                il.Emit(OpCodes.Call, onPropertyChanged); 
                il.Emit(OpCodes.Ret);    
            }

            typebuild.DefineDefaultConstructor(MethodAttributes.Public);
            _ProxyType = typebuild.CreateType();  
            return CreateInstance();
        }

        private static Type _ProxyType;

        private  SynchronizationContext  uiContext;
        SynchronizationContext IBindable.uiContext
        { get { return uiContext; } set { uiContext = value; } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                if (uiContext != null) uiContext.Post(s => 
                     handler(this, new PropertyChangedEventArgs(propertyName)), null);
                else handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        /// <summary>
        /// Возвращает значение свойства, возбуждая событие PropertyChanged
        /// <para/>public int FooProperty {
        /// <para/>&#160;&#160;&#160;&#160;get { return Get&lt;int&gt;(); }
        /// <para/>&#160;&#160;&#160;&#160;set { Set&lt;int&gt;(value); }
        /// <para/>}
        /// </summary>
        protected P Get<P>([CallerMemberName] string name = null) {
            Debug.Assert(name != null, "name != null");
            object value = null;
            if (_properties.TryGetValue(name, out value))
                return value == null ? default(P) : (P)value;
            return default(P);
        }

        /// <summary>
        /// Устанавливает значение свойства, возбуждая событие PropertyChanged. Вовзращает false если<para/>
        /// старое значение совпадало с новым и true в обратном случае. Предназначен для определения свойств:
        /// <para/>public int FooProperty {
        /// <para/>&#160;&#160;&#160;&#160;get { return Get&lt;int&gt;(); }
        /// <para/>&#160;&#160;&#160;&#160;set { Set&lt;int&gt;(value); }
        /// <para/>}
        /// </summary>
        protected bool Set<P>(P value, [CallerMemberName] string name = null) {
            Debug.Assert(name != null, "name != null");
            if (EqualityComparer<P>.Default.Equals(value, Get<P>(name)))
                return false;
            _properties[name] = value;
            OnPropertyChanged(name);
            return true;
        }        
    }

    internal interface IBindable {
        SynchronizationContext uiContext { get; set; }
    }

    /// <summary>
    /// Базовый аттрибут для связывания и представления этого свойства в элементе управления ValueStorage
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class DisplayPropertyAttribute : Attribute
    {
        internal string  Name     { get; private set; }   // Отбражаемое название в эл. управления или null
        internal object  Default  { get; private set; }   // Значение свойства по умолчанию
        internal string  Property { get; private set; }   // Свойство элемента управления для привязки
        internal DataSourceUpdateMode UpdateMode { get; set; }    // Тип привязки элемента управления
        internal protected readonly DisplayType displayType = DisplayType.Unknown;
        internal protected enum DisplayType { Unknown, Checker, Numeric, TextBox, EnumList, Vector2, Vector3, Vector4, Matrix3, Matrix4, ColorBox }
        internal protected DisplayPropertyAttribute(DisplayType DisplayType, string Property, object Default, 
            string Name, DataSourceUpdateMode UpdateMode = DataSourceUpdateMode.OnPropertyChanged) {
            this.Name = Name;
            this.Default = Default;
            this.Property = Property;
            this.UpdateMode = UpdateMode;
            this.displayType = DisplayType;
        }
        public DisplayPropertyAttribute(object Default, string Property, string Name = null, DataSourceUpdateMode UpdateMode 
            = DataSourceUpdateMode.OnPropertyChanged) : this(DisplayType.Unknown, Property, Default, Name, UpdateMode) { }

        protected Type ValueType { get { return Default.GetType(); } }

        public virtual double LineHeightFactor { get { return 1d; } }

        public virtual bool SupportType(Type type) {
            switch (Type.GetTypeCode(type)) {
                case TypeCode.Int32:    case TypeCode.UInt32:   if (displayType == DisplayType.ColorBox)
                                                                    return true; 
                                                                goto case TypeCode.Int64;
                case TypeCode.Byte:     case TypeCode.UInt16:   case TypeCode.UInt64:
                case TypeCode.SByte:    case TypeCode.Int16:    case TypeCode.Int64:
                                        if (displayType == DisplayType.EnumList && type.IsEnum)
                                            return true;
                                        else goto case TypeCode.Decimal;
                case TypeCode.Single:   case TypeCode.Double:   case TypeCode.Decimal:  
                                        return displayType == DisplayType.Numeric;
                case TypeCode.Boolean:  return displayType == DisplayType.Checker;
                case TypeCode.String:   return displayType == DisplayType.TextBox;
                case TypeCode.Object:   switch (displayType) {
                                            case DisplayType.ColorBox:
                                            case DisplayType.Vector2: return type == typeof(DVector2);
                                            case DisplayType.Vector3: return type == typeof(DVector3);
                                            case DisplayType.Vector4: return type == typeof(DVector4);
                                            case DisplayType.Matrix3: return type == typeof(DMatrix3);
                                            case DisplayType.Matrix4: return type == typeof(DMatrix4);
                                            default: return false;
                                        }
                default:                return false;
            }
        }

        internal Control CreateNewControl() { return CreateControl(); }
        protected virtual Control CreateControl() {
            throw new NotImplementedException();
        }

        internal virtual void SetControlStyle(ValueStorage owner, Control control) {
            control.Font   = owner.Font;
            control.Margin = new Padding(0, (owner.RowHeight - control.Height) / 2, 0, 0);
            control.Dock   = DockStyle.Fill;
        }
    }

    /// <summary>
    /// Задает элемент CheckBox для связывания и представления этого свойства в элементе управления ValueStorage
    /// <para/>Может применяться к свойствам типа bool
    /// </summary>
    public sealed class DisplayCheckerPropertyAttribute : DisplayPropertyAttribute {
        internal new string Name { get; private set; } 
        public DisplayCheckerPropertyAttribute(bool Default, string Name)
            : base(DisplayType.Checker, "Checked", Default, null) { this.Name = Name; }
        protected override Control CreateControl() {
            return new CheckBox() { AutoSize = true, Text = Name };
        }
    }

    /// <summary>
    /// Задает элемент TextBox для связывания и представления этого свойства в элементе управления ValueStorage
    /// <para/>Может применяться к свойствам типа string
    /// </summary>
    public sealed class DisplayTextBoxPropertyAttribute : DisplayPropertyAttribute {
        public DisplayTextBoxPropertyAttribute(string Default, string Name)
            : base(DisplayType.TextBox, "Text", Default, Name) { }
        protected override Control CreateControl() {
            return new TextBox();
        }
    }

    /// <summary>
    /// Задает элемент ComboBox для связывания и представления этого свойства в элементе управления ValueStorage
    /// <para/>Может применяться к свойствам типа enum
    /// </summary>
    public sealed class DisplayEnumListPropertyAttribute : DisplayPropertyAttribute {
        public DisplayEnumListPropertyAttribute(object Default, string Name = null)
            : base(DisplayType.EnumList, "SelectedValue", Default, Name) { }
        protected override Control CreateControl() {
            return new ComboBox() {
                DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Value", ValueMember = "Key",
                DataSource = new BindingSource(Enum.GetValues(ValueType).Cast<object>().ToDictionary(v => v,
                v => (GetCustomAttribute(ValueType.GetField(v.ToString()), typeof(DescriptionAttribute)) 
                as DescriptionAttribute ?? new DescriptionAttribute(v.ToString())).Description), null)
            };
        }
    }

    /// <summary>
    /// Задает элемент NumericUpDown для связывания и представления этого свойства в элементе управления ValueStorage
    /// <para/>Может применяться к свойствам типа double, float, decimal, sbyte, byte, short, ushort, int, uint, long, ulong,
    /// <para/>DVector2, DVector3, DVector4, DMatrix3, DMatrix4
    /// </summary>
    public sealed class DisplayNumericPropertyAttribute : DisplayPropertyAttribute {
        private decimal Minimum, Maximum, Increment;  private int Decimals;
        private DisplayNumericPropertyAttribute(object Default, string Name, dynamic Increment, dynamic Minimum,
            dynamic Maximum, int Decimals = -1) : base(GetDisplayType(Default), "Value", ConvertTo(Default), Name) {
            IEnumerable<decimal> values;
            switch (displayType) {
                case DisplayType.Numeric: values = new[]{(decimal)this.Default}; break;
                case DisplayType.Vector2: values = ((DVector2)this.Default).ToArray().Select(v => (decimal)v); break;
                case DisplayType.Vector3: values = ((DVector3)this.Default).ToArray().Select(v => (decimal)v); break;
                case DisplayType.Vector4: values = ((DVector4)this.Default).ToArray().Select(v => (decimal)v); break;
                case DisplayType.Matrix3: values = ((DMatrix3)this.Default).ToArray().Select(v => (decimal)v); break;
                case DisplayType.Matrix4: values = ((DMatrix4)this.Default).ToArray().Select(v => (decimal)v); break;
                default: throw new NotSupportedException();
            }
            this.Minimum   = Math.Min(Convert.ToDecimal(Minimum, NumberFormatInfo.InvariantInfo), values.Min());
            this.Maximum   = Math.Max(Convert.ToDecimal(Maximum, NumberFormatInfo.InvariantInfo), values.Max());
            this.Increment = Convert.ToDecimal(Increment, NumberFormatInfo.InvariantInfo);

            this.Decimals  = Decimals >= 0 ? Decimals : values.Concat(new[] {this.Increment}).Max(v => 
                             v.ToString().SkipWhile(c => c != ',' && c != '.').Skip(1).Count());   
        }

        public override double LineHeightFactor { get {
            switch (displayType) {
                case DisplayType.Matrix3: return 3 * base.LineHeightFactor;
                case DisplayType.Matrix4: return 4 * base.LineHeightFactor;
                default: return base.LineHeightFactor;
            }
        } }

        private static DisplayType GetDisplayType(object Default)
        {
            var type = Default.GetType();
            if (Type.GetTypeCode(type) != TypeCode.Object)
                return DisplayType.Numeric;

            if (type.IsArray && type.GetArrayRank() == 1) {
                switch ((Default as Array).Length) {
                    case  2: return DisplayType.Vector2;
                    case  3: return DisplayType.Vector3;
                    case  4: return DisplayType.Vector4;
                    case  9: return DisplayType.Matrix3;
                    case 16: return DisplayType.Matrix4;
                }
            }
            return DisplayType.Unknown;
        }

        private static object ConvertTo(object Default)
        {
            var type = Default.GetType();
            if (Type.GetTypeCode(type) != TypeCode.Object)
                return Convert.ToDecimal(Default);
           
            if (type.IsArray && type.GetArrayRank() == 1) {
                double[] array;
                switch (Type.GetTypeCode(type.GetElementType())) {
                    case TypeCode.Byte:     array = (Default as byte[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.UInt16:   array = (Default as ushort[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.UInt32:   array = (Default as uint[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.UInt64:   array = (Default as ulong[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.SByte:    array = (Default as sbyte[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.Int16:    array = (Default as short[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.Int32:    array = (Default as int[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.Int64:    array = (Default as long[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.Single:   array = (Default as float[]).Select(v => (double)v).ToArray(); break;
                    case TypeCode.Double:   array = Default as double[]; break;
                    case TypeCode.Decimal:  array = (Default as decimal[]).Select(v => (double)v).ToArray(); break;
                    default: return null;
                }
                switch (array.Length) {
                    case  2: return new DVector2(array);
                    case  3: return new DVector3(array);
                    case  4: return new DVector4(array);
                    case  9: return new DMatrix3(array);
                    case 16: return new DMatrix4(array);
                    default: return null;
                }
            }

            return null;
        }
        protected override Control CreateControl() {
            switch (displayType) {
                case DisplayType.Numeric: return new NumericUpDownEx() { Minimum = Minimum, Maximum = Maximum, Increment = Increment, DecimalPlaces = Decimals };
                case DisplayType.Vector2: return new DVector2Edit()    { Minimum = Minimum, Maximum = Maximum, Increment = Increment, DecimalPlaces = Decimals };
                case DisplayType.Vector3: return new DVector3Edit()    { Minimum = Minimum, Maximum = Maximum, Increment = Increment, DecimalPlaces = Decimals };
                case DisplayType.Vector4: return new DVector4Edit()    { Minimum = Minimum, Maximum = Maximum, Increment = Increment, DecimalPlaces = Decimals };
                case DisplayType.Matrix3: return new DMatrix3Edit()    { Minimum = Minimum, Maximum = Maximum, Increment = Increment, DecimalPlaces = Decimals };
                case DisplayType.Matrix4: return new DMatrix4Edit()    { Minimum = Minimum, Maximum = Maximum, Increment = Increment, DecimalPlaces = Decimals };
                default: throw new NotSupportedException();
            }
        }
        public DisplayNumericPropertyAttribute(sbyte Default, sbyte Increment, string Name, sbyte Minimum = sbyte.MinValue, sbyte Maximum = sbyte.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, 0) {}
        public DisplayNumericPropertyAttribute(short Default, short Increment, string Name, short Minimum = short.MinValue, short Maximum = short.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, 0) {}
        public DisplayNumericPropertyAttribute(int Default, int Increment, string Name, int Minimum = int.MinValue, int Maximum = int.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, 0) {}
        public DisplayNumericPropertyAttribute(long Default, long Increment, string Name, long Minimum = long.MinValue, long Maximum = long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, 0) {}
        public DisplayNumericPropertyAttribute(byte Default, byte Increment, string Name, byte Minimum = byte.MinValue, byte Maximum = byte.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, 0) {}
        public DisplayNumericPropertyAttribute(ushort Default, ushort Increment, string Name, ushort Minimum = ushort.MinValue, ushort Maximum = ushort.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, 0) {}
        public DisplayNumericPropertyAttribute(ulong Default, ulong Increment, string Name, ulong Minimum = ulong.MinValue, ulong Maximum = ulong.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, 0) {}
        public DisplayNumericPropertyAttribute(float Default, float Increment, string Name, float Minimum = (float)long.MinValue, float Maximum = (float)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(float Default, float Increment, int Decimals, string Name, float Minimum = (float)long.MinValue, float Maximum = (float)long.MaxValue)
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}
        public DisplayNumericPropertyAttribute(double Default, double Increment, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(double Default, double Increment, int Decimals, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}
        public DisplayNumericPropertyAttribute(decimal Default, decimal Increment, string Name, decimal Minimum = decimal.MinValue, decimal Maximum = decimal.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(decimal Default, decimal Increment, int Decimals, string Name, decimal Minimum = decimal.MinValue, decimal Maximum = decimal.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}  

        public DisplayNumericPropertyAttribute(double[] Default, double Increment, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(double[] Default, double Increment, int Decimals, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}       
        public DisplayNumericPropertyAttribute(DVector2 Default, double Increment, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(DVector2 Default, double Increment, int Decimals, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}
        public DisplayNumericPropertyAttribute(DVector3 Default, double Increment, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(DVector3 Default, double Increment, int Decimals, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}
        public DisplayNumericPropertyAttribute(DVector4 Default, double Increment, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(DVector4 Default, double Increment, int Decimals, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}
        public DisplayNumericPropertyAttribute(DMatrix3 Default, double Increment, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(DMatrix3 Default, double Increment, int Decimals, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}
        public DisplayNumericPropertyAttribute(DMatrix4 Default, double Increment, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum) {}
        public DisplayNumericPropertyAttribute(DMatrix4 Default, double Increment, int Decimals, string Name, double Minimum = (double)long.MinValue, double Maximum = (double)long.MaxValue) 
                                       : this(Default, Name, Increment, Minimum, Maximum, Decimals) {}
    }

    /// <summary>
    /// Задает элемент ColorBox для связывания и представления этого свойства в элементе управления ValueStorage
    /// <para/>Может применяться к свойствам типа int, Color, DVector3
    /// </summary>
    public sealed class DisplayColorBoxPropertyAttribute : DisplayPropertyAttribute { 
        internal new string Name { get; private set; }
        public DisplayColorBoxPropertyAttribute(int Default, string Name)
            : base(DisplayType.ColorBox, "Value", Default, null) { this.Name = Name; }
        public DisplayColorBoxPropertyAttribute(Color Default, string Name)
            : this(Default.ToArgb(), Name) { }
        public DisplayColorBoxPropertyAttribute(DVector3 Default, string Name)
            : this(0xFF | ((int)(255*Default.X)&0xFF<<24) | ((int)(255*Default.Y)&0xFF<<16) | (int)(255*Default.Z)&0xFF, Name) { }
        protected override Control CreateControl()
        {
            throw new NotImplementedException();
        }
    }
}
