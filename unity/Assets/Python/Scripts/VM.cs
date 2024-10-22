using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Python.Modules;
using UnityEngine.Scripting;

[assembly: Preserve]
namespace Python
{
    public partial class VM
    {
        public static PyNone None = PyNoneType.None;
        public static EllipsisType Ellipsis = new EllipsisType();
        public static NotImplementedType NotImplemented = new NotImplementedType();
        public static StopIterationType StopIteration = new StopIterationType();
        public static YieldType YieldOp = new YieldType();

        private static IntPtr p;
        private object @lock = new object();

        public Stack<Frame> callStack = new Stack<Frame>();
        public PyModule builtins;
        public PyModule main;
        public bool debug = false;
        public bool throwException = true;
        public volatile bool isRunning = false;
        public volatile bool keyboardInterruptFlag = false;
        public int maxRecursionDepth = 100;

        public bool needRaise = false;
        public PyObject lastException = null;
        public PyObject curClass = null;

        public System.Action<string> stdout = Debug.Log;
        public System.Action<string> stderr = Debug.LogError;
        public System.Func<string, string> stdin;

        //opcode, lvalue, rvalue
        //opcode list: "/", "**", "+", "-", "*", "//", "%", "<", "<=", "==", "!=", ">", ">=", "<<", ">>", "&", "|", "^", "@"
        public System.Action<string, object, object> BinaryOpListener;

        //funcname, args
        public System.Action<string, object[]> CsharpFunctionListener;

        //funcname, args("argname": "argvalue")
        public System.Action<string, Dictionary<string, object>> PyFunctionListener;

        //modulename
        public System.Action<string> PyImportModuleListener;

        public Dictionary<string, PyModule> modules = new Dictionary<string, PyModule>();
        public Dictionary<string, string> lazyModules = new Dictionary<string, string>();

        public Dictionary<Type, PyTypeObject> allTypes = new Dictionary<Type, PyTypeObject>();

