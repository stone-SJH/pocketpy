using System;
using System.Runtime.InteropServices;

namespace Python
{
    public static class Bindings
    {
#if !UNITY_EDITOR && UNITY_IOS
        private const string _libName = "__Internal";
#else
        private const string _libName = "py";
#endif
        [DllImport(_libName)]
        public static extern void pkpy_compile_to_string(IntPtr vm, string source, string filename, int mode, bool unknownGlobalScope, out bool ok, out string res);
        [DllImport(_libName)]
        public static extern IntPtr pkpy_new_vm(bool enable_os);
    }
}
