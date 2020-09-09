using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CGLabPlatform
{
    [Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct DVector2 : ICloneable, IEquatable<DVector2>, IFormattable
	{
		public double X;
		public double Y;

        #region Конструкторы
        
        public DVector2(double x, double y) {
			X = x;  Y = y;
		}

		public DVector2(double[] coordinates) {
			Debug.Assert(coordinates != null && coordinates.Length == 2);
			X = coordinates[0];  Y = coordinates[1];
		}

		public DVector2(List<double> coordinates) {
			Debug.Assert(coordinates != null && coordinates.Count == 2);
			X = coordinates[0];  Y = coordinates[1];
		}

        public DVector2(decimal[] elements) : this(elements.Select(e => (double)e).ToArray()) { }

		public DVector2(DVector2 vector) {
			X = vector.X;  Y = vector.Y;
		}

        public DVector2(PointF point) {
            X = point.X; Y = point.Y;
		}

        public DVector2(Point point) {
            X = point.X; Y = point.Y;
		}
        #endregion

        #region Константы

        /// <summary>
        /// Получает вектор, три элемента которого равны нулю.
		/// </summary>
        public static readonly DVector2 Zero  = new DVector2(0.0, 0.0);
        /// <summary>
		/// Получает вектор, три элемента которого равны единице.
		/// </summary>
        public static readonly DVector2 One   = new DVector2(1.0, 1.0);
		/// <summary>
		/// Получает вектор (1,0).
		/// </summary>
        public static readonly DVector2 UnitX = new DVector2(1.0, 0.0);
		/// <summary>
		/// Получает вектор (0,1).
		/// </summary>
        public static readonly DVector2 UnitY = new DVector2(0.0, 1.0);
        #endregion

        #region Статические методы

        public static DVector2 Add(DVector2 left, DVector2 right) {
            return new DVector2(left.X + right.X, left.Y + right.Y);
		}

        public static DVector2 Add(DVector2 vector, double scalar) {
            return new DVector2(vector.X + scalar, vector.Y + scalar);
		}

        public static void Add(DVector2 left, DVector2 right, ref DVector2 result) {
			result.X = left.X + right.X;
			result.Y = left.Y + right.Y;
		}

        public static void Add(DVector2 vector, double scalar, ref DVector2 result) {
			result.X = vector.X + scalar;
			result.Y = vector.Y + scalar;
		}

        public static DVector2 Subtract(DVector2 left, DVector2 right) {
            return new DVector2(left.X - right.X, left.Y - right.Y);
		}

        public static DVector2 Subtract(DVector2 vector, double scalar) {
            return new DVector2(vector.X - scalar, vector.Y - scalar);
		}

        public static DVector2 Subtract(double scalar, DVector2 vector) {
            return new DVector2(scalar - vector.X, scalar - vector.Y);
		}

        public static void Subtract(DVector2 left, DVector2 right, ref DVector2 result) {
			result.X = left.X - right.X;
			result.Y = left.Y - right.Y;
		}

        public static void Subtract(DVector2 vector, double scalar, ref DVector2 result) {
			result.X = vector.X - scalar;
			result.Y = vector.Y - scalar;
		}

        public static void Subtract(double scalar, DVector2 vector, ref DVector2 result) {
			result.X = scalar - vector.X;
			result.Y = scalar - vector.Y;
		}

        public static DVector2 Divide(DVector2 left, DVector2 right) {
            return new DVector2(left.X / right.X, left.Y / right.Y);
		}

        public static DVector2 Divide(DVector2 vector, double scalar) {
            return new DVector2(vector.X / scalar, vector.Y / scalar);
		}

        public static DVector2 Divide(double scalar, DVector2 vector) {
            return new DVector2(scalar / vector.X, scalar / vector.Y);
		}

        public static void Divide(DVector2 left, DVector2 right, ref DVector2 result) {
			result.X = left.X / right.X;
			result.Y = left.Y / right.Y;
		}

        public static void Divide(DVector2 vector, double scalar, ref DVector2 result) {
			result.X = vector.X / scalar;
			result.Y = vector.Y / scalar;
		}

        public static void Divide(double scalar, DVector2 vector, ref DVector2 result) {
			result.X = scalar / vector.X;
			result.Y = scalar / vector.Y;
		}

        public static DVector2 Multiply(DVector2 vector, double scalar) {
            return new DVector2(vector.X * scalar, vector.Y * scalar);
		}

        public static void Multiply(DVector2 vector, double scalar, ref DVector2 result) {
			result.X = vector.X * scalar;
			result.Y = vector.Y * scalar;
		}

        public static DVector2 Multiply(DVector2 left, DVector2 right) {
            return new DVector2(left.X * right.X, left.Y * right.Y);
        }

        public static void Multiply(ref DVector2 left, DVector2 right) {
            left.X *= right.X;
            left.Y *= right.Y;
        }

        public static double DotProduct(DVector2 left, DVector2 right) {
			return (left.X * right.X) + (left.Y * right.Y);
		}

        public static DVector2 Negate(DVector2 vector) {
            return new DVector2(-vector.X, -vector.Y);
		}

        public static bool ApproxEqual(DVector2 left, DVector2 right) {
			return ApproxEqual(left, right, Double.Epsilon);
		}

        public static bool ApproxEqual(DVector2 left, DVector2 right, double tolerance) {
			return ((System.Math.Abs(left.X - right.X) <= tolerance) &&
				    (System.Math.Abs(left.Y - right.Y) <= tolerance) );
		}
		#endregion

        #region Свойства и Методы

	    public DVector2 Multiply(DVector2 vector) {
	        return Multiply(this, vector);
	    }

        public DVector2 Multiplied(DVector2 vector) {
	        Multiply(ref this, vector);
            return this;
        }

        public double DotProduct(DVector2 vector) {
            return DotProduct(this, vector);
	    }

        public void Normalize() {
			double length = GetLength();
			if (length == 0)
                throw new DivideByZeroException("Невозможно нормализовать вектор нулевой длинны");
			X /= length;
			Y /= length;
		}

	    public DVector2 Normalized() {
	        Normalize();
	        return this;
	    }

		public double GetLength() {
			return System.Math.Sqrt(X * X + Y * Y);
		}

		public double GetLengthSquared() {
			return (X * X + Y * Y);
		}

		public void ClampZero(double tolerance) {
            X = (tolerance > Math.Abs(X)) ? 0 : X;
            Y = (tolerance > Math.Abs(Y)) ? 0 : Y;
		}

		public void ClampZero() {
            X = (Double.Epsilon > Math.Abs(X)) ? 0 : X;
            Y = (Double.Epsilon > Math.Abs(Y)) ? 0 : Y;
		}

        public bool ApproxEqual(DVector2 vector) {
            return ApproxEqual(this, vector);
        }

        public bool ApproxEqual(DVector2 vector, double tolerance) {
            return ApproxEqual(this, vector, tolerance);
        }

        public double[] ToArray() {
	        return new double[2] { X, Y };
	    }

        public float[] ToFloatArray() {
            return ToArray().Select(v => (float)v).ToArray();
	    }

        public decimal[] ToDecimalArray() {
            return ToArray().Select(v => (decimal)v).ToArray();
	    }

        public PointF ToPointF() {
            return new PointF((float)X, (float)Y);
        }

        public Point ToPoint() {
            return new Point((int)X, (int)Y);
        }

        public Point ToPointRound() {
            return new Point((int)Math.Round(X), (int)Math.Round(Y));
        }

        public Point ToPointFloor() {
            return new Point((int)Math.Floor(X), (int)Math.Floor(Y));
        }

        public Point ToPointCeiling() {
            return new Point((int)Math.Ceiling(X), (int)Math.Ceiling(Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public DVector3 ToDVector3(double z) { unchecked {
	        return new DVector3( X, Y, z );
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public DVector4 ToDVector4(double z, double w) { unchecked {
	        return new DVector4( X, Y, z, w );
	    }}
        #endregion

        #region Реализация интерфейса ICloneable

        object ICloneable.Clone() {
            return new DVector2(this);
        }

        public DVector2 Clone() {
            return new DVector2(this);
        }
        #endregion

        #region Реализация интерфейса IEquatable<Vector4>

        bool IEquatable<DVector2>.Equals(DVector2 v) {
            return (X == v.X) && (Y == v.Y);
        }

        public bool Equals(DVector2 v) {
            return (X == v.X) && (Y == v.Y);
        }
        #endregion

        #region Реализация интерфейса IFormattable

        public string ToString(string format) {
            return String.Format("({0:}, {1})", X.ToString(format, null),
                Y.ToString(format, null));
        }

        string IFormattable.ToString(string format, IFormatProvider provider) {
            return ToString(format, provider);
        }

        public string ToString(string format, IFormatProvider provider) {
            return String.Format("({0:}, {1})", X.ToString(format, provider), 
                Y.ToString(format, provider));
        }
        #endregion

        #region Перегрузки

        public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode();
		}

		public override bool Equals(object obj) {
            if (obj is DVector3) {
                DVector3 v = (DVector3)obj;
				return (X == v.X) && (Y == v.Y);
			}
			if (obj is DVector4) {
                DVector4 v = (DVector4)obj;
				return (X == v.X) && (Y == v.Y);
			}
			return false;
		}

		public override string ToString() {
			return string.Format("({0:}, {1})", X, Y);
		}
		#endregion

        #region Операторы

        public static bool operator ==(DVector2 left, DVector2 right) {
            return ((left.X == right.X) && (left.Y == right.Y));
		}

        public static bool operator !=(DVector2 left, DVector2 right) {
            return ((left.X != right.X) || (left.Y != right.Y));
		}

		public static bool operator >(DVector2 left, DVector2 right) {
			return ((left.X > right.X) && (left.Y > right.Y));
		}

        public static bool operator <(DVector2 left, DVector2 right) {
			return ((left.X < right.X) && (left.Y < right.Y));
		}

        public static bool operator >=(DVector2 left, DVector2 right) {
			return ((left.X >= right.X) && (left.Y >= right.Y));
		}

        public static bool operator <=(DVector2 left, DVector2 right) {
			return ((left.X <= right.X) && (left.Y <= right.Y));
		}

        public static DVector2 operator -(DVector2 vector) {
            return DVector2.Negate(vector);
		}

        public static DVector2 operator +(DVector2 left, DVector2 right) {
            return DVector2.Add(left, right);
		}

        public static DVector2 operator +(DVector2 vector, double scalar) {
            return DVector2.Add(vector, scalar);
		}

        public static DVector2 operator +(double scalar, DVector2 vector) {
            return DVector2.Add(vector, scalar);
		}

        public static DVector2 operator -(DVector2 left, DVector2 right) {
            return DVector2.Subtract(left, right);
		}

		public static DVector2 operator -(DVector2 vector, double scalar) {
            return DVector2.Subtract(vector, scalar);
		}

        public static DVector2 operator -(double scalar, DVector2 vector) {
            return DVector2.Subtract(scalar, vector);
		}

		public static DVector2 operator *(DVector2 vector, double scalar) {
            return DVector2.Multiply(vector, scalar);
		}

        public static DVector2 operator *(double scalar, DVector2 vector) {
            return DVector2.Multiply(vector, scalar);
		}

        public static DVector2 operator /(DVector2 vector, double scalar) {
            return DVector2.Divide(vector, scalar);
		}

        public static DVector2 operator /(double scalar, DVector2 vector) {
            return DVector2.Divide(scalar, vector);
		}

        public double this[int index] {
			get { switch (index) {
					case  0: return X;
					case  1: return Y;
					default: throw new IndexOutOfRangeException();
			}}
			set { switch (index) {
					case  0: X = value; break;
					case  1: Y = value; break;
					default: throw new IndexOutOfRangeException();
			}}
		}

		public static explicit operator double[](DVector2 vector) {
            return new double[] { vector.X, vector.Y };
		}

		public static explicit operator List<double>(DVector2 vector) {
			List<double> list = new List<double>(2);
			list.Add(vector.X);  list.Add(vector.Y);  
			return list;
		}

		public static explicit operator LinkedList<double>(DVector2 vector) {
			LinkedList<double> list = new LinkedList<double>();
			list.AddLast(vector.X);  list.AddLast(vector.Y); 
			return list;
		}

        public static explicit operator PointF(DVector2 vector) {
            return new PointF((float)vector.X, (float)vector.Y);
		}

        public static explicit operator Point(DVector2 vector) {
            return new Point((int)vector.X, (int)vector.Y);
		}
        #endregion
    }


	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct DVector3 : ICloneable, IEquatable<DVector3>, IFormattable
	{
		public double X;
		public double Y;
		public double Z;

        #region Конструкторы
        
        public DVector3(double x, double y, double z) {
			X = x;  Y = y;   Z = z;
		}

		public DVector3(double[] coordinates) {
			Debug.Assert(coordinates != null && coordinates.Length == 3);
			X = coordinates[0];  Y = coordinates[1];  Z = coordinates[2];
		}

		public DVector3(List<double> coordinates) {
			Debug.Assert(coordinates != null && coordinates.Count == 3);
			X = coordinates[0];  Y = coordinates[1];  Z = coordinates[2];
		}

        public DVector3(decimal[] elements) : this(elements.Select(e => (double)e).ToArray()) { }

        public DVector3(DVector2 vector, double z) {
			X = vector.X;  Y = vector.Y;  Z = z;
		}

		public DVector3(DVector3 vector) {
			X = vector.X;  Y = vector.Y;  Z = vector.Z;
		}
        #endregion

        #region Константы

        /// <summary>
        /// Получает вектор, три элемента которого равны нулю.
		/// </summary>
        public static readonly DVector3 Zero  = new DVector3(0.0, 0.0, 0.0);
        /// <summary>
		/// Получает вектор, три элемента которого равны единице.
		/// </summary>
        public static readonly DVector3 One   = new DVector3(1.0, 1.0, 1.0);
		/// <summary>
		/// Получает вектор (1,0,0).
		/// </summary>
        public static readonly DVector3 UnitX = new DVector3(1.0, 0.0, 0.0);
		/// <summary>
		/// Получает вектор (0,1,0).
		/// </summary>
        public static readonly DVector3 UnitY = new DVector3(0.0, 1.0, 0.0);
		/// <summary>
		/// Получает вектор (0,0,1).
		/// </summary>
        public static readonly DVector3 UnitZ = new DVector3(0.0, 0.0, 1.0);
        #endregion

        #region Статические методы

        public static DVector3 Add(DVector3 left, DVector3 right) {
            return new DVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

        public static DVector3 Add(DVector3 vector, double scalar) {
            return new DVector3(vector.X + scalar, vector.Y + scalar, vector.Z + scalar);
		}

        public static void Add(DVector3 left, DVector3 right, ref DVector3 result) {
			result.X = left.X + right.X;
			result.Y = left.Y + right.Y;
			result.Z = left.Z + right.Z;
		}

        public static void Add(DVector3 vector, double scalar, ref DVector3 result) {
			result.X = vector.X + scalar;
			result.Y = vector.Y + scalar;
			result.Z = vector.Z + scalar;
		}

        public static DVector3 Subtract(DVector3 left, DVector3 right) {
            return new DVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

        public static DVector3 Subtract(DVector3 vector, double scalar) {
            return new DVector3(vector.X - scalar, vector.Y - scalar, vector.Z - scalar);
		}

        public static DVector3 Subtract(double scalar, DVector3 vector) {
            return new DVector3(scalar - vector.X, scalar - vector.Y, scalar - vector.Z);
		}

        public static void Subtract(DVector3 left, DVector3 right, ref DVector3 result) {
			result.X = left.X - right.X;
			result.Y = left.Y - right.Y;
			result.Z = left.Z - right.Z;
		}

        public static void Subtract(DVector3 vector, double scalar, ref DVector3 result) {
			result.X = vector.X - scalar;
			result.Y = vector.Y - scalar;
			result.Z = vector.Z - scalar;
		}

        public static void Subtract(double scalar, DVector3 vector, ref DVector3 result) {
			result.X = scalar - vector.X;
			result.Y = scalar - vector.Y;
			result.Z = scalar - vector.Z;
		}

        public static DVector3 Divide(DVector3 left, DVector3 right) {
            return new DVector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
		}

        public static DVector3 Divide(DVector3 vector, double scalar) {
            return new DVector3(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
		}

        public static DVector3 Divide(double scalar, DVector3 vector) {
            return new DVector3(scalar / vector.X, scalar / vector.Y, scalar / vector.Z);
		}

        public static void Divide(DVector3 left, DVector3 right, ref DVector3 result) {
			result.X = left.X / right.X;
			result.Y = left.Y / right.Y;
			result.Z = left.Z / right.Z;
		}

        public static void Divide(DVector3 vector, double scalar, ref DVector3 result) {
			result.X = vector.X / scalar;
			result.Y = vector.Y / scalar;
			result.Z = vector.Z / scalar;
		}

        public static void Divide(double scalar, DVector3 vector, ref DVector3 result) {
			result.X = scalar / vector.X;
			result.Y = scalar / vector.Y;
			result.Z = scalar / vector.Z;
		}

        public static DVector3 Multiply(DVector3 vector, double scalar) {
            return new DVector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
		}

        public static void Multiply(DVector3 vector, double scalar, ref DVector3 result) {
			result.X = vector.X * scalar;
			result.Y = vector.Y * scalar;
			result.Z = vector.Z * scalar;
		}

        public static DVector3 Multiply(DVector3 left, DVector3 right) {
            return new DVector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        public static void Multiply(ref DVector3 left, DVector3 right) {
            left.X *= right.X;
            left.Y *= right.Y;
            left.Z *= right.Z;
        }

        public static double DotProduct(DVector3 left, DVector3 right) {
			return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
		}

        public static DVector3 CrossProduct(DVector3 left, DVector3 right) {
            return new DVector3(left.Y * right.Z - left.Z * right.Y,
				                left.Z * right.X - left.X * right.Z,
				                left.X * right.Y - left.Y * right.X);
		}

        public static void CrossProduct(DVector3 left, DVector3 right, ref DVector3 result) {
			result.X = left.Y * right.Z - left.Z * right.Y;
			result.Y = left.Z * right.X - left.X * right.Z;
			result.Z = left.X * right.Y - left.Y * right.X;
		}

        public static DVector4 CrossProduct(DVector4 left, DVector4 right) {
            return new DVector4(left.Y * right.Z - left.Z * right.Y,    // По сути это тоже вектороное произведение 3х
				                left.Z * right.X - left.X * right.Z,    // мерных векторов, представленных как 4х мерные
				                left.X * right.Y - left.Y * right.X,    // (последнее значение просто игнорируется)
                                0);
		}

        public static void CrossProduct(DVector4 left, DVector4 right, ref DVector4 result) {
			result.X = left.Y * right.Z - left.Z * right.Y;
			result.Y = left.Z * right.X - left.X * right.Z;
			result.Z = left.X * right.Y - left.Y * right.X;
            result.W = 0;
        }

        public static DVector3 Negate(DVector3 vector) {
            return new DVector3(-vector.X, -vector.Y, -vector.Z);
		}

        /// <summary>
        /// Отражает вектор от плоскости, заданной нормалью.
        /// </summary>
        /// <param name="vector">вектор, входящий в плоскость</param>
        /// <param name="normal">Вектор нормали к плоскости плоскость, направленный наружу.</param>
        /// <returns>Вектор равный по величене vector, но с отраженным направлением</returns>
	    public static DVector3 Reflect(DVector3 vector, DVector3 normal) {                          //  vector    ^
            // Из свойства векторного сложения vector - reflect = удвоенной проекции vector на normal      |     / surface
            // Т.к. вектор normal нормализованный, то dot(vector,normal) = |vector|*cos(vector,normal)     |   /   normal
            // что соответсвует модулю проекции. А произведение модуля проекции на вектор normal даст \\   | /   
            // саму проецию. Таким образом получается reflect = vector - 2*dot(vector,normal) * normal  \\ V-------->    
            Multiply(normal, 2 * DotProduct(vector, normal), ref normal);                           //    \\    reflected
	        Subtract(vector, normal, ref vector);                                                   //      \\  
	        return vector;                                                                          //        \\ surface
	    }

        public static bool ApproxEqual(DVector3 left, DVector3 right) {
			return ApproxEqual(left, right, Double.Epsilon);
		}

        public static bool ApproxEqual(DVector3 left, DVector3 right, double tolerance) {
			return ((System.Math.Abs(left.X - right.X) <= tolerance) &&
				    (System.Math.Abs(left.Y - right.Y) <= tolerance) &&
				    (System.Math.Abs(left.Z - right.Z) <= tolerance) );
		}
		#endregion

        #region Свойства и Методы

	    public DVector3 Multiply(DVector3 vector) {
	        return Multiply(this, vector);
	    }

        public DVector3 Multiplied(DVector3 vector) {
	        Multiply(ref this, vector);
            return this;
        }

        public double DotProduct(DVector3 vector) {
            return DotProduct(this, vector);
	    }

	    /// <summary>
	    /// Отражает данный вектор, входящий в плоскость, заданной нормалью.
	    /// </summary>
	    /// <param name="vector">вектор, входящий в плоскость</param>
	    /// <param name="normal">Вектор нормали к плоскости плоскость, направленный наружу.</param>
	    /// <returns>Вектор равный по величене, но с отраженным направлением</returns>
	    public DVector3 Reflect(DVector3 normal) {
	        return Reflect(this, normal);
	    }

        public void Normalize() {
			double length = GetLength();
			if (length == 0)
                throw new DivideByZeroException("Невозможно нормализовать вектор нулевой длинны");
			X /= length;
			Y /= length;
			Z /= length;
		}

	    public DVector3 Normalized() {
	        Normalize();
	        return this;
	    }

		public double GetLength() {
			return System.Math.Sqrt(X * X + Y * Y + Z * Z);
		}

		public double GetLengthSquared() {
			return (X * X + Y * Y + Z * Z);
		}

		public void ClampZero(double tolerance) {
            X = (tolerance > Math.Abs(X)) ? 0 : X;
            Y = (tolerance > Math.Abs(Y)) ? 0 : Y;
            Z = (tolerance > Math.Abs(Z)) ? 0 : Z;
		}

		public void ClampZero() {
            X = (Double.Epsilon > Math.Abs(X)) ? 0 : X;
            Y = (Double.Epsilon > Math.Abs(Y)) ? 0 : Y;
            Z = (Double.Epsilon > Math.Abs(Z)) ? 0 : Z;
		}

        public bool ApproxEqual(DVector3 vector) {
            return ApproxEqual(this, vector);
        }

        public bool ApproxEqual(DVector3 vector, double tolerance) {
            return ApproxEqual(this, vector, tolerance);
        }

        public double[] ToArray() {
	        return new double[3] { X, Y, Z };
	    }

        public float[] ToFloatArray() {
            return ToArray().Select(v => (float)v).ToArray();
	    }

        public decimal[] ToDecimalArray() {
            return ToArray().Select(v => (decimal)v).ToArray();
	    }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public DVector4 ToDVector4(double W) { unchecked {
	        return new DVector4( X, Y, Z, W );
	    }}
        #endregion

        #region Реализация интерфейса ICloneable

        object ICloneable.Clone() {
            return new DVector3(this);
        }

        public DVector3 Clone() {
            return new DVector3(this);
        }
        #endregion

        #region Реализация интерфейса IEquatable<Vector4>

        bool IEquatable<DVector3>.Equals(DVector3 v) {
            return (X == v.X) && (Y == v.Y) && (Z == v.Z);
        }

        public bool Equals(DVector3 v) {
            return (X == v.X) && (Y == v.Y) && (Z == v.Z);
        }
        #endregion

        #region Реализация интерфейса IFormattable

        public string ToString(string format) {
            return String.Format("({0}, {1}, {2})", X.ToString(format, null),
                Y.ToString(format, null), Z.ToString(format, null));
        }

        string IFormattable.ToString(string format, IFormatProvider provider) {
            return ToString(format, provider);
        }

        public string ToString(string format, IFormatProvider provider) {
            return String.Format("({0:}, {1}, {2})", X.ToString(format, provider), 
                Y.ToString(format, provider), Z.ToString(format, provider));
        }
        #endregion

        #region Перегрузки

        public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is DVector4) {
                DVector4 v = (DVector4)obj;
				return (X == v.X) && (Y == v.Y) && (Z == v.Z);
			}
			return false;
		}

		public override string ToString() {
			return string.Format("({0:}, {1}, {2})", X, Y, Z);
		}
		#endregion

        #region Операторы

        public static bool operator ==(DVector3 left, DVector3 right) {
			return ValueType.Equals(left, right);
		}

        public static bool operator !=(DVector3 left, DVector3 right) {
			return !ValueType.Equals(left, right);
		}

		public static bool operator >(DVector3 left, DVector3 right) {
			return ((left.X > right.X) && (left.Y > right.Y) && (left.Z > right.Z));
		}

        public static bool operator <(DVector3 left, DVector3 right) {
			return ((left.X < right.X) && (left.Y < right.Y) && (left.Z < right.Z));
		}

        public static bool operator >=(DVector3 left, DVector3 right) {
			return ((left.X >= right.X) && (left.Y >= right.Y) && (left.Z >= right.Z));
		}

        public static bool operator <=(DVector3 left, DVector3 right) {
			return ((left.X <= right.X) && (left.Y <= right.Y) && (left.Z <= right.Z));
		}

        public static DVector3 operator -(DVector3 vector) {
            return DVector3.Negate(vector);
		}

        public static DVector3 operator +(DVector3 left, DVector3 right) {
            return DVector3.Add(left, right);
		}

        public static DVector3 operator +(DVector3 vector, double scalar) {
            return DVector3.Add(vector, scalar);
		}

        public static DVector3 operator +(double scalar, DVector3 vector) {
            return DVector3.Add(vector, scalar);
		}

        public static DVector3 operator -(DVector3 left, DVector3 right) {
            return DVector3.Subtract(left, right);
		}

		public static DVector3 operator -(DVector3 vector, double scalar) {
            return DVector3.Subtract(vector, scalar);
		}

        public static DVector3 operator -(double scalar, DVector3 vector) {
            return DVector3.Subtract(scalar, vector);
		}

		public static DVector3 operator *(DVector3 vector, double scalar) {
            return DVector3.Multiply(vector, scalar);
		}

        public static DVector3 operator *(double scalar, DVector3 vector) {
            return DVector3.Multiply(vector, scalar);
		}

        public static DVector3 operator *(DVector3 left, DVector3 right) {
            return DVector3.CrossProduct(left, right);
		}

        public static DVector3 operator /(DVector3 vector, double scalar) {
            return DVector3.Divide(vector, scalar);
		}

        public static DVector3 operator /(double scalar, DVector3 vector) {
            return DVector3.Divide(scalar, vector);
		}

        public double this[int index] {
			get { switch (index) {
					case  0: return X;
					case  1: return Y;
					case  2: return Z;
					default: throw new IndexOutOfRangeException();
			}}
			set { switch (index) {
					case  0: X = value; break;
					case  1: Y = value; break;
					case  2: Z = value; break;
					default: throw new IndexOutOfRangeException();
			}}
		}

		public static explicit operator double[](DVector3 vector) {
            return new double[] { vector.X, vector.Y, vector.Z };
		}

		public static explicit operator List<double>(DVector3 vector) {
			List<double> list = new List<double>(3);
			list.Add(vector.X);  list.Add(vector.Y);  list.Add(vector.Z);
			return list;
		}

		public static explicit operator LinkedList<double>(DVector3 vector) {
			LinkedList<double> list = new LinkedList<double>();
			list.AddLast(vector.X);  list.AddLast(vector.Y);  list.AddLast(vector.Z);
			return list;
		}

        public static explicit operator DVector2(DVector3 vector) {
            return new DVector2(vector.X, vector.Y);
		}
        #endregion
    }


	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
    public struct DVector4 : ICloneable, IEquatable<DVector4>, IFormattable
	{
		public double X;
        public double Y;
        public double Z;
        public double W;

        #region Конструкторы

        public DVector4(double x, double y, double z, double w) {
			X = x;  Y = y;  Z = z;  W = w;
		}

		public DVector4(double[] coordinates) {
			Debug.Assert(coordinates != null && coordinates.Length == 4);
			X = coordinates[0];     Y = coordinates[1];
			Z = coordinates[2];     W = coordinates[3];
		}

		public DVector4(List<double> coordinates) {
			Debug.Assert(coordinates != null && coordinates.Count == 4);
			X = coordinates[0];     Y = coordinates[1];
			Z = coordinates[2];     W = coordinates[3];
		}

        public DVector4(decimal[] elements) : this(elements.Select(e => (double)e).ToArray()) { }

        public DVector4(DVector2 vector, double z, double w) {
			X = vector.X;   Y = vector.Y;   Z = z;   W = w;
		}

        public DVector4(DVector3 vector, double w) {
			X = vector.X;   Y = vector.Y;
			Z = vector.Z;   W = w;
		}

		public DVector4(DVector4 vector) {
			X = vector.X;   Y = vector.Y;
			Z = vector.Z;   W = vector.W;
		}
        #endregion

        #region Константы

        /// <summary>
		/// Получает вектор, четыре элемента которого равны нулю.
		/// </summary>
        public static readonly DVector4 Zero  = new DVector4(0.0, 0.0, 0.0, 0.0);
        /// <summary>
		/// Получает вектор, четыре элемента которого равны единице.
		/// </summary>
        public static readonly DVector4 One   = new DVector4(1.0, 1.0, 1.0, 1.0);
		/// <summary>
		/// Получает вектор (1,0,0,0).
		/// </summary>
        public static readonly DVector4 UnitX = new DVector4(1.0, 0.0, 0.0, 0.0);
		/// <summary>
		/// Получает вектор (0,1,0,0).
		/// </summary>
        public static readonly DVector4 UnitY = new DVector4(0.0, 1.0, 0.0, 0.0);
		/// <summary>
		/// Получает вектор (0,0,1,0).
		/// </summary>
        public static readonly DVector4 UnitZ = new DVector4(0.0, 0.0, 1.0, 0.0);
		/// <summary>
		/// Получает вектор (0,0,0,1).
		/// </summary>
        public static readonly DVector4 UnitW = new DVector4(0.0, 0.0, 0.0, 1.0);
        #endregion

        #region Статические методы

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DVector4 Add(DVector4 left, DVector4 right) { unchecked {
			return new DVector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Add(DVector4 vector, double scalar) { unchecked {
			return new DVector4(vector.X + scalar, vector.Y + scalar, vector.Z + scalar, vector.W + scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(DVector4 left, DVector4 right, ref DVector4 result) { unchecked {
			result.X = left.X + right.X;
			result.Y = left.Y + right.Y;
			result.Z = left.Z + right.Z;
			result.W = left.W + right.W;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(DVector4 vector, double scalar, ref DVector4 result) { unchecked {
			result.X = vector.X + scalar;
			result.Y = vector.Y + scalar;
			result.Z = vector.Z + scalar;
			result.W = vector.W + scalar;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Subtract(DVector4 left, DVector4 right) { unchecked {
			return new DVector4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Subtract(DVector4 vector, double scalar) { unchecked {
			return new DVector4(vector.X - scalar, vector.Y - scalar, vector.Z - scalar, vector.W - scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Subtract(double scalar, DVector4 vector) { unchecked {
			return new DVector4(scalar - vector.X, scalar - vector.Y, scalar - vector.Z, scalar - vector.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Subtract(DVector4 left, DVector4 right, ref DVector4 result) { unchecked {
			result.X = left.X - right.X;
			result.Y = left.Y - right.Y;
			result.Z = left.Z - right.Z;
			result.W = left.W - right.W;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Subtract(DVector4 vector, double scalar, ref DVector4 result) { unchecked {
			result.X = vector.X - scalar;
			result.Y = vector.Y - scalar;
			result.Z = vector.Z - scalar;
			result.W = vector.W - scalar;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Subtract(double scalar, DVector4 vector, ref DVector4 result) { unchecked {
			result.X = scalar - vector.X;
			result.Y = scalar - vector.Y;
			result.Z = scalar - vector.Z;
			result.W = scalar - vector.W;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Divide(DVector4 left, DVector4 right) { unchecked {
			return new DVector4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Divide(DVector4 vector, double scalar) { unchecked {
			return new DVector4(vector.X / scalar, vector.Y / scalar, vector.Z / scalar, vector.W / scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Divide(double scalar, DVector4 vector) { unchecked {
			return new DVector4(scalar / vector.X, scalar / vector.Y, scalar / vector.Z, scalar / vector.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Divide(DVector4 left, DVector4 right, ref DVector4 result) { unchecked {
			result.X = left.X / right.X;
			result.Y = left.Y / right.Y;
			result.Z = left.Z / right.Z;
			result.W = left.W / right.W;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Divide(DVector4 vector, double scalar, ref DVector4 result) { unchecked {
			result.X = vector.X / scalar;
			result.Y = vector.Y / scalar;
			result.Z = vector.Z / scalar;
			result.W = vector.W / scalar;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Divide(double scalar, DVector4 vector, ref DVector4 result) { unchecked {
			result.X = scalar / vector.X;
			result.Y = scalar / vector.Y;
			result.Z = scalar / vector.Z;
			result.W = scalar / vector.W;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Multiply(DVector4 vector, double scalar) { unchecked {
			return new DVector4(vector.X * scalar, vector.Y * scalar, vector.Z * scalar, vector.W * scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DVector4 Multiply(DVector4 left, DVector4 right) { unchecked {
            return new DVector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref DVector4 left, DVector4 right) { unchecked {
            left.X *= right.X;
            left.Y *= right.Y;
            left.Z *= right.Z;
            left.W *= right.W;
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiply(DVector4 vector, double scalar, ref DVector4 result) { unchecked {
			result.X = vector.X * scalar;
			result.Y = vector.Y * scalar;
			result.Z = vector.Z * scalar;
			result.W = vector.W * scalar;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double DotProduct(DVector4 left, DVector4 right) { unchecked {
			return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 Negate(DVector4 vector) { unchecked {
			return new DVector4(-vector.X, -vector.Y, -vector.Z, -vector.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ApproxEqual(DVector4 left, DVector4 right) { unchecked {
			return ApproxEqual(left, right, Double.Epsilon);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ApproxEqual(DVector4 left, DVector4 right, double tolerance) { unchecked {
			return ((System.Math.Abs(left.X - right.X) <= tolerance) &&
				    (System.Math.Abs(left.Y - right.Y) <= tolerance) &&
				    (System.Math.Abs(left.Z - right.Z) <= tolerance) &&
				    (System.Math.Abs(left.W - right.W) <= tolerance) );
		}}
		#endregion

        #region Свойства и Методы

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DVector4 Multiply(DVector4 vector) { unchecked {
	        return Multiply(this, vector);
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DVector4 Multiplied(DVector4 vector) { unchecked {
	        Multiply(ref this, vector);
            return this;
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DotProduct(DVector4 vector) { unchecked {
            return DotProduct(this, vector);
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize() { unchecked {
			double length = GetLength();
			if (length == 0)
                throw new DivideByZeroException("Невозможно нормализовать вектор нулевой длинны");
			X /= length;
			Y /= length;
			Z /= length;
			W /= length;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DVector4 Normalized() { unchecked {
	        Normalize();
	        return this;
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double GetLength() { unchecked {
			return System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double GetLengthSquared() { unchecked {
			return (X * X + Y * Y + Z * Z + W * W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClampZero(double tolerance) { unchecked {
            X = (tolerance > Math.Abs(X)) ? 0 : X;
            Y = (tolerance > Math.Abs(Y)) ? 0 : Y;
            Z = (tolerance > Math.Abs(Z)) ? 0 : Z;
            W = (tolerance > Math.Abs(W)) ? 0 : W;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClampZero() { unchecked {
            X = (Double.Epsilon > Math.Abs(X)) ? 0 : X;
            Y = (Double.Epsilon > Math.Abs(Y)) ? 0 : Y;
            Z = (Double.Epsilon > Math.Abs(Z)) ? 0 : Z;
            W = (Double.Epsilon > Math.Abs(W)) ? 0 : W;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public double[] ToArray() { unchecked {
	        return new double[4] { X, Y, Z, W };
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float[] ToFloatArray() { unchecked {
            return ToArray().Select(v => (float)v).ToArray();
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal[] ToDecimalArray() { unchecked {
            return ToArray().Select(v => (decimal)v).ToArray();
	    }}
        #endregion

        #region Реализация интерфейса ICloneable

        object ICloneable.Clone() {
            return new DVector4(this);
        }

        public DVector4 Clone() {
            return new DVector4(this);
        }
        #endregion

        #region Реализация интерфейса IEquatable<Vector4>

        bool IEquatable<DVector4>.Equals(DVector4 v) {
            return (X == v.X) && (Y == v.Y) && (Z == v.Z) && (W == v.W);
        }

        public bool Equals(DVector4 v) {
            return (X == v.X) && (Y == v.Y) && (Z == v.Z) && (W == v.W);
        }
        #endregion

        #region Реализация интерфейса IFormattable

        public string ToString(string format) {
            return String.Format("({0:}, {1}, {2}, {3})", X.ToString(format, null),
                Y.ToString(format, null), Z.ToString(format, null), W.ToString(format, null));
        }

        string IFormattable.ToString(string format, IFormatProvider provider) {
            return ToString(format, provider);
        }

        public string ToString(string format, IFormatProvider provider) {
            return String.Format("({0:}, {1}, {2}, {3})", X.ToString(format, provider), 
                Y.ToString(format, provider), Z.ToString(format, provider), W.ToString(format, provider));
        }
        #endregion

        #region Перегрузки

        public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is DVector4) {
                DVector4 v = (DVector4)obj;
				return (X == v.X) && (Y == v.Y) && (Z == v.Z) && (W == v.W);
			}
			return false;
		}

		public override string ToString() {
			return string.Format("({0:}, {1}, {2}, {3})", X, Y, Z, W);
		}
		#endregion

        #region Операторы

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DVector4 left, DVector4 right) { unchecked {
			return ValueType.Equals(left, right);
		}}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(DVector4 left, DVector4 right) { unchecked {
			return !ValueType.Equals(left, right);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(DVector4 left, DVector4 right) { unchecked {
			return( (left.X > right.X) && (left.Y > right.Y) &&
				    (left.Z > right.Z) && (left.W > right.W));
		}}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <(DVector4 left, DVector4 right) { unchecked {
			return( (left.X < right.X) && (left.Y < right.Y) &&
				    (left.Z < right.Z) && (left.W < right.W)); 
        }}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >=(DVector4 left, DVector4 right) { unchecked {
			return( (left.X >= right.X) && (left.Y >= right.Y) &&
				    (left.Z >= right.Z) && (left.W >= right.W));
		}}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <=(DVector4 left, DVector4 right) { unchecked {
			return( (left.X <= right.X) && (left.Y <= right.Y) &&
				    (left.Z <= right.Z) && (left.W <= right.W)); 
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator -(DVector4 vector) { unchecked {
			return DVector4.Negate(vector);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator +(DVector4 left, DVector4 right) { unchecked {
			return DVector4.Add(left, right);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator +(DVector4 vector, double scalar) { unchecked {
			return DVector4.Add(vector, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator +(double scalar, DVector4 vector) { unchecked {
			return DVector4.Add(vector, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator -(DVector4 left, DVector4 right) { unchecked {
			return DVector4.Subtract(left, right);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator -(DVector4 vector, double scalar) { unchecked {
			return DVector4.Subtract(vector, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator -(double scalar, DVector4 vector) { unchecked {
			return DVector4.Subtract(scalar, vector);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator *(DVector4 vector, double scalar) { unchecked {
			return DVector4.Multiply(vector, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator *(double scalar, DVector4 vector) { unchecked {
			return DVector4.Multiply(vector, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DVector4 operator *(DVector4 left, DVector4 right) { unchecked {
            return DVector3.CrossProduct(left, right);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator /(DVector4 vector, double scalar) { unchecked {
			return DVector4.Divide(vector, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DVector4 operator /(double scalar, DVector4 vector) { unchecked {
			return DVector4.Divide(scalar, vector);
		}}

		public double this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { unchecked { switch (index) {
					case  0: return X;
					case  1: return Y;
					case  2: return Z;
					case  3: return W;
					default: throw new IndexOutOfRangeException();
			}}}
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
			set { unchecked { switch (index) {
					case  0: X = value; break;
					case  1: Y = value; break;
					case  2: Z = value; break;
					case  3: W = value; break;
					default: throw new IndexOutOfRangeException();
			}}}
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator double[](DVector4 vector) { unchecked {
            return new double[] { vector.X, vector.Y, vector.Z, vector.W };
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator List<double>(DVector4 vector) { unchecked {
			List<double> list = new List<double>(4);
			list.Add(vector.X);  list.Add(vector.Y);  list.Add(vector.Z);  list.Add(vector.W);
			return list;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator LinkedList<double>(DVector4 vector) { unchecked {
			LinkedList<double> list = new LinkedList<double>();
			list.AddLast(vector.X);  list.AddLast(vector.Y);  list.AddLast(vector.Z);  list.AddLast(vector.W);
			return list;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator DVector3(DVector4 vector) { unchecked {
            return new DVector3(vector.X, vector.Y, vector.Z);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator DVector2(DVector4 vector) {
            return new DVector2(vector.X, vector.Y);
		}
        #endregion
	}

	
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct DMatrix3 : ICloneable, IEquatable<DMatrix3>, IFormattable
    {
        public double M11, M12, M13;
        public double M21, M22, M23;
        public double M31, M32, M33;

        #region Конструкторы

        public DMatrix3( double m11, double m12, double m13,
			             double m21, double m22, double m23,
			             double m31, double m32, double m33 ) {
			M11 = m11; M12 = m12; M13 = m13;
			M21 = m21; M22 = m22; M23 = m23;
			M31 = m31; M32 = m32; M33 = m33;
		}

        public DMatrix3(double[] elements) {
			Debug.Assert(elements != null && elements.Length == 9);
			M11 = elements[ 0]; M12 = elements[ 1]; M13 = elements[ 2];
			M21 = elements[ 3]; M22 = elements[ 4]; M23 = elements[ 5];
			M31 = elements[ 6]; M32 = elements[ 7]; M33 = elements[ 8];
		}

        public DMatrix3(List<double> elements) {
            Debug.Assert(elements != null && elements.Count == 9);
			M11 = elements[ 0]; M12 = elements[ 1]; M13 = elements[ 2];
			M21 = elements[ 3]; M22 = elements[ 4]; M23 = elements[ 5];
			M31 = elements[ 6]; M32 = elements[ 7]; M33 = elements[ 8];
		}

        public DMatrix3(decimal[] elements) : this(elements.Select(e => (double)e).ToArray()) { }

        public DMatrix3(DMatrix3 m) {
            M11 = m.M11; M12 = m.M12; M13 = m.M13;
            M21 = m.M21; M22 = m.M22; M23 = m.M23;
            M31 = m.M31; M32 = m.M32; M33 = m.M33;
		}
        #endregion

        #region Константы

        public static readonly DMatrix3 Zero = new DMatrix3(0, 0, 0, 0, 0, 0, 0, 0, 0);

        public static readonly DMatrix3 Identity = new DMatrix3(1, 0, 0,
                                                                0, 1, 0,
                                                                0, 0, 1 );
        #endregion

        #region Статические методы

		public static DMatrix3 Add(DMatrix3 left, DMatrix3 right) {
			return new DMatrix3(
				left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13, 
				left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23, 
				left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33);
		}

		public static DMatrix3 Add(DMatrix3 matrix, double scalar) {
			return new DMatrix3(
				matrix.M11 + scalar, matrix.M12 + scalar, matrix.M13 + scalar, 
				matrix.M21 + scalar, matrix.M22 + scalar, matrix.M23 + scalar, 
				matrix.M31 + scalar, matrix.M32 + scalar, matrix.M33 + scalar);
		}

		public static void Add(DMatrix3 left, DMatrix3 right, ref DMatrix3 result) {
			result.M11 = left.M11 + right.M11;
			result.M12 = left.M12 + right.M12;
			result.M13 = left.M13 + right.M13;

			result.M21 = left.M21 + right.M21;
			result.M22 = left.M22 + right.M22;
			result.M23 = left.M23 + right.M23;

			result.M31 = left.M31 + right.M31;
			result.M32 = left.M32 + right.M32;
			result.M33 = left.M33 + right.M33;
		}

		public static void Add(DMatrix3 matrix, double scalar, ref DMatrix3 result) {
			result.M11 = matrix.M11 + scalar;
			result.M12 = matrix.M12 + scalar;
			result.M13 = matrix.M13 + scalar;

			result.M21 = matrix.M21 + scalar;
			result.M22 = matrix.M22 + scalar;
			result.M23 = matrix.M23 + scalar;

			result.M31 = matrix.M31 + scalar;
			result.M32 = matrix.M32 + scalar;
			result.M33 = matrix.M33 + scalar;
		}

		public static DMatrix3 Subtract(DMatrix3 left, DMatrix3 right) {
			return new DMatrix3(
				left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13,
				left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23,
				left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33);
		}

		public static DMatrix3 Subtract(DMatrix3 matrix, double scalar) {
			return new DMatrix3(
				matrix.M11 - scalar, matrix.M12 - scalar, matrix.M13 - scalar,
				matrix.M21 - scalar, matrix.M22 - scalar, matrix.M23 - scalar,
				matrix.M31 - scalar, matrix.M32 - scalar, matrix.M33 - scalar);
		}

		public static void Subtract(DMatrix3 left, DMatrix3 right, ref DMatrix3 result) {
			result.M11 = left.M11 - right.M11;
			result.M12 = left.M12 - right.M12;
			result.M13 = left.M13 - right.M13;

			result.M21 = left.M21 - right.M21;
			result.M22 = left.M22 - right.M22;
			result.M23 = left.M23 - right.M23;

			result.M31 = left.M31 - right.M31;
			result.M32 = left.M32 - right.M32;
			result.M33 = left.M33 - right.M33;
		}

		public static void Subtract(DMatrix3 matrix, double scalar, ref DMatrix3 result) {
			result.M11 = matrix.M11 - scalar;
			result.M12 = matrix.M12 - scalar;
			result.M13 = matrix.M13 - scalar;

			result.M21 = matrix.M21 - scalar;
			result.M22 = matrix.M22 - scalar;
			result.M23 = matrix.M23 - scalar;

			result.M31 = matrix.M31 - scalar;
			result.M32 = matrix.M32 - scalar;
			result.M33 = matrix.M33 - scalar;
		}

		public static DMatrix3 Multiply(DMatrix3 left, DMatrix3 right) {
			return new DMatrix3(
				left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31,
				left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32,
				left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33,

				left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31,
				left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32,
				left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33,

				left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31,
				left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32,
				left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33);
		}

		public static void Multiply(DMatrix3 left, DMatrix3 right, ref DMatrix3 result) {
			result.M11 = left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31;
			result.M12 = left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32;
			result.M13 = left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33;

			result.M21 = left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31;
			result.M22 = left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32;
			result.M23 = left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33;

			result.M31 = left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31;
			result.M32 = left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32;
			result.M33 = left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33;
		}

		public static DVector3 Transform(DMatrix3 matrix, DVector3 vector) {
			return new DVector3(
				(matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z),
				(matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z),
				(matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z));
		}
		
		public static void Transform(DMatrix3 matrix, DVector3 vector, ref DVector3 result) {
			result.X = (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z);
			result.Y = (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z);
			result.Z = (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z);
		}

        public static DMatrix3 Invert(DMatrix3 matrix) {
            double invdet = 1 / matrix.GetDeterminant();
            return new DMatrix3() {
                M11 = invdet * (matrix.M22 * matrix.M33 - matrix.M32 * matrix.M23),
                M12 = invdet * (matrix.M13 * matrix.M32 - matrix.M12 * matrix.M33),
                M13 = invdet * (matrix.M12 * matrix.M23 - matrix.M13 * matrix.M22),

                M21 = invdet * (matrix.M23 * matrix.M31 - matrix.M21 * matrix.M33),
                M22 = invdet * (matrix.M11 * matrix.M33 - matrix.M13 * matrix.M31),
                M23 = invdet * (matrix.M21 * matrix.M13 - matrix.M11 * matrix.M23),
            
                M31 = invdet * (matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22),
                M32 = invdet * (matrix.M31 * matrix.M12 - matrix.M11 * matrix.M32),
                M33 = invdet * (matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12)
            };
        }

        public static DMatrix4 TransposeInvert(DMatrix4 matrix) {
            double det =  matrix.M11 * (matrix.M22 * matrix.M33 - matrix.M23 * matrix.M32)
                        - matrix.M12 * (matrix.M21 * matrix.M33 - matrix.M23 * matrix.M31) 
                        + matrix.M13 * (matrix.M21 * matrix.M32 - matrix.M22 * matrix.M31);
            double invdet = 1 / det;
            return new DMatrix4() {
                M11 = invdet * (matrix.M22 * matrix.M33 - matrix.M32 * matrix.M23),
                M12 = invdet * (matrix.M23 * matrix.M31 - matrix.M21 * matrix.M33),
                M13 = invdet * (matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22),
                M21 = invdet * (matrix.M13 * matrix.M32 - matrix.M12 * matrix.M33),
                M22 = invdet * (matrix.M11 * matrix.M33 - matrix.M13 * matrix.M31),
                M23 = invdet * (matrix.M31 * matrix.M12 - matrix.M11 * matrix.M32),
                M31 = invdet * (matrix.M12 * matrix.M23 - matrix.M13 * matrix.M22),
                M32 = invdet * (matrix.M21 * matrix.M13 - matrix.M11 * matrix.M23),
                M33 = invdet * (matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12),
                M14 = 0, M24 = 0, M34 = 0, M41 = 0, M42 = 0, M43 = 0, M44 = 0
            };
        }

        public static DMatrix4 NormalVecTransf(DMatrix4 matrix) {
            return new DMatrix4(
                matrix.M33 * matrix.M22 - matrix.M23 * matrix.M32, 
                matrix.M23 * matrix.M31 - matrix.M21 * matrix.M33, 
                matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22, 0,
                matrix.M13 * matrix.M32 - matrix.M33 * matrix.M12,
                matrix.M33 * matrix.M11 - matrix.M13 * matrix.M31,
                matrix.M31 * matrix.M12 - matrix.M11 * matrix.M32, 0,
                matrix.M23 * matrix.M12 - matrix.M13 * matrix.M22,
                matrix.M21 * matrix.M13 - matrix.M23 * matrix.M11,
                matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12, 0,
                0,   0,   0,   0);
        }

		public static DMatrix3 Transpose(DMatrix3 m) {
			DMatrix3 t = new DMatrix3(m);
			t.Transpose();
			return t;
		}

        public static bool ApproxEqual(DMatrix3 left, DMatrix3 right, double tolerance) {
			return ((System.Math.Abs(left.M11 - right.M11) <= tolerance) &&
                    (System.Math.Abs(left.M12 - right.M12) <= tolerance) &&
				    (System.Math.Abs(left.M13 - right.M13) <= tolerance) &&

                    (System.Math.Abs(left.M21 - right.M21) <= tolerance) &&
                    (System.Math.Abs(left.M22 - right.M22) <= tolerance) &&
				    (System.Math.Abs(left.M23 - right.M23) <= tolerance) &&

                    (System.Math.Abs(left.M31 - right.M31) <= tolerance) &&
                    (System.Math.Abs(left.M32 - right.M32) <= tolerance) &&
				    (System.Math.Abs(left.M33 - right.M33) <= tolerance) );
		}
		#endregion

        #region Свойства и Методы

        public double Trace {
			get { return M11 + M22 + M33; }
		}  

		public double GetDeterminant() {
            return  M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32 -
                    M13 * M22 * M31 - M11 * M23 * M32 - M12 * M21 * M33;
		}

        public DMatrix3 Invert() {
            return Invert(this);
        }

		public void Transpose() {
		    double temp;
		    temp = M12;  M12 = M21;  M21 = temp;
            temp = M13;  M13 = M31;  M31 = temp;
            temp = M23;  M23 = M32;  M32 = temp;
		}

        public double[] ToArray(bool glorder = false) {
            return glorder 
                ? new double[9] { M11, M21, M31, M12, M22, M32, M13, M23, M33 }
                : new double[9] { M11, M12, M13, M21, M22, M23, M31, M32, M33 };
	    }

        public float[] ToFloatArray(bool glorder = false) {
            return ToArray(glorder).Select(v => (float)v).ToArray();
	    }

        public decimal[] ToDecimalArray(bool glorder = false) {
            return ToArray(glorder).Select(v => (decimal)v).ToArray();
	    }
        #endregion

        #region Реализация интерфейса ICloneable

        object ICloneable.Clone() {
            return new DMatrix3(this);
        }

        public DMatrix3 Clone() {
            return new DMatrix3(this);
        }
        #endregion

        #region Реализация интерфейса IEquatable<Vector4>

        bool IEquatable<DMatrix3>.Equals(DMatrix3 m) {
            return  (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) &&
				    (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) &&
				    (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33);
        }

        public bool Equals(DMatrix3 m) {
            return  (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) &&
				    (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) && 
				    (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33);
        }
        #endregion

        #region Реализация интерфейса IFormattable

        public string ToString(string format) {
            return string.Format("3x3[{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}]",
                M11.ToString(format, null), M12.ToString(format, null), M13.ToString(format, null),
                M21.ToString(format, null), M22.ToString(format, null), M23.ToString(format, null),
                M31.ToString(format, null), M32.ToString(format, null), M33.ToString(format, null));
        }

        string IFormattable.ToString(string format, IFormatProvider provider) {
            return ToString(format, provider);
        }

        public string ToString(string format, IFormatProvider provider) {
            return string.Format("3x3[{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}]",
                M11.ToString(format, provider), M12.ToString(format, provider), M13.ToString(format, provider),
                M21.ToString(format, provider), M22.ToString(format, provider), M23.ToString(format, provider),
                M31.ToString(format, provider), M32.ToString(format, provider), M33.ToString(format, provider));
        }
        #endregion

        #region Перегрузки

        public override int GetHashCode() {
			return  M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^
				    M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^
				    M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is DMatrix3) {
                DMatrix3 m = (DMatrix3)obj;
				return  (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) &&
					    (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) &&
					    (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33);
			}
			return false;
		}

		public override string ToString() {
			return string.Format("3x3[{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}]",
				M11, M12, M13, M21, M22, M23, M31, M32, M33);
		}
		#endregion

        #region Операторы

         public static bool operator ==(DMatrix3 left, DMatrix3 right) {
			return ValueType.Equals(left, right);
		}

        public static bool operator !=(DMatrix3 left, DMatrix3 right) {
			return !ValueType.Equals(left, right);
		}

        public static DMatrix3 operator +(DMatrix3 left, DMatrix3 right) {
            return DMatrix3.Add(left, right); ;
		}

        public static DMatrix3 operator +(DMatrix3 matrix, double scalar) {
            return DMatrix3.Add(matrix, scalar);
		}

        public static DMatrix3 operator +(double scalar, DMatrix3 matrix) {
            return DMatrix3.Add(matrix, scalar);
		}

        public static DMatrix3 operator -(DMatrix3 left, DMatrix3 right) {
            return DMatrix3.Subtract(left, right); ;
		}

        public static DMatrix3 operator -(DMatrix3 matrix, double scalar) {
            return DMatrix3.Subtract(matrix, scalar);
		}

        public static DMatrix3 operator *(DMatrix3 left, DMatrix3 right) {
            return DMatrix3.Multiply(left, right); ;
		}

        public static DVector3 operator *(DMatrix3 matrix, DVector3 vector) {
            return DMatrix3.Transform(matrix, vector);
		}

		public unsafe double this[int index] {
			get {
				if (index < 0 || index >= 9)
					throw new IndexOutOfRangeException("Недопустимый индекс элемента матрицы!");
				fixed (double* f = &M11)
					return *(f + index);
			}
			set {
				if (index < 0 || index >= 9)
                    throw new IndexOutOfRangeException("Недопустимый индекс элемента матрицы!");
				fixed (double* f = &M11)
					*(f + index) = value;
			}
		}

		public double this[int row, int column] {
			get { return this[(row-1) * 3 + (column-1)]; }
			set { this[(row-1) * 3 + (column-1)] = value; }
		}

        #endregion
    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct DMatrix4 : ICloneable, IEquatable<DMatrix4>, IFormattable
    {
        public double M11, M12, M13, M14;
        public double M21, M22, M23, M24;
        public double M31, M32, M33, M34;
        public double M41, M42, M43, M44;

        #region Конструкторы

        public DMatrix4( double m11, double m12, double m13, double m14,
			             double m21, double m22, double m23, double m24,
			             double m31, double m32, double m33, double m34,
			             double m41, double m42, double m43, double m44 ) {
			M11 = m11; M12 = m12; M13 = m13; M14 = m14;
			M21 = m21; M22 = m22; M23 = m23; M24 = m24;
			M31 = m31; M32 = m32; M33 = m33; M34 = m34;
			M41 = m41; M42 = m42; M43 = m43; M44 = m44;
		}

        public DMatrix4(double[] elements) {
			Debug.Assert(elements != null && elements.Length == 16);
			M11 = elements[ 0]; M12 = elements[ 1]; M13 = elements[ 2]; M14 = elements[ 3];
			M21 = elements[ 4]; M22 = elements[ 5]; M23 = elements[ 6]; M24 = elements[ 7];
			M31 = elements[ 8]; M32 = elements[ 9]; M33 = elements[10]; M34 = elements[11];
			M41 = elements[12]; M42 = elements[13]; M43 = elements[14]; M44 = elements[15];
		}

        public DMatrix4(List<double> elements) {
            Debug.Assert(elements != null && elements.Count == 16);
			M11 = elements[ 0]; M12 = elements[ 1]; M13 = elements[ 2]; M14 = elements[ 3];
			M21 = elements[ 4]; M22 = elements[ 5]; M23 = elements[ 6]; M24 = elements[ 7];
			M31 = elements[ 8]; M32 = elements[ 9]; M33 = elements[10]; M34 = elements[11];
			M41 = elements[12]; M42 = elements[13]; M43 = elements[14]; M44 = elements[15];
		}

        public DMatrix4(decimal[] elements) : this(elements.Select(e => (double)e).ToArray()) { }

        public DMatrix4(DMatrix4 m) {
            M11 = m.M11; M12 = m.M12; M13 = m.M13; M14 = m.M14;
            M21 = m.M21; M22 = m.M22; M23 = m.M23; M24 = m.M24;
            M31 = m.M31; M32 = m.M32; M33 = m.M33; M34 = m.M34;
            M41 = m.M41; M42 = m.M42; M43 = m.M43; M44 = m.M44;
		}
        #endregion

        #region Константы

        public static readonly DMatrix4 Zero = new DMatrix4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        public static readonly DMatrix4 Identity = new DMatrix4(1, 0, 0, 0,
                                                                0, 1, 0, 0,
                                                                0, 0, 1, 0,
                                                                0, 0, 0, 1 );
        #endregion

        #region Статические методы

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 Add(DMatrix4 left, DMatrix4 right) { unchecked {
            return new DMatrix4(
				left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13, left.M14 + right.M14,
				left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23, left.M24 + right.M24,
				left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33, left.M34 + right.M34,
				left.M41 + right.M41, left.M42 + right.M42, left.M43 + right.M43, left.M44 + right.M44);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 Add(DMatrix4 matrix, double scalar) { unchecked {
            return new DMatrix4(
				matrix.M11 + scalar, matrix.M12 + scalar, matrix.M13 + scalar, matrix.M14 + scalar,
				matrix.M21 + scalar, matrix.M22 + scalar, matrix.M23 + scalar, matrix.M24 + scalar,
				matrix.M31 + scalar, matrix.M32 + scalar, matrix.M33 + scalar, matrix.M34 + scalar,
				matrix.M41 + scalar, matrix.M42 + scalar, matrix.M43 + scalar, matrix.M44 + scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(DMatrix4 left, DMatrix4 right, ref DMatrix4 result) { unchecked {
			result.M11 = left.M11 + right.M11;
			result.M12 = left.M12 + right.M12;
			result.M13 = left.M13 + right.M13;
			result.M14 = left.M14 + right.M14;

			result.M21 = left.M21 + right.M21;
			result.M22 = left.M22 + right.M22;
			result.M23 = left.M23 + right.M23;
			result.M24 = left.M24 + right.M24;

			result.M31 = left.M31 + right.M31;
			result.M32 = left.M32 + right.M32;
			result.M33 = left.M33 + right.M33;
			result.M34 = left.M34 + right.M34;

			result.M41 = left.M41 + right.M41;
			result.M42 = left.M42 + right.M42;
			result.M43 = left.M43 + right.M43;
			result.M44 = left.M44 + right.M44;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(DMatrix4 matrix, double scalar, ref DMatrix4 result) { unchecked {
			result.M11 = matrix.M11 + scalar;
			result.M12 = matrix.M12 + scalar;
			result.M13 = matrix.M13 + scalar;
			result.M14 = matrix.M14 + scalar;

			result.M21 = matrix.M21 + scalar;
			result.M22 = matrix.M22 + scalar;
			result.M23 = matrix.M23 + scalar;
			result.M24 = matrix.M24 + scalar;

			result.M31 = matrix.M31 + scalar;
			result.M32 = matrix.M32 + scalar;
			result.M33 = matrix.M33 + scalar;
			result.M34 = matrix.M34 + scalar;

			result.M41 = matrix.M41 + scalar;
			result.M42 = matrix.M42 + scalar;
			result.M43 = matrix.M43 + scalar;
			result.M44 = matrix.M44 + scalar;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 Subtract(DMatrix4 left, DMatrix4 right) { unchecked {
            return new DMatrix4(
				left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13, left.M14 - right.M14,
				left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23, left.M24 - right.M24,
				left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33, left.M34 - right.M34,
				left.M41 - right.M41, left.M42 - right.M42, left.M43 - right.M43, left.M44 - right.M44);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 Subtract(DMatrix4 matrix, double scalar) { unchecked {
            return new DMatrix4(
				matrix.M11 - scalar, matrix.M12 - scalar, matrix.M13 - scalar, matrix.M14 - scalar,
				matrix.M21 - scalar, matrix.M22 - scalar, matrix.M23 - scalar, matrix.M24 - scalar,
				matrix.M31 - scalar, matrix.M32 - scalar, matrix.M33 - scalar, matrix.M34 - scalar,
				matrix.M41 - scalar, matrix.M42 - scalar, matrix.M43 - scalar, matrix.M44 - scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(DMatrix4 left, DMatrix4 right, ref DMatrix4 result) { unchecked {
			result.M11 = left.M11 - right.M11;
			result.M12 = left.M12 - right.M12;
			result.M13 = left.M13 - right.M13;
			result.M14 = left.M14 - right.M14;

			result.M21 = left.M21 - right.M21;
			result.M22 = left.M22 - right.M22;
			result.M23 = left.M23 - right.M23;
			result.M24 = left.M24 - right.M24;

			result.M31 = left.M31 - right.M31;
			result.M32 = left.M32 - right.M32;
			result.M33 = left.M33 - right.M33;
			result.M34 = left.M34 - right.M34;

			result.M41 = left.M41 - right.M41;
			result.M42 = left.M42 - right.M42;
			result.M43 = left.M43 - right.M43;
			result.M44 = left.M44 - right.M44;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(DMatrix4 matrix, double scalar, ref DMatrix4 result) { unchecked {
			result.M11 = matrix.M11 - scalar;
			result.M12 = matrix.M12 - scalar;
			result.M13 = matrix.M13 - scalar;
			result.M14 = matrix.M14 - scalar;

			result.M21 = matrix.M21 - scalar;
			result.M22 = matrix.M22 - scalar;
			result.M23 = matrix.M23 - scalar;
			result.M24 = matrix.M24 - scalar;

			result.M31 = matrix.M31 - scalar;
			result.M32 = matrix.M32 - scalar;
			result.M33 = matrix.M33 - scalar;
			result.M34 = matrix.M34 - scalar;

			result.M41 = matrix.M41 - scalar;
			result.M42 = matrix.M42 - scalar;
			result.M43 = matrix.M43 - scalar;
			result.M44 = matrix.M44 - scalar;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 Multiply(DMatrix4 left, DMatrix4 right) { unchecked {
            return new DMatrix4(
				left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41,
				left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42,
				left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43,
				left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44,

				left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41,
				left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42,
				left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43,
				left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44,

				left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41,
				left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42,
				left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43,
				left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44,

				left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41,
				left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42,
				left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43,
				left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(DMatrix4 left, DMatrix4 right, ref DMatrix4 result) { unchecked {
			result.M11 = left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41;
			result.M12 = left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42;
			result.M13 = left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43;
			result.M14 = left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44;

			result.M21 = left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41;
			result.M22 = left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42;
			result.M23 = left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43;
			result.M24 = left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44;

			result.M31 = left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41;
			result.M32 = left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42;
			result.M33 = left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43;
			result.M34 = left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44;

			result.M41 = left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41;
			result.M42 = left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42;
			result.M43 = left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43;
			result.M44 = left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DVector4 Transform(DMatrix4 matrix, DVector4 vector) { unchecked {
			return new DVector4(
				(matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z) + (matrix.M14 * vector.W),
				(matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z) + (matrix.M24 * vector.W),
				(matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z) + (matrix.M34 * vector.W),
				(matrix.M41 * vector.X) + (matrix.M42 * vector.Y) + (matrix.M43 * vector.Z) + (matrix.M44 * vector.W));
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(DMatrix4 matrix, DVector4 vector, ref DVector4 result) { unchecked {
			result.X = (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z) + (matrix.M14 * vector.W);
			result.Y = (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z) + (matrix.M24 * vector.W);
			result.Z = (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z) + (matrix.M34 * vector.W);
			result.W = (matrix.M41 * vector.X) + (matrix.M42 * vector.Y) + (matrix.M43 * vector.Z) + (matrix.M44 * vector.W);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform3dPoint(DMatrix4 matrix, DVector4 vector, ref DVector4 result) { unchecked {
            Debug.Assert(vector.W == 1d, "vector.W != 1");
			result.X = (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z) + matrix.M14;
			result.Y = (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z) + matrix.M24;
			result.Z = (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z) + matrix.M34;
			result.W = (matrix.M41 * vector.X) + (matrix.M42 * vector.Y) + (matrix.M43 * vector.Z) + matrix.M44;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform3dVect(DMatrix4 matrix, DVector4 vector, ref DVector4 result) { unchecked {
            Debug.Assert(vector.W == 0d, "vector.W != 0");
			result.X = (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z);
			result.Y = (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z);
			result.Z = (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z);
			result.W = (matrix.M41 * vector.X) + (matrix.M42 * vector.Y) + (matrix.M43 * vector.Z);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 Invert(DMatrix4 matrix) { unchecked {
            var a3434 = matrix.M33 * matrix.M44 - matrix.M34 * matrix.M43 ;
            var a2434 = matrix.M32 * matrix.M44 - matrix.M34 * matrix.M42 ;
            var a2334 = matrix.M32 * matrix.M43 - matrix.M33 * matrix.M42 ;
            var a1434 = matrix.M31 * matrix.M44 - matrix.M34 * matrix.M41 ;
            var a1334 = matrix.M31 * matrix.M43 - matrix.M33 * matrix.M41 ;
            var a1234 = matrix.M31 * matrix.M42 - matrix.M32 * matrix.M41 ;
            var a3424 = matrix.M23 * matrix.M44 - matrix.M24 * matrix.M43 ;
            var a2424 = matrix.M22 * matrix.M44 - matrix.M24 * matrix.M42 ;
            var a2324 = matrix.M22 * matrix.M43 - matrix.M23 * matrix.M42 ;
            var a3423 = matrix.M23 * matrix.M34 - matrix.M24 * matrix.M33 ;
            var a2423 = matrix.M22 * matrix.M34 - matrix.M24 * matrix.M32 ;
            var a2323 = matrix.M22 * matrix.M33 - matrix.M23 * matrix.M32 ;
            var a1424 = matrix.M21 * matrix.M44 - matrix.M24 * matrix.M41 ;
            var a1324 = matrix.M21 * matrix.M43 - matrix.M23 * matrix.M41 ;
            var a1423 = matrix.M21 * matrix.M34 - matrix.M24 * matrix.M31 ;
            var a1323 = matrix.M21 * matrix.M33 - matrix.M23 * matrix.M31 ;
            var a1224 = matrix.M21 * matrix.M42 - matrix.M22 * matrix.M41 ;
            var a1223 = matrix.M21 * matrix.M32 - matrix.M22 * matrix.M31 ;
            var det = matrix.M11 * ( matrix.M22 * a3434 - matrix.M23 * a2434 + matrix.M24 * a2334 ) 
                    - matrix.M12 * ( matrix.M21 * a3434 - matrix.M23 * a1434 + matrix.M24 * a1334 ) 
                    + matrix.M13 * ( matrix.M21 * a2434 - matrix.M22 * a1434 + matrix.M24 * a1234 ) 
                    - matrix.M14 * ( matrix.M21 * a2334 - matrix.M22 * a1334 + matrix.M23 * a1234 ) ;
            det = 1 / det;
            return new DMatrix4() {
               M11 = det *   ( matrix.M22 * a3434 - matrix.M23 * a2434 + matrix.M24 * a2334 ),
               M12 = det * - ( matrix.M12 * a3434 - matrix.M13 * a2434 + matrix.M14 * a2334 ),
               M13 = det *   ( matrix.M12 * a3424 - matrix.M13 * a2424 + matrix.M14 * a2324 ),
               M14 = det * - ( matrix.M12 * a3423 - matrix.M13 * a2423 + matrix.M14 * a2323 ),
               M21 = det * - ( matrix.M21 * a3434 - matrix.M23 * a1434 + matrix.M24 * a1334 ),
               M22 = det *   ( matrix.M11 * a3434 - matrix.M13 * a1434 + matrix.M14 * a1334 ),
               M23 = det * - ( matrix.M11 * a3424 - matrix.M13 * a1424 + matrix.M14 * a1324 ),
               M24 = det *   ( matrix.M11 * a3423 - matrix.M13 * a1423 + matrix.M14 * a1323 ),
               M31 = det *   ( matrix.M21 * a2434 - matrix.M22 * a1434 + matrix.M24 * a1234 ),
               M32 = det * - ( matrix.M11 * a2434 - matrix.M12 * a1434 + matrix.M14 * a1234 ),
               M33 = det *   ( matrix.M11 * a2424 - matrix.M12 * a1424 + matrix.M14 * a1224 ),
               M34 = det * - ( matrix.M11 * a2423 - matrix.M12 * a1423 + matrix.M14 * a1223 ),
               M41 = det * - ( matrix.M21 * a2334 - matrix.M22 * a1334 + matrix.M23 * a1234 ),
               M42 = det *   ( matrix.M11 * a2334 - matrix.M12 * a1334 + matrix.M13 * a1234 ),
               M43 = det * - ( matrix.M11 * a2324 - matrix.M12 * a1324 + matrix.M13 * a1224 ),
               M44 = det *   ( matrix.M11 * a2323 - matrix.M12 * a1323 + matrix.M13 * a1223 ),
            };
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 Transpose(DMatrix4 m) { unchecked {
            DMatrix4 t = new DMatrix4(m);
			t.Transpose();
			return t;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ApproxEqual(DMatrix4 left, DMatrix4 right, double tolerance) { unchecked {
			return ((System.Math.Abs(left.M11 - right.M11) <= tolerance) &&
                    (System.Math.Abs(left.M12 - right.M12) <= tolerance) &&
				    (System.Math.Abs(left.M13 - right.M13) <= tolerance) &&
                    (System.Math.Abs(left.M14 - right.M14) <= tolerance) &&

                    (System.Math.Abs(left.M21 - right.M21) <= tolerance) &&
                    (System.Math.Abs(left.M22 - right.M22) <= tolerance) &&
				    (System.Math.Abs(left.M23 - right.M23) <= tolerance) &&
                    (System.Math.Abs(left.M24 - right.M24) <= tolerance) &&

                    (System.Math.Abs(left.M31 - right.M31) <= tolerance) &&
                    (System.Math.Abs(left.M32 - right.M32) <= tolerance) &&
				    (System.Math.Abs(left.M33 - right.M33) <= tolerance) &&
                    (System.Math.Abs(left.M34 - right.M34) <= tolerance) &&

                    (System.Math.Abs(left.M41 - right.M41) <= tolerance) &&
                    (System.Math.Abs(left.M42 - right.M42) <= tolerance) &&
				    (System.Math.Abs(left.M43 - right.M43) <= tolerance) &&
                    (System.Math.Abs(left.M44 - right.M44) <= tolerance) );
		}}
        #endregion

        #region Свойства и Методы

        public double Trace {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unchecked { return M11 + M22 + M33 + M44; } }
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double GetDeterminant() { unchecked {
        //	double det = 0.0f;
        //	for (int col = 0; col < 4; col++) 
        //		if ((col % 2) == 0) det += this[0, col] * Minor(0, col).Determinant();
        //		else                det -= this[0, col] * Minor(0, col).Determinant();
        //	return det;
			return
				M14 * M23 * M32 * M41 - M13 * M24 * M32 * M41 - M14 * M22 * M33 * M41 + M12 * M24 * M33 * M41 +
				M13 * M22 * M34 * M41 - M12 * M23 * M34 * M41 - M14 * M23 * M31 * M42 + M13 * M24 * M31 * M42 +
				M14 * M21 * M33 * M42 - M11 * M24 * M33 * M42 - M13 * M21 * M34 * M42 + M11 * M23 * M34 * M42 +
				M14 * M22 * M31 * M43 - M12 * M24 * M31 * M43 - M14 * M21 * M32 * M43 + M11 * M24 * M32 * M43 +
				M12 * M21 * M34 * M43 - M11 * M22 * M34 * M43 - M13 * M22 * M31 * M44 + M12 * M23 * M31 * M44 +
				M13 * M21 * M32 * M44 - M11 * M23 * M32 * M44 - M12 * M21 * M33 * M44 + M11 * M22 * M33 * M44;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DMatrix4 Invert() { unchecked {
            return Invert(this);
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Transpose() { unchecked {
		    double temp;
		    temp = M12;  M12 = M21;  M21 = temp;
            temp = M13;  M13 = M31;  M31 = temp;
            temp = M14;  M14 = M41;  M41 = temp;
            temp = M23;  M23 = M32;  M32 = temp;
            temp = M24;  M24 = M42;  M42 = temp;
            temp = M34;  M34 = M43;  M43 = temp;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double[] ToArray(bool glorder = false) { unchecked {
            return glorder 
            ? new double[16] { M11, M21, M31, M41, M12, M22, M32, M42, M13, M23, M33, M43, M14, M24, M34, M44 } 
            : new double[16] { M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44 };
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float[] ToFloatArray(bool glorder = false) { unchecked {
            return ToArray(glorder).Select(v => (float)v).ToArray();
	    }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal[] ToDecimalArray(bool glorder = false) { unchecked {
            return ToArray(glorder).Select(v => (decimal)v).ToArray();
	    }}
        #endregion

        #region Реализация интерфейса ICloneable

        object ICloneable.Clone() {
            return new DMatrix4(this);
        }

        public DMatrix4 Clone() {
            return new DMatrix4(this);
        }

        #endregion

        #region Реализация интерфейса IEquatable<Vector4>

        bool IEquatable<DMatrix4>.Equals(DMatrix4 m) {
            return  (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) && (M14 == m.M14) &&
				    (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) && (M24 == m.M24) &&
				    (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33) && (M34 == m.M34) &&
				    (M41 == m.M41) && (M42 == m.M42) && (M43 == m.M43) && (M44 == m.M44);
        }

        public bool Equals(DMatrix4 m) {
            return  (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) && (M14 == m.M14) &&
				    (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) && (M24 == m.M24) &&
				    (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33) && (M34 == m.M34) &&
				    (M41 == m.M41) && (M42 == m.M42) && (M43 == m.M43) && (M44 == m.M44);
        }
        #endregion

        #region Реализация интерфейса IFormattable

        public string ToString(string format) {
            return string.Format("4x4[{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}]",
                M11.ToString(format, null), M12.ToString(format, null), M13.ToString(format, null), M14.ToString(format, null),
                M21.ToString(format, null), M22.ToString(format, null), M23.ToString(format, null), M24.ToString(format, null),
                M31.ToString(format, null), M32.ToString(format, null), M33.ToString(format, null), M34.ToString(format, null),
                M41.ToString(format, null), M42.ToString(format, null), M43.ToString(format, null), M44.ToString(format, null));
        }

        string IFormattable.ToString(string format, IFormatProvider provider) {
            return ToString(format, provider);
        }

        public string ToString(string format, IFormatProvider provider) {
            return string.Format("4x4[{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}]",
                M11.ToString(format, provider), M12.ToString(format, provider), M13.ToString(format, provider), M14.ToString(format, provider),
                M21.ToString(format, provider), M22.ToString(format, provider), M23.ToString(format, provider), M24.ToString(format, provider),
                M31.ToString(format, provider), M32.ToString(format, provider), M33.ToString(format, provider), M34.ToString(format, provider),
                M41.ToString(format, provider), M42.ToString(format, provider), M43.ToString(format, provider), M44.ToString(format, provider));
        }
        #endregion

        #region Перегрузки

        public override int GetHashCode() {
			return  M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^ M14.GetHashCode() ^
				    M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^ M24.GetHashCode() ^
				    M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode() ^ M34.GetHashCode() ^
				    M41.GetHashCode() ^ M42.GetHashCode() ^ M43.GetHashCode() ^ M44.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is DMatrix4) {
                DMatrix4 m = (DMatrix4)obj;
				return  (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) && (M14 == m.M14) &&
					    (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) && (M24 == m.M24) &&
					    (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33) && (M34 == m.M34) &&
					    (M41 == m.M41) && (M42 == m.M42) && (M43 == m.M43) && (M44 == m.M44);
			}
			return false;
		}

		public override string ToString() {
			return string.Format("4x4[{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}]",
				M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44);
		}
		#endregion

        #region Операторы

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DMatrix4 left, DMatrix4 right) { unchecked {
			return ValueType.Equals(left, right);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(DMatrix4 left, DMatrix4 right) { unchecked {
			return !ValueType.Equals(left, right);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 operator +(DMatrix4 left, DMatrix4 right) { unchecked {
            return DMatrix4.Add(left, right); ;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 operator +(DMatrix4 matrix, double scalar) { unchecked {
            return DMatrix4.Add(matrix, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 operator +(double scalar, DMatrix4 matrix) { unchecked {
            return DMatrix4.Add(matrix, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 operator -(DMatrix4 left, DMatrix4 right) { unchecked {
            return DMatrix4.Subtract(left, right); ;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 operator -(DMatrix4 matrix, double scalar) { unchecked {
            return DMatrix4.Subtract(matrix, scalar);
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DMatrix4 operator *(DMatrix4 left, DMatrix4 right) { unchecked {
            return DMatrix4.Multiply(left, right); ;
		}}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DVector4 operator *(DMatrix4 matrix, DVector4 vector) { unchecked {
            return DMatrix4.Transform(matrix, vector);
		}}

		public unsafe double this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { unchecked {
				if (index < 0 || index >= 16)
                    throw new IndexOutOfRangeException("Недопустимый индекс элемента матрицы!");
				fixed (double* f = &M11)
					return *(f + index);
			}}
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
			set { unchecked {
				if (index < 0 || index >= 16)
                    throw new IndexOutOfRangeException("Недопустимый индекс элемента матрицы!");
				fixed (double* f = &M11)
					*(f + index) = value;
			}}
		}

		public double this[int row, int column] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unchecked { return this[(row - 1) * 4 + (column - 1)]; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { unchecked { this[(row - 1) * 4 + (column - 1)] = value; } }
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator DMatrix3(DMatrix4 matrix) { unchecked {
            return new DMatrix3(matrix.M11, matrix.M12, matrix.M13,
                                matrix.M21, matrix.M22, matrix.M23,
                                matrix.M31, matrix.M32, matrix.M33);
		}}
        #endregion
    }
}