        public List<object> listReprRecursiveList = new List<object>();
        public VM()
        {
            if (p == IntPtr.Zero) p = Bindings.pkpy_new_vm(false);

            builtins = NewModule("builtins");
            main = NewModule("__main__");

            RegisterType(new PyObjectType());       // object
            RegisterType(new PyTypeType());         // System.Type
            RegisterType(new PyIntType());          // int
            RegisterType(new PyFloatType());        // float
            RegisterType(new PyBoolType());         // bool
            RegisterType(new PyStrType());          // string
            RegisterType(new PyBytesType());        // byte[]
            RegisterType(new PyListType());         // List<object>
            RegisterType(new PyTupleType());        // object[]
            RegisterType(new PyNoneType());

            RegisterType(new PySliceType());
            RegisterType(new PyRangeType());
            RegisterType(new PyModuleType());
            RegisterType(new PySuperType());
            RegisterType(new PyDictType());
            RegisterType(new PyPropertyType());
            RegisterType(new PyStarWrapperType());
            RegisterType(new PyIteratorType());

            RegisterType(new CSharpMethodType());
            RegisterType(new CSharpLazyMethodType());
            RegisterType(new CSharpLambdaType());
            RegisterType(new PyFunctionType());
            RegisterType(new PyBoundMethodType());
            RegisterType(new PyExceptionType());
            RegisterType(new PyGeneratorType());
            RegisterType(new PyMappingProxyType());

            builtins["type"] = typeof(Type).GetPyType(this);
            builtins["object"] = typeof(object).GetPyType(this);
            builtins["bool"] = typeof(bool).GetPyType(this);
            builtins["int"] = typeof(int).GetPyType(this);
            builtins["float"] = typeof(float).GetPyType(this);
            builtins["str"] = typeof(string).GetPyType(this);
            builtins["bytes"] = typeof(byte[]).GetPyType(this);
            builtins["list"] = typeof(List<object>).GetPyType(this);
            builtins["tuple"] = typeof(object[]).GetPyType(this);
            builtins["range"] = typeof(PyRange).GetPyType(this);
            builtins["dict"] = typeof(PyDict).GetPyType(this);
            builtins["property"] = typeof(PyProperty).GetPyType(this);
            builtins["StopIteration"] = StopIteration;
            builtins["NotImplemented"] = NotImplemented;
            builtins["None"] = None;
            builtins["slice"] = typeof(PySlice).GetPyType(this);
            builtins["Exception"] = typeof(PyException).GetPyType(this);

            /*******************************************************/
            BindBuiltinFunc("repr", (VM vm, object[] args) => PyRepr(args[0]));
            BindBuiltinFunc("len", (VM vm, object[] args) => CallMethod(args[0], "__len__"));
            BindBuiltinFunc("iter", (VM vm, object[] args) => vm.PyIter(args[0]));
            BindBuiltinFunc("next", (VM vm, object[] args) => vm.PyNext(args[0]));
            BindBuiltinFunc("super", (vm, args) =>
            {
                object self = null;
                PyTypeObject cls = null;
                if (args.Length == 0)
                {
                    Frame f = callStack.Peek();

                    if (f.callable != null)
                    {
                        cls = ((f.callable as PyFunction).@class as PyTypeObject).GetBaseType() as PyTypeObject;
                        f.locals.TryGetValue("self", out self);
                        return new PySuper(self, cls);
                    }
                    if (self == null || cls == null)
                    {
                        TypeError("super(): unable to determine the class context, use super(class, self) instead");
                    }
                }
                if (args.Length == 2)
                {
                    Utils.Assert(args[0] is PyTypeObject, "super(): first arg must be type");
                    // TODO: assert isinstance(args[1], args[0])
                    object @base = (args[0] as PyTypeObject).GetBaseType();
                    Utils.Assert(@base != None, "super(): object does not have a base");
                    return new PySuper(args[1], @base as PyTypeObject);
                }
                TypeError("super() takes 0 or 2 arguments");
                return VM.None;
            });
            BindBuiltinFunc("isinstance", (VM vm, object[] args) => IsInstance(args[0], args[1]));

            BindBuiltinFunc("getattr", (VM vm, object[] args) => GetAttr(args[0], PyCast<string>(args[1])));
            BindBuiltinFunc("setattr", (VM vm, object[] args) => SetAttr(args[0], PyCast<string>(args[1]), args[2]));
            BindBuiltinFunc("hasattr", (VM vm, object[] args) => HasAttr(args[0], PyCast<string>(args[1])));

            BindBuiltinFunc("dir",  (VM vm, object[] args) =>
            {
                List<object> result = new List<object>();
                if (args[0] is PyObject obj)
                {
                    foreach (var kv in obj.attr)
                    {
                        result.Add(kv.Key);
                    }
                }
                return result;
            });

            BindBuiltinFunc("globals", (vm, args) =>
            {
                Dictionary<string, object> globals = vm.callStack.Peek().module.attr;
                //convert to pydict
                PyDict dic = new PyDict(globals, vm);
                return dic;
            });

            BindBuiltinFunc("locals", (vm, args) =>
            {
                Dictionary<string, object> locals = (vm.callStack.Peek().module == main && callStack.Count <= 1) ?
                    vm.callStack.Peek().module.attr : vm.callStack.Peek().locals;
                //convert to pydict
                PyDict dic = new PyDict(locals, vm);
                return dic;
            });

            BindBuiltinFunc("eval", (vm, args) =>
            {
                string source = args[0] as string;
                if (source == null)
                {
                    TypeError("eval() arg 1 must be a string");
                    return VM.None;
                }
                return Eval(source, callStack.Peek().module);
            });

            BindBuiltinFunc("exec", (vm, args) =>
            {
                string source = args[0] as string;
                if (source == null)
                {
                    TypeError("exec() arg 1 must be a string");
                    return VM.None;
                }
                CodeObject code = Compile(source, "<exec>", CompileMode.EXEC_MODE, true);
                var mod = callStack.Peek().module;

                //exec() should not check running status, and lock should be held in outer exec() frame
                mod ??= main;
                Frame f = callStack.Peek();

                callStack.Push(new Frame(code, mod, f.callable, f.locals));

                try
                {
                    RunTopFrame();
                    return VM.None;
                }
                catch (CsharpException e)
                {
                    if (throwException || stderr == null) throw;
                    else stderr(e.Message);
                    return VM.None;
                }
                catch (Exception e)
                {
                    if (throwException || stderr == null) throw;
                    else
                    {
                        while (e.InnerException != null) e = e.InnerException;
                        stderr(e.Message);
                        stderr(e.StackTrace);
                    }
                    return VM.None;
                }
            });

            BindBuiltinFunc("input", (vm, args) =>
            {
                if (stdin == null)
                    throw new CsharpException("Please register builtin stdin method before calling input().");

                string prompt;
                if (args.Length == 0)
                    prompt = "";
                else if (args.Length == 1)
                    prompt = args[0] as string;
                else
                {
                    TypeError("input expected at most 1 argument, got " + args.Length);
                    return None;
                }

                return stdin(prompt);
            });

            // register modules
            RandomModule randomModule = new RandomModule();
            randomModule.RegisterRandomModule(this);
            MathModule mathModule = new MathModule();
            mathModule.RegisterMathModule(this);
            SysModule sysModule = new SysModule();
            sysModule.RegisterSysModule(this);

            // register py modules after natvie modules
            Exec(Utils.LoadPythonLib("builtins"), "<builtins>", CompileMode.EXEC_MODE, builtins);
            Exec(Utils.LoadPythonLib("_set"), "<set>", CompileMode.EXEC_MODE, builtins);
            foreach (string name in new string[] { "bisect", "collections", "heapq" })
            {
                lazyModules[name] = Utils.LoadPythonLib(name);
            }
        }

