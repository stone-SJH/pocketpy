using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Python
{
    public class PyBytesType : PyTypeObject
    {
        public override string Name
        {
            get => "bytes";
            set => throw new NotImplementedException("bytes type cannot be renamed.");
        }
        public override Type CSType => typeof(byte[]);

        [PythonBinding]
        public object __new__(PyTypeObject type, object value)
        {
            List<object> list = vm.PyList(value);
            byte[] buffer = new byte[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                int b = (int)list[i];
                if (b < 0 || b > 255)
                    vm.ValueError("byte must be in range[0, 256)");
                buffer[i] = (byte)b;
            }
            return buffer;
        }

        [PythonBinding]
        public object __repr__(byte[] a)
        {
            StringBuilder hex = new StringBuilder();
            hex.Append("b'");
            foreach (byte b in a)
            {
                if (b >= 0x7f || b <= 0x1f)
                {
                    int high = b >> 4;
                    int low = b & 0xf;
                    hex.Append("\\x");
                    hex.Append("0123456789abcdef"[high]);
                    hex.Append("0123456789abcdef"[low]);
                }
                else
                    hex.Append((char)b);
            }
            hex.Append("'");
            return hex.ToString();
        }

        [PythonBinding]
        public object __len__(byte[] a)
        {
            return a.Length;
        }

        [PythonBinding]
        public object __eq__(byte[] a, object b)
        {
            if (b is byte[] ba)
                return a.SequenceEqual(ba);

            return VM.NotImplemented;
        }

        [PythonBinding]
        public object __getitem__(byte[] a, object b)
        {
            int index = vm.NormalizedIndex(vm.PyCast<int>(b), a.Length);
            return a[index].ToString();
        }

        [PythonBinding]
        public int __hash__(byte[] value)
        {
            long x = 1000003;
            foreach (var item in value)
            {
                int y = vm.PyHash(item);
                x = x ^ (y + 0x9e3779b9 + (x << 6) + (x >> 2));
            }
            return (int)x;
        }

        [PythonBinding]
        public string decode(byte[] a)
        {
            return Encoding.UTF8.GetString(a);
        }

        [PythonBinding]
        public object __str__(byte[] a)
        {
            return __repr__(a);
        }
    }
}
