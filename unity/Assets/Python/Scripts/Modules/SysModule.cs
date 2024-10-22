// unset

namespace Python.Modules
{
    public class SysModule
    {
        private PyObject stdout = new PyObject();
        private PyObject stderr = new PyObject();

        public void RegisterSysModule(VM vm)
        {
            PyModule sys = vm.NewModule("sys");

            sys["version"] = Python.Version.Frontend;
            sys["platform"] = GetPlatform();
            sys["stdout"] = stdout;
            sys["stderr"] = stderr;

            vm.BindFunc(stdout, "write", (vm1, args) =>
            {
                if (stdout == null)
                    throw new CsharpException("Please register builtin stdout method before calling print().");
                string data = vm.PyStr(args[0]);
                vm.stdout(data);
                return VM.None;
            });

            vm.BindFunc(stderr, "write", (vm1, args) =>
            {
                string data = args[0] as string;
                vm.stderr(data);
                vm.stderr(data.Length.ToString());
                return VM.None;
            });
        }

        private string GetPlatform()
        {
            #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return "win32";
            #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return "darwin";
            #elif UNITY_EDITOR_LINUX || UNITY_EMBEDDED_LINUX || UNITY_STANDALONE_LINUX
            return "linux2";
            #elif UNITY_ANDROID
            return "android";
            #elif UNITY_IOS
            return "ios";
            #endif
            return "unknown";
        }
    }
}