        public PyModule NewModule(string name)
        {
            Utils.Assert(!modules.ContainsKey(name), $"Module {name} already exists");
            var module = new PyModule(name);
            modules[name] = module;
            return module;
        }

        public bool DeleteModule(string name)
        {
            Utils.Assert(!name.Equals(main.name) && !name.Equals(builtins.name), $"{main.name} and {builtins.name} can not be deleted");
            if (modules.TryGetValue(name, out PyModule module))
            {
                module.attr.Clear();
                modules.Remove(name);
                return true;
            }
            return false;
        }

        public PyDynamicType NewTypeObject(PyModule module, string name, PyTypeObject @base)
        {
            string fullname;
            if (module != builtins) fullname = module.name + "." + name;
            else fullname = name;
            PyDynamicType type = new PyDynamicType(fullname, @base);
            module[name] = type;
            return type;
        }

        public void RegisterType(PyTypeObject type, PyModule module = null)
        {
            type.vm = this;
            type.Initialize();
            Utils.Assert(!allTypes.ContainsKey(type.CSType), $"Type {type.CSType} already exists");
            allTypes[type.CSType] = type;
            //register internal PyObject
            if (type is PyObjectType)
                allTypes[typeof(PyObject)] = type;
            if (module != null) module[type.Name] = type;
        }

        public void RegisterAutoType<T>(PyModule module = null, string clsName = null)
        {
            PyTypeObject type = new PyAutoTypeObject<T>(clsName);
            RegisterType(type, module);
        }

        public void RegisterEnumType<T>(string name, PyModule module = null) where T: System.Enum{
            PyTypeObject @base = typeof(object).GetPyType(this);
            PyTypeObject type = NewTypeObject(module ?? builtins, name, @base);
            foreach (var value in Enum.GetValues(typeof(T))){
                string key = Enum.GetName(typeof(T), value);
                type[key] = value;
            }
        }

        public CSharpLambda BindBuiltinFunc(string name, NativeFuncC f)
        {
            return BindFunc(builtins, name, f);
        }

        public CSharpLambda BindFunc(PyObject obj, string name, NativeFuncC f)
        {
            var func = new CSharpLambda(name, f);
            obj[name] = func;
            return func;
        }

        public CodeObject Compile(string source, string filename, CompileMode mode, bool unknownGlobalScope = false)
        {
            // Debug.Log(Utils.Base64Encode(source));
            Bindings.pkpy_compile_to_string(p, Utils.Base64Encode(source), filename, (int)mode, unknownGlobalScope, out bool ok, out string res);
            res = Utils.Base64Decode(res);
            if (ok)
            {
                return CodeObject.FromBytes(res, source);
            }
            else
            {
                if (mode != CompileMode.EXEC_MODE)
                {
                    SyntaxError(res);
                }
                else
                {
                    CompileErrorObject ceo = CompileErrorObject.FromBytes(res, source);
                    // Debug.Log(ceo.filename);
                    // Debug.Log(ceo.lineNo);
                    // Debug.Log(ceo.columnNo);
                    CompileError("Compile Error", ceo);
                }
                return null;
            }
        }

        //If you're using the 'Call' method to directly access members within the VM externally,
        //please make sure you're correctly maintaining the 'isRunning' state on your own.
        //use 'CallExternal(object callable)' instead.
        internal object Call(object callable){
            return Call(callable, new object[0], null);
        }

