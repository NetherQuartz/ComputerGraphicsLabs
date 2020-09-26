using CGLabPlatform;
using System;
using System.Globalization;

namespace CGLabExtensions
{
    public static class DVector2Extensions
    {
        // косое произведение с одним аргументом
        public static double CrossProduct(this DVector2 this_vec, DVector2 vector)
        {
            return this_vec.X * vector.Y - vector.X * this_vec.Y;
        }

        // косое произведение с двумя аргументами
        public static double CrossProduct(this DVector2 _, DVector2 left, DVector2 right)
        {
            return left.X * right.Y - right.X * left.Y;
        }

        // деконструкция в отдельные переменные: var (x, y) = vector
        public static void Deconstruct(this DVector2 vector, out double x, out double y)
        {
            x = vector.X;
            y = vector.Y;
        }

        // преобразование любого кортежа из двух элементов в DVector2: (x, y).ToDvector2()
        public static DVector2 ToDVector2<TX, TY>(this (TX, TY) tuple)
            where TX : struct, IConvertible
            where TY : struct, IConvertible
        {
            return new DVector2(
                tuple.Item1.ToDouble(CultureInfo.CurrentCulture),
                tuple.Item2.ToDouble(CultureInfo.CurrentCulture));
        }
    }
    
    public static class DVector3Extensions
    {
        // деконструкция в отдельные переменные: var (x, y, z) = vector
        public static void Deconstruct(this DVector3 vector, out double x, out double y, out double z)
        {
            x = vector.X;
            y = vector.Y;
            z = vector.Z;
        }

        // преобразование любого кортежа из трёх элементов в DVector3: (x, y, z).ToDvector3()
        public static DVector3 ToDVector3<TX, TY, TZ>(this (TX, TY, TZ) tuple)
            where TX : struct, IConvertible
            where TY : struct, IConvertible
            where TZ : struct, IConvertible
        {
            return new DVector3(
                tuple.Item1.ToDouble(CultureInfo.CurrentCulture),
                tuple.Item2.ToDouble(CultureInfo.CurrentCulture),
                tuple.Item3.ToDouble(CultureInfo.CurrentCulture));
        }
    }
    
    public static class DVector4Extensions
    {
        // деконструкция в отдельные переменные: var (x, y, z, w) = vector
        public static void Deconstruct(this DVector4 vector, out double x, out double y, out double z, out double w)
        {
            x = vector.X;
            y = vector.Y;
            z = vector.Z;
            w = vector.W;
        }

        // преобразование любого кортежа из четырёх элементов в DVector3: (x, y, z, w).ToDvector4()
        public static DVector4 ToDVector4<TX, TY, TZ, TW>(this (TX, TY, TZ, TW) tuple)
            where TX : struct, IConvertible
            where TY : struct, IConvertible
            where TZ : struct, IConvertible
            where TW : struct, IConvertible 
        {
            return new DVector4(
                tuple.Item1.ToDouble(CultureInfo.CurrentCulture),
                tuple.Item2.ToDouble(CultureInfo.CurrentCulture),
                tuple.Item3.ToDouble(CultureInfo.CurrentCulture),
                tuple.Item4.ToDouble(CultureInfo.CurrentCulture));
        }

        // преобразование в DVector3 с делением на W, если это точка (т.е. W != 0)
        // или с простым отбрасыванием W, если это вектор (т.е. W == 0)
        public static DVector3 ToDVector3(this DVector4 vector)
        {
            if (vector.W == 0)
            {
                return new DVector3(vector.X, vector.Y, vector.Z);
            }
            return new DVector3(vector.X, vector.Y, vector.Z) / vector.W;
        }
    }
}