using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace CGLabPlatform
{
    [System.ComponentModel.DesignerCategory("")]
    public abstract class NumericUpDownTable<T> : UserControl, INotifyPropertyChanged where T : IEquatable<T>
    {
        /// <summary>
        /// Получает или задает значение, назначенное регулятору
        /// </summary>
        public T Value {
            get { return _Value; }
            set {
                if (EqualityComparer<T>.Default.Equals(_Value, value))
                    return;
                internalBinding = false;
                _Value = ElementValueChanged(value);
                
                if (!EqualityComparer<T>.Default.Equals(_Value, value)) {
                    var binding = DataBindings["Value"];
                    if (binding != null) {
                        var updmode = binding.ControlUpdateMode;
                        binding.ControlUpdateMode = ControlUpdateMode.Never;
                        binding.WriteValue();
                        binding.ControlUpdateMode = updmode;
                    }
                }
                OnPropertyChanged();
                internalBinding = true;
            }
        }
        protected T _Value;

        /// <summary>
        /// Получает или задает минимальное значение для регулятора 
        /// (Значение по умолчанию — 0).
        /// </summary>
        public decimal Minimum {
            get { return _Minimum; }
            set {
                if (_Minimum == value)
                    return;
                _Minimum = value;
                OnPropertyChanged();
            }
        }
        private decimal _Minimum;

        /// <summary>
        /// Получает или задает максимальное значение для регулятора 
        /// (Значение по умолчанию — 100).
        /// </summary>
        public decimal Maximum {
            get { return _Maximum; }
            set {
                if (_Maximum == value)
                    return;
                _Maximum = value;
                OnPropertyChanged();
            }
        }
        private decimal _Maximum;

        /// <summary>
        /// Получает или задает значение для увеличения или уменьшения регулятора при 
        /// нажатии кнопки ВВЕРХ или ВНИЗ (Значение по умолчанию — 1).
        /// </summary>
        public decimal Increment {
            get { return _Increment; }
            set {
                if (_Increment == value)
                    return;
                _Increment = value;
                OnPropertyChanged();
            }
        }
        private decimal _Increment;

        /// <summary>
        /// Получает или задает число десятичных позиций для отображения в регуляторе
        /// (Значение по умолчанию — 0).
        /// </summary>
        public int DecimalPlaces {
            get { return _DecimalPlaces; }
            set {
                if (_DecimalPlaces == value)
                    return;
                _DecimalPlaces = value;
                OnPropertyChanged();
            }
        }
        private int _DecimalPlaces;


        protected NumericUpDown[] elements;

        private bool internalBinding;

        protected NumericUpDownTable(int cols, int rows)
        {
            CreateLayout(cols, rows);
        }

        private void CreateLayout(int cols, int rows)
        {
            elements = new NumericUpDown[cols * rows];
            var _Layout = new TableLayoutPanel() { Dock = DockStyle.Fill, 
                ColumnCount = 2*cols-1, RowCount = 2*rows-1,  
                Margin = new Padding(0), Padding = new Padding(0) 
            };
            var fwidth = 100f / cols;
            var fheigh = 100f / rows;
            for (int r = 0; r < rows; ++r) {
                if (r != 0) _Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));
                _Layout.RowStyles.Add(new RowStyle(SizeType.Percent, fheigh));
            }
            for (int c = 0; c < cols; ++c) {
                if (c != 0) _Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 4));
                _Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fwidth));
            }

            for (int r = 0; r < rows; ++r) {
                for (int c = 0; c < cols; ++c) {
                    var e = r * cols + c;
                    elements[e] = new NumericUpDown() { Dock = DockStyle.Fill, Margin = new Padding(0), Tag = e };
                    elements[e].DataBindings.Add("Minimum", this, "Minimum", false, DataSourceUpdateMode.Never);
                    elements[e].DataBindings.Add("Maximum", this, "Maximum", false, DataSourceUpdateMode.Never);
                    elements[e].DataBindings.Add("Increment", this, "Increment", false, DataSourceUpdateMode.Never);
                    elements[e].DataBindings.Add("DecimalPlaces", this, "DecimalPlaces", false, DataSourceUpdateMode.Never);
                    elements[e].ValueChanged += (sender, args) => {
                        var element = sender as NumericUpDown;
                        if (element == null)
                            throw new InvalidCastException("Недопустимый тип объекта отправителя");
                        if (ElementValueUpdated(element, (int)element.Tag) && internalBinding)
                            OnPropertyChanged("Value");
                    };
                    _Layout.Controls.Add(elements[e], 2*c, 2*r);
                }
            }
            Controls.Add(_Layout);
            internalBinding = true;
        }


        protected abstract T    ElementValueChanged(T value);
        protected abstract bool ElementValueUpdated(NumericUpDown element, int e);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (!IsHandleCreated) {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
                return;
            }
                
            Invoke(new Action(() => {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }));
        }

        protected decimal[] ReadValues(decimal[] values)
        {
            for (int e = 0; e < elements.Length; ++e) 
                elements[e].Value = values[e] = Math.Min(Math.Max(Minimum, values[e]), Maximum);
            return values;
        }
    }




    #region Перегрузки
    
    public class DVector2Edit : NumericUpDownTable<DVector2> {
        public DVector2Edit() : base(2, 1) { }

        protected override DVector2 ElementValueChanged(DVector2 value) {
            return new DVector2(ReadValues(value.ToDecimalArray()));
        }

        protected override bool ElementValueUpdated(NumericUpDown element, int e) {
            if (_Value[e] != (double)element.Value) {
                _Value[e] = (double)element.Value;
                return true;
            }  return false;
        }
    }

    public class DVector3Edit : NumericUpDownTable<DVector3> {
        public DVector3Edit() : base(3, 1) { }

        protected override DVector3 ElementValueChanged(DVector3 value) {
            return new DVector3(ReadValues(value.ToDecimalArray()));
        }

        protected override bool ElementValueUpdated(NumericUpDown element, int e) {
            if (_Value[e] != (double)element.Value) {
                _Value[e] = (double)element.Value;
                return true;
            }  return false;
        }
    }

    public class DVector4Edit : NumericUpDownTable<DVector4> {      
        public DVector4Edit() : base(4, 1) { }

        protected override DVector4 ElementValueChanged(DVector4 value) {
            return new DVector4(ReadValues(value.ToDecimalArray()));
        }

        protected override bool ElementValueUpdated(NumericUpDown element, int e) {
            if (_Value[e] != (double)element.Value) {
                _Value[e] = (double)element.Value;
                return true;
            }  return false;
        }
    }

    public class DMatrix3Edit : NumericUpDownTable<DMatrix3> {
        public DMatrix3Edit() : base(3, 3) { }

        protected override DMatrix3 ElementValueChanged(DMatrix3 value) {
            return new DMatrix3(ReadValues(value.ToDecimalArray()));
        }

        protected override bool ElementValueUpdated(NumericUpDown element, int e) {
            if (_Value[e] != (double)element.Value) {
                _Value[e] = (double)element.Value;
                return true;
            }  return false;
        }
    }

    public class DMatrix4Edit : NumericUpDownTable<DMatrix4> {
        public DMatrix4Edit() : base(4, 4) { }

        protected override DMatrix4 ElementValueChanged(DMatrix4 value) {
            return new DMatrix4(ReadValues(value.ToDecimalArray()));
        }

        protected override bool ElementValueUpdated(NumericUpDown element, int e) {
            if (_Value[e] != (double)element.Value) {
                _Value[e] = (double)element.Value;
                return true;
            }  return false;
        }
    }
    #endregion
}