        private object Call(object callable, object[] args, Dictionary<string, object> kwargs)
        {
            Utils.Assert(callable != null, "callable must not be null");
            object self;
            if (callable is PyBoundMethod bm)
            {
                return Call(bm.func, args.Prepend(bm.self), kwargs);
            }
            if (callable is CSharpMethod cm)
            {

                // filter __call__ from CSharpLambda
                if (!cm.method.Name.Equals("__call__"))
                    CsharpFunctionListener?.Invoke(cm.method.Name, args);
                Utils.Assert(kwargs == null || kwargs.Count == 0, "CSharpMethod does not support kwargs");
                try
                {
                    object res = cm.Invoke(this, args);
                    if (res == null) res = None;
                    return res;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
            if (callable is CSharpLazyMethod clm)
            {
                CsharpFunctionListener?.Invoke(clm.name, args);
                Utils.Assert(kwargs == null || kwargs.Count == 0, "CSharpLazyMethod does not support kwargs");
                try
                {
                    object res = clm.Invoke(this, args);
                    if (res == null) res = None;
                    return res;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
            if (callable is PyFunction f)
            {
                Frame frame = new Frame(f.decl.code, f.module, callable);
                FuncDecl decl = f.decl;
                CodeObject co = decl.code;
                int i = 0;
                if (args.Length < decl.args.Count) TypeError($"expected {decl.args.Count} positional arguments, got {args.Length}");
                // prepare args
                foreach (int ni in decl.args)
                {
                    string name = I2N(co.varnames[ni]);
                    frame.locals[name] = args[i++];
                }
                // prepare kwdefaults
                foreach (FuncDecl.KwArg kv in decl.kwargs)
                {
                    string name = I2N(co.varnames[kv.key]);
                    frame.locals[name] = kv.value;
                }
                // handle *args
                if (decl.starredArg != -1)
                {
                    object[] rest = new object[args.Length - decl.args.Count];
                    Array.Copy(args, decl.args.Count, rest, 0, rest.Length);
                    string name = I2N(co.varnames[decl.starredArg]);
                    frame.locals[name] = rest;
                }
                else
                {
                    // kwdefaults override
                    foreach (FuncDecl.KwArg kv in decl.kwargs)
                    {
                        if (i >= args.Length) break;
                        string name = I2N(co.varnames[kv.key]);
                        frame.locals[name] = args[i++];
                    }
                    if (i < args.Length) TypeError($"too many arguments ({f.decl.code.name})");
                }

                PyDict vkwargs = null;
                if (decl.starredKwarg != -1)
                {
                    vkwargs = new PyDict();
                    string name = I2N(co.varnames[decl.starredKwarg]);
                    frame.locals[name] = vkwargs;
                }

                if (kwargs != null)
                {
                    foreach (KeyValuePair<string, object> kv in kwargs)
                    {
                        bool ok = frame.locals.ContainsKey(kv.Key);
                        if (!ok)
                        {
                            if (vkwargs == null)
                            {
                                TypeError($"unexpected keyword argument '{kv.Key}'");
                            }
                            vkwargs[new PyDictKey(this, kv.Key)] = kv.Value;
                        }
                        else
                        {
                            // use default kwarg if value is None
                            if (PyIsNone(kv.Value)) continue;

                            frame.locals[kv.Key] = kv.Value;
                        }
                    }
                }

                PyFunctionListener?.Invoke(f.decl.code.name, frame.locals);

                if (co.isGenerator)
                {
                    return new PyGenerator(frame, new List<object>());
                }

                if (callStack.Count >= maxRecursionDepth)
                {
                    Error("RecursionError", "maximum recursion depth exceeded");
                    return null;
                }
                callStack.Push(frame);
                return RunTopFrame();
            }

            if (callable is PyTypeObject type)
            {
                // __new__
                object new_f = FindNameInMro(type, "__new__");
                object res = Call(new_f, args.Prepend(type), kwargs);
                // __init__

                object init_f = GetUnboundMethod(res, "__init__", out self, false);
                if (init_f != null) Call(init_f, args.Prepend(self), kwargs);
                return res;
            }

            object call_f = GetUnboundMethod(callable, "__call__", out self, false);
            if (call_f != null)
            {
                //invoke CsharpFunctionListener callback using the original method name
                if (CsharpFunctionListener != null)
                {
                    if (callable is CSharpLambda cl)
                    {
                        CsharpFunctionListener?.Invoke(cl.mname, args.Prepend(self));
                    }
                }
                return Call(call_f, args.Prepend(self), kwargs);
            }

            TypeError("'" + callable.GetPyType(this).Name + "' object is not callable");
            return null;
        }

        public object Eval(string source, PyModule mod = null)
        {
            CodeObject code = Compile(source, "<eval>", CompileMode.EVAL_MODE,  true);

            //eval() should not check running status, and lock should be held in outer exec() frame
            mod ??= main;
            Frame f = callStack.Peek();

            callStack.Push(new Frame(code, mod, f.callable, f.locals));

            try
            {
                object ret = RunTopFrame();
                return ret;
            }
            catch (CsharpException e)
            {
                if (throwException || stderr == null) throw;
                else stderr(e.Message);
                return null;
            }
            catch (Exception e)
            {
                if (throwException || stderr == null) throw;
                else
                {
                    while (e.InnerException != null) e = e.InnerException;
                    stderr(e.Message);
                    stderr(e.StackTrace);
                }
                return null;
            }
        }
        
        public object CallExternal(object callable, bool exitRunningOnFinished = true, bool exitRunningOnError = true){
            lock (@lock)
            {
                if (isRunning)
                {
                    stderr("Multiple threads trying execute codes in one VM at the same time while VM is not thread-safe.");
                }
                isRunning = true;

                //reset keyboardInterruptFlag when executing new code
                keyboardInterruptFlag = false;

                //clear callstack when executing new code
                callStack.Clear();

                try
                {
                    object ret = Call(callable);
                    isRunning = !exitRunningOnFinished;
                    return ret;
                }
                catch (CsharpException e)
                {
                    isRunning = !exitRunningOnError;

                    if (throwException || stderr == null) throw;
                    else stderr(e.Message);

                    return null;
                }
                catch (Exception e)
                {
                    isRunning = !exitRunningOnError;

                    if (throwException || stderr == null) throw;
                    else
                    {
                        while (e.InnerException != null) e = e.InnerException;
                        stderr(e.Message + "\n" + e.StackTrace);
                    }

                    return null;
                }
            }
        }

        public object Exec(CodeObject co, PyModule mod = null, bool exitRunningOnFinished = true, bool exitRunningOnError = true)
        {
            lock (@lock)
            {
                if (isRunning)
                {
                    stderr("Multiple threads trying execute codes in one VM at the same time while VM is not thread-safe.");
                }
                isRunning = true;

                //reset keyboardInterruptFlag when executing new code
                keyboardInterruptFlag = false;

                //clear callstack when executing new code
                callStack.Clear();

                mod ??= main;
                callStack.Push(new Frame(co, mod));
                try
                {
                    object ret = RunTopFrame();
                    isRunning = !exitRunningOnFinished;
                    return ret;
                }
                catch (CsharpException e)
                {
                    isRunning = !exitRunningOnError;

                    if (throwException || stderr == null) throw;
                    else stderr(e.Message);

                    return null;
                }
                catch (Exception e)
                {
                    isRunning = !exitRunningOnError;

                    if (throwException || stderr == null) throw;
                    else
                    {
                        while (e.InnerException != null) e = e.InnerException;
                        stderr(e.Message + "\n" + e.StackTrace);
                    }

                    return null;
                }
            }
        }

        public object Exec(string source, string filename, CompileMode mode = CompileMode.EXEC_MODE, PyModule mod = null, bool exitRunningOnFinished = true, bool exitRunningOnError = true)
        {
            var co = Compile(source, filename, mode);
            return Exec(co, mod, exitRunningOnFinished, exitRunningOnError);
        }

        public object CallMethod(object obj, string name, params object[] args)
        {
            object f = GetUnboundMethod(obj, name, out object self);
            return Call(f, args.Prepend(self), null);
        }

        public object CallMethod(object self, object callable, params object[] args)
        {
            return Call(callable, args.Prepend(self), null);
        }

        public void Error(string type, string msg)
        {
            if (!builtins.attr.ContainsKey(type))
            {
                throw new CsharpException(string.IsNullOrEmpty(msg) ? type : type + ": " + msg);
            }
            PyException e = Call(builtins[type]) as PyException;
            if (callStack.Count == 0 || e == null)
            {
                throw new CsharpException(msg, e);
            }
            e.msg = msg;
            Frame frame = callStack.Peek();
            frame.s.Push(e);
            Raise(false);
        }

        public void CompileError(string msg, CompileErrorObject ceo)
        {
            throw new CsharpException(msg, ceo);
        }

        public void SyntaxError(string msg)
        {
            Error("SyntaxError", msg);
        }

        public void NameError(string name)
        {
            Error("NameError", "name '" + name + "' is not defined");
        }

        public void TypeError(string msg)
        {
            Error("TypeError", msg);
        }

        public void AttributeError(object obj, string name)
        {
            Error("AttributeError", $"'{obj.GetPyType(this).Name}' object has no attribute '{name}'");
        }

        public void IndexError(string msg)
        {
            Error("IndexError", msg);
        }

        public void ValueError(string msg)
        {
            Error("ValueError", msg);
        }

        public void UnboundLocalError(string name)
        {
            string msg = "local variable " + name + " referenced before assignment";
            Error("UnboundLocalError", msg);
        }

        public void ZeroDivisionError(string msg="")
        {
            if (string.IsNullOrEmpty(msg)) msg = "division by zero";
            Error("ZeroDivisionError", msg);
        }

        public void KeyError(object key)
        {
            Error("KeyError", PyRepr(key));
        }

        public void NotImplementedError()
        {
            Error("NotImplementedError", "");
        }

        public void NotImplementedOpcode(sbyte op)
        {
            Error("NotImplementedError", ((Opcode)op).ToString() + " is not supported yet");
        }

        public void CheckType<T>(object t)
        {
            if (t is T) return;
            TypeError($"expected {typeof(T).GetPyType(this).Name.Escape()}, got {t.GetPyType(this).Name.Escape()}");
        }

        public bool IsInstance(object obj, object container)
        {
            if (container is PyTypeObject type)
                return obj.GetPyType(this).IsSubclassOf(type);

            if (container is object[] tuple)
            {
                foreach (var o in tuple)
                {
                    if (!(o is PyTypeObject))
                        TypeError("isinstance() arg 2 must be a type or tuple of types");
                    if (obj.GetPyType(this).IsSubclassOf(o as PyTypeObject))
                        return true;
                }
                return false;
            }

            TypeError("isinstance() arg 2 must be a type or tuple of types");
            return false;
        }

        public T PyCast<T>(object obj)
        {
            if (obj == None) return default;
            CheckType<T>(obj);
            return (T)obj;
        }

        public bool PyIs(object lhs, object rhs)
        {
            /*
                in python, float is not value type, which means a=1.1; b=1.1; => a is b == False
                This seems a bit unreasonable, so I just ignore this case and take float as value type
                TODO: Need double check here, and The current valuestack implemented using C# does not support the implementation of methods similar to native Python.
            */
            //here we must handle value type first, except float type
            if (lhs is int || rhs is int ||
                lhs is bool || rhs is bool ||
                lhs is string || rhs is string ||
                lhs is float || rhs is float)
                return PyEquals(lhs, rhs);

            // if (lhs is float || rhs is float)
            // {
            //     GCHandle lhsHandle = GCHandle.Alloc(lhs, GCHandleType.WeakTrackResurrection);
            //     int lhsaddr = GCHandle.ToIntPtr(lhsHandle).ToInt32();
            //     GCHandle rhsHandle = GCHandle.Alloc(rhs, GCHandleType.WeakTrackResurrection);
            //     int rhsaddr = GCHandle.ToIntPtr(rhsHandle).ToInt32();
            //     return (lhsaddr == rhsaddr);
            // }

            //handle special types
            if (PyIsNone(lhs) || PyIsNone(rhs) ||
                lhs is EllipsisType || rhs is EllipsisType ||
                lhs is NotImplementedType || rhs is NotImplementedType ||
                lhs is StopIterationType || rhs is StopIterationType)
                return PyEquals(lhs, rhs);

            if (lhs == rhs) return true;
            return false;
        }

        public bool PyEquals(object lhs, object rhs)
        {
            //Handle special types
            bool hasSpType = PyIsNone(lhs) || PyIsNone(rhs) ||
                lhs is EllipsisType || rhs is EllipsisType ||
                lhs is NotImplementedType || rhs is NotImplementedType ||
                lhs is StopIterationType || rhs is StopIterationType;

            if (lhs == rhs) return true;
            if (hasSpType) return false;
            object res;
            res = CallMethod(lhs, "__eq__", rhs);
            if (res != NotImplemented) return (bool)res;
            res = CallMethod(rhs, "__eq__", lhs);
            if (res != NotImplemented) return (bool)res;
            return false;
        }

        public bool PyIsNone(object o)
        {
            return Equals(o, VM.None);
        }

        object PyIter(object obj)
        {
            object f = GetUnboundMethod(obj, "__iter__", out object self, false);
            if (f != null) return CallMethod(self, f);
            if (obj is IEnumerator enumerator) return new PyIterator(enumerator);
            if (obj is IEnumerable enumerable) return new PyIterator(enumerable.GetEnumerator());
            TypeError($"'{obj.GetPyType(this).Name}' object is not iterable");
            return null;
        }

        object PyNext(object obj)
        {
            object f = GetUnboundMethod(obj, "__next__", out object self, false);
            if (f != null) return CallMethod(self, f);
            if (obj is PyIterator it)
            {
                if (it.MoveNext())
                {
                    object val = it.Current;
                    if (val is char v) return new string(v, 1);
                    return val;
                }
                return StopIteration;
            }
            if (obj is PyGenerator ge)
            {
                if (ge.state == 2)
                    return StopIteration;

                foreach (object o in ge.s_backup)
                {
                    ge.frame.s.Push(o);
                }
                ge.s_backup.Clear();
                callStack.Push(ge.frame);

                object ret;
                try
                {
                    ret = RunTopFrame();
                }
                catch (Exception e)
                {
                    ge.state = 2;
                    throw;
                }

                if (ret == YieldOp)
                {
                    ge.frame = callStack.Peek();
                    //pop ret
                    ret = ge.frame.s.Pop();
                    //store the context
                    foreach (var o in ge.frame.s)
                    {
                        ge.s_backup.Add(o);
                    }
                    callStack.Pop();
                    ge.state = 1;
                    if (ret == StopIteration)
                        ge.state = 2;
                    return ret;
                }
                else
                {
                    ge.state = 2;
                    return StopIteration;
                }
            }
            TypeError($"'{obj.GetPyType(this).Name}' object is not an iterator");
            return null;
        }

        public bool PyBool(object obj)
        {
#pragma warning disable IDE0038
            if (obj is bool) return (bool)obj;
            if (obj == None) return false;
            if (obj is int) return (int)obj != 0;
            if (obj is float) return (float)obj != 0.0f;
#pragma warning restore IDE0038
            // check __len__ for other types
            object f = GetUnboundMethod(obj, "__len__", out object self, false);
            if (f != null) return (int)CallMethod(self, f) > 0;
            return true;
        }

        public string PyStr(object obj)
        {
            object f = GetUnboundMethod(obj, "__str__", out object self, false);
            if (f != null) return (string)CallMethod(self, f);
            return PyRepr(obj);
        }

        public string PyRepr(object obj)
        {
            if (PyIsNone(obj)) return "None";
            if (obj is EllipsisType) return "Ellipsis";

            return (string)CallMethod(obj, "__repr__");
        }

        public int PyHash(object obj)
        {
            return (int)CallMethod(obj, "__hash__");
        }

        public PyModule PyImport(string key)
        {
            PyImportModuleListener?.Invoke(key);
            if (modules.TryGetValue(key, out PyModule module))
            {
                return module;
            }
            if (lazyModules.TryGetValue(key, out string source))
            {
                //do not remove lazyModules key anymore since we can delete existing modules
                module = NewModule(key);
                Exec(source, key + ".py", CompileMode.EXEC_MODE, module);
                return module;
            }
            Error("ImportError", "cannot import name " + key.Escape());
            return null;
        }

        public List<object> PyList(object obj)
        {
            object it = PyIter(obj);
            var res = new List<object>();
            while (true)
            {
                object next = PyNext(it);
                if (next == StopIteration) break;
                res.Add(next);
            }
            return res;
        }

        public object GetUnboundMethod(object obj, string name, out object self, bool throwErr = true, bool fallback = false)
        {
            self = null;
            PyTypeObject objtype;
            // handle super() proxy
            if (obj is PySuper super)
            {
                obj = super.first;
                objtype = super.second;
            }
            else
            {
                objtype = obj.GetPyType(this);
            }
            object clsVar = FindNameInMro(objtype, name);

            if (fallback)
            {
                if (clsVar != null)
                {
                    // handle descriptor
                    if (clsVar is PyProperty prop)
                    {
                        return Call(prop.getter, new object[] { obj }, null);
                    }
                }
                // handle instance __dict__
                if (obj is PyObject)
                {
                    if ((obj as PyObject).attr.TryGetValue(name, out object val))
                    {
                        return val;
                    }
                }
            }
            if (clsVar != null)
            {
                // bound method is non-data descriptor
                if (clsVar is ITrivialCallable)
                {
                    self = obj;
                }
                return clsVar;
            }
            if (throwErr) AttributeError(obj, name);
            return null;
        }

        public object GetAttr(object obj, string name, bool throwErr = true)
        {
            PyTypeObject objtype;
            // handle super() proxy
            if (obj is PySuper)
            {
                PySuper super = obj as PySuper;
                obj = super.first;
                objtype = super.second;
            }
            else
            {
                objtype = obj.GetPyType(this);
            }
            object clsVar = FindNameInMro(objtype, name);
            if (clsVar != null)
            {
                // handle descriptor
                if (clsVar is PyProperty)
                {
                    PyProperty prop = clsVar as PyProperty;
                    return Call(prop.getter, new object[] {obj}, null);
                }
            }
            // handle instance __dict__
            if (obj is PyObject)
            {
                if ((obj as PyObject).attr.TryGetValue(name, out object val))
                {
                    return val;
                }

                //handle dynamic type inherit
                if (obj is PyDynamicType)
                {
                    PyTypeObject _0 = obj as PyTypeObject;
                    while (_0.GetBaseType() != VM.None)
                    {
                        if ((_0.GetBaseType() as PyObject).attr.TryGetValue(name, out val))
                        {
                            return val;
                        }
                        _0 = _0.GetBaseType() as PyTypeObject;
                    }
                }

            }
            if (clsVar != null)
            {
                // bound method is non-data descriptor
                if (clsVar is ITrivialCallable)
                {
                    return new PyBoundMethod(obj, clsVar, name);
                }
                return clsVar;
            }


            object getattr = FindNameInMro(objtype, "__getattr__");
            if (getattr != null)
            {
                return Call(getattr, new object[] {obj, name}, null);
            }
            if (throwErr) AttributeError(obj, name);
            return null;
        }

        public bool HasAttr(object obj, string name)
        {
            object res = GetAttr(obj, name, false);
            return res != null;
        }

        public PyNone SetAttr(object obj, string name, object value)
        {
            PyTypeObject objtype;
            if (obj is PySuper super)
            {
                obj = super.first;
                objtype = super.second;
            }
            else
            {
                objtype = obj.GetPyType(this);
            }

            object setattr = FindNameInMro(objtype, "__setattr__");
            if (setattr != null)
            {
                Call(setattr, new object[] { obj, name, value }, null);
                return None;
            }

            object clsVar = FindNameInMro(objtype, name);
            if (clsVar != null)
            {
                // handle descriptor
                if (clsVar is PyProperty prop)
                {
                    if (prop.setter != None)
                    {
                        Call(prop.setter, new object[] { obj, value }, null);
                    }
                    else
                    {
                        TypeError("readonly attribute");
                    }
                    return None;
                }
            }
            // handle instance __dict__
            PyObject val = obj as PyObject;
            if (val == null) TypeError("cannot set attribute");
            val[name] = value;
            return None;
        }

        public object FindNameInMro(PyTypeObject cls, string name)
        {
            do
            {
                if (cls.attr.TryGetValue(name, out object val)) return val;
                object @base = cls.GetBaseType();
                if (@base == None) break;
                cls = @base as PyTypeObject;
            } while (true);
            return null;
        }

        public int NormalizedIndex(int index, int size)
        {
            if (index < 0) index += size;
            if (index < 0 || index >= size)
            {
                IndexError($"{index} not in [0, {size})");
            }
            return index;
        }

        public void ParseIntSlice(PySlice s, int length, out int start, out int stop, out int step)
        {
            static int clip(int value, int min, int max)
            {
                if (value < min) return min;
                if (value > max) return max;
                return value;
            }

            // handle NoneType
            if (s.step == None) step = 1;
            else step = PyCast<int>(s.step);
            if (step == 0) ValueError("slice step cannot be zero");
            if (step > 0)
            {
                if (s.start == None)
                {
                    start = 0;
                }
                else
                {
                    start = PyCast<int>(s.start);
                    if (start < 0) start += length;
                    start = clip(start, 0, length);
                }
                if (s.stop == None)
                {
                    stop = length;
                }
                else
                {
                    stop = PyCast<int>(s.stop);
                    if (stop < 0) stop += length;
                    stop = clip(stop, 0, length);
                }
            }
            else
            {
                if (s.start == None)
                {
                    start = length - 1;
                }
                else
                {
                    start = PyCast<int>(s.start);
                    if (start < 0) start += length;
                    start = clip(start, -1, length - 1);
                }
                if (s.stop == None)
                {
                    stop = -1;
                }
                else
                {
                    stop = PyCast<int>(s.stop);
                    if (stop < 0) stop += length;
                    stop = clip(stop, -1, length - 1);
                }
            }
        }

        internal void BINARY_OP_SPECIAL_EX(Frame frame, string op, string name, string rname = null)
        {
            object _1 = frame.s.Pop();
            object _0 = frame.s.Top();
            BinaryOpListener?.Invoke(op, _0, _1);
            object _2 = GetUnboundMethod(_0, name, out object self, false);
            if (_2 != null) frame.s.SetTop(CallMethod(self, _2, _1));
            else frame.s.SetTop(NotImplemented);
            if (frame.s.Top() != NotImplemented) return;

            //or else use reverse
            if (rname != null)
            {
                _2 = GetUnboundMethod(_1, rname, out self, false);
                if (_2 != null)
                {
                    frame.s.SetTop(CallMethod(self, _2, _0));
                }
                else frame.s.SetTop(NotImplemented);
            }

            if (frame.s.Top() == NotImplemented)
            {
                Error("TypeError", "unsupported operand type(s) for " + op);
            }
        }

        internal PyDict PopUnpackAsDict(ValueStack s, int n)
        {
            PyDict d = new PyDict();
            object[] args = s.PopNReversed(n);
            foreach (var item in args)
            {
                if (item is PyStarWrapper w)
                {
                    if (w.level != 2) TypeError("expected level 2 star wrapper");
                    PyDict other = PyCast<PyDict>(w.obj);
                    foreach (var item2 in other) d[item2.Key] = item2.Value;
                }
                else
                {
                    object[] t = PyCast<object[]>(item);
                    if (t.Length != 2) TypeError("expected tuple of length 2");
                    d[new PyDictKey(this, t[0])] = t[1];
                }
            }
            return d;
        }

        internal static string I2N(int index) => I2N(new StrName(index));

        internal static string I2N(StrName name)
        {
            if (CodeObject.nameMapping.TryGetValue(name, out var val))
            {
                return val;
            }
            throw new CsharpException($"{name.index} does not exist in CodeObject.nameMapping");
        }

        internal List<object> PopUnpackAsList(ValueStack s, int n)
        {
            object[] tuple = s.PopNReversed(n);
            List<object> list = new List<object>();
            for (int i = 0; i < n; i++)
            {
                if (!(tuple[i] is PyStarWrapper wrapper)) list.Add(tuple[i]);
                else
                {
                    if (wrapper.level != 1) TypeError("expected level 1 star wrapper");
                    list.AddRange(PyList(wrapper.obj));
                }
            }
            return list;
        }

        internal void Raise(bool reRaise)
        {
            Frame frame = callStack.Peek();
            PyException exception = frame.s.Top() as PyException;
            if (exception == null)
                throw new CsharpException("Callstacks error.");
            if (!reRaise)
            {
                exception.ipOnError = frame.ip;
                exception.coOnError = frame.co;
            }

            bool ok = frame.JumpToExceptionHandler();

            //record stacktrace
            int actualip = frame.ip;
            if (exception.ipOnError >= 0 && exception.coOnError == frame.co)
                actualip = exception.ipOnError;
            string funcname = frame.co.name;
            if (frame.callable == null) //not in function call
                funcname = string.Empty;
            exception.PushStacktrace(frame.co, actualip, funcname);

            if (ok) throw new HandledException();
            throw new UnhandledException();
        }

        public void KeyboardInterrupt()
        {
            this.keyboardInterruptFlag = true;
        }

        internal void EvalOnStepCheck()
        {
            if (keyboardInterruptFlag)
            {
                keyboardInterruptFlag = false;
                Error("KeyboardInterrupt", "");
            }
        }
    }
}
