// using UnityEngine;

using System;
using UnityEngine;

namespace Python
{
    public class PyIntType : PyTypeObject
    {
        public override string Name
        {
            get => "int";

            set => throw new NotImplementedException("int type cannot be renamed.");
        }
        public override System.Type CSType { get { return typeof(int); } }

        [PythonBinding]
        public object __new__(PyTypeObject type, object value)
        {
            if (value is int) return (int)value;
            if (value is ushort) return (int)(ushort) value;
            if (value is float) return (int)(float)value;
            if (value is string) return int.Parse((string)value);
            vm.TypeError("expected int, float or string, got " + type.Name);
            return 0;
        }

        [PythonBinding]
        public object __add__(int a, object b)
        {
            if (b is int) return a + (int)b;
            if (b is ushort) return a + (int)(ushort)b;
            if (b is float) return a + (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __radd__(int a, object b)
        {
            return __add__(a, b);
        }

        [PythonBinding]
        public object __sub__(int a, object b)
        {
            if (b is int) return a - (int)b;
            if (b is ushort) return a - (ushort) b;
            if (b is float) return a - (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __rsub__(int a, object b)
        {
            return __sub__(a, b);
        }

        [PythonBinding]
        public object __mul__(int a, object b)
        {
            if (b is int) return a * (int)b;
            if (b is ushort) return a * (ushort) b;
            if (b is float) return a * (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __rmul__(int a, object b)
        {
            return __mul__(a, b);
        }

        [PythonBinding]
        public object __truediv__(int a, object b)
        {
            if (b is int @int)
            {
                return a / (float)@int;
            }
            if (b is ushort @ushort)
            {
                return a / (float) @ushort;
            }
            if (b is float @float)
            {
                return a / @float;
            }
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __floordiv__(int a, object b)
        {
            if (b is int @int)
            {
                if (@int == 0) vm.ZeroDivisionError();
                return a / @int;
            }
            if (b is ushort @ushort)
            {
                if (@ushort == 0) vm.ZeroDivisionError();
                return a / @ushort;
            }
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __mod__(int a, object b)
        {
            if (b is int @int)
            {
                if (@int == 0) vm.ZeroDivisionError();
                return a % @int;
            }
            if (b is ushort @ushort)
            {
                if (@ushort == 0) vm.ZeroDivisionError();
                return a % @ushort;
            }
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __pow__(int a, object b)
        {
            if (b is int @int)
            {
                if (@int < 0 && a == 0) vm.ZeroDivisionError("0.0 cannot be raised to a negative power");
                int result = 1;
                for (int i = 0; i < @int; i++) result *= a;
                return result;
            }

            if (b is ushort @ushort)
            {
                int result = 1;
                for (int i = 0; i < @ushort; i++) result *= a;
                return result;
            }
            if (b is float) return Mathf.Pow(a, (float)b);
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __eq__(int a, object b)
        {
            if (b is int) return a == (int)b;
            if (b is ushort) return a == (ushort) b;
            if (b is float) return a == (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __lt__(int a, object b)
        {
            if (b is int) return a < (int)b;
            if (b is ushort) return a < (ushort) b;
            if (b is float) return a < (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __gt__(int a, object b)
        {
            if (b is int) return a > (int)b;
            if (b is ushort) return a > (ushort) b;
            if (b is float) return a > (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __le__(int a, object b)
        {
            if (b is int) return a <= (int)b;
            if (b is ushort) return a <= (ushort) b;
            if (b is float) return a <= (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __ge__(int a, object b)
        {
            if (b is int) return a >= (int)b;
            if (b is ushort) return a >= (ushort) b;
            if (b is float) return a >= (float)b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __neg__(int a)
        {
            return -a;
        }

        [PythonBinding]
        public object __repr__(int a)
        {
            return a.ToString();
        }

        [PythonBinding]
        public object __lshift__(int a, object b)
        {
            if (b is int) return a << (int)b;
            if (b is ushort) return a << (ushort) b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __rshift__(int a, object b)
        {
            if (b is int) return a >> (int)b;
            if (b is ushort) return a >> (ushort) b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __and__(int a, object b)
        {
            if (b is int) return a & (int)b;
            if (b is ushort) return a & (ushort) b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __or__(int a, object b)
        {
            if (b is int) return a | (int)b;
            if (b is ushort) return a | (ushort) b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __xor__(int a, object b)
        {
            if (b is int) return a ^ (int)b;
            if (b is ushort) return a ^ (ushort) b;
            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __invert__(int a)
        {
            return ~a;
        }
    }
}
