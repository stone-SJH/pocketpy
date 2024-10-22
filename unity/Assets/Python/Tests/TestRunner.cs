#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Python
{
    public class PyExecutionAnalytics
    {
        private StringBuilder binaryop;
        private StringBuilder csfunc;
        private StringBuilder pyfunc;
        private string filepath;

        public PyExecutionAnalytics(string filepath)
        {
            binaryop = new StringBuilder();
            csfunc = new StringBuilder();
            pyfunc = new StringBuilder();
            this.filepath = filepath;
        }

        public void BinaryOpCollector(string op, object lvalue, object rvalue)
        {
            binaryop.Append($"Op: {op}, lvalue: {lvalue}, rvalue: {rvalue}\n");
        }

        public void CsharpFuncCollector(string funcname, object[] args)
        {
            csfunc.Append($"C# function name: {funcname}, args:[");
            if (args.Length != 0)
            {
                csfunc.Append(args[0]);
                for (int i = 1; i < args.Length; i++)
                {
                    csfunc.Append(", ");
                    csfunc.Append(args[i]);
                }
            }
            csfunc.Append("]\n");
        }

        public void PyFuncCollector(string funcname, Dictionary<string, object> kwargs)
        {
            pyfunc.Append($"Python function name: {funcname}, args:[");
            if (kwargs.Count != 0)
            {
                foreach (var key in kwargs.Keys)
                {
                    pyfunc.Append("\"" + key + "\" : \"" + kwargs[key] + "\", ");
                }
                pyfunc.Length -= 2;
            }
            pyfunc.Append("]\n");
        }

        public void Generate()
        {
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }

            File.WriteAllText(filepath, "\n-------------BinaryOp------------\n" +
                binaryop.ToString() + "\n------------CsFunc-------------\n" +
                csfunc.ToString() + "\n-------------PyFunc------------\n" + pyfunc.ToString());
        }
    }

    public class TestRunner
    {
        private static readonly string testPath = Application.dataPath + "/Python/Tests/py/";

        private static readonly List<string> testcases = new List<string>()
        {
            "00.py",
            // "01_int.py",
            // "02_float.py",
            // "03_bool.py",
            // "04_line_continue.py",
            // "05_str.py",
            // "06_builtin.py",
            // "07_if_for.py",
            // "08_list.py",
            // "09_tuple.py",
            // "10_dict.py",
            // "11_set.py",
            // "12_is.py",
            // "13_function.py",
            // "14_cmp.py",
            // "15_typehints.py",
            // "16_inlineblocks.py",
            // "17_multiline.py",
            // "18_goto.py",
            // "19_iter.py",
            // "20_import.py",
            // "21_class.py"
        };

        [MenuItem("Python/Run Tests")]

        public static void RunTests()
        {
            PyExecutionAnalytics analytics = new PyExecutionAnalytics(Application.dataPath + "/../py_analytics_single.txt");
            try
            {
                VM vm = new VM();

                vm.stdin = prompt =>
                {
                    return "test input()";
                };

                vm.BinaryOpListener += analytics.BinaryOpCollector;
                vm.CsharpFunctionListener += analytics.CsharpFuncCollector;
                vm.PyFunctionListener += analytics.PyFuncCollector;

                foreach (var testcase in testcases)
                {
                    var pyscript = Utils.LoadTestCase(testPath + testcase);
                    var co = vm.Compile(pyscript, "testcase", CompileMode.EXEC_MODE);

                    Thread th = new Thread((o =>
                    {
                        VM _vm = (VM) o;
                        while (_vm.isRunning)
                            Thread.Sleep(100);
                        _vm.Exec(co);
                    }));
                    th.Start(vm);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                Debug.LogError(e.StackTrace);
            }
            finally
            {
                //Be aware it does not guarantee that all the op/func of testcases are collected at thi time
                analytics.Generate();
            }
        }

        [MenuItem("Python/Run All Tests")]
        public static void RunAllTests()
        {
            PyExecutionAnalytics analytics = new PyExecutionAnalytics(Application.dataPath + "/../py_analytics.txt");
            try
            {
                VM vm = new VM();
                PyModule module = vm.NewModule("test");

                vm.stdin = prompt =>
                {
                    return "test input()";
                };

                vm.BinaryOpListener += analytics.BinaryOpCollector;
                vm.CsharpFunctionListener += analytics.CsharpFuncCollector;
                vm.PyFunctionListener += analytics.PyFuncCollector;

                var ext = new List<string> {"py", "txt"};
                var testcases = Directory
                    .EnumerateFiles(testPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));

                foreach (var testcase in testcases)
                {
                    var pyscript = Utils.LoadTestCase(testcase);
                    var co = vm.Compile(pyscript, "testcase", CompileMode.EXEC_MODE);

                    Thread th = new Thread((o =>
                    {
                        VM _vm = (VM)o;
                        while (_vm.isRunning)
                            Thread.Sleep(5);
                        _vm.Exec(co, module);
                    }));
                    th.Start(vm);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                Debug.LogError(e.StackTrace);
            }
            finally
            {
                //Be aware it does not guarantee that all the op/func of testcases are collected at thi time
                analytics.Generate();
            }
        }
    }
}
#endif
