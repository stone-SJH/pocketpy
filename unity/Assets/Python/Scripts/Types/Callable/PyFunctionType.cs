using System;

namespace Python
{
    /// <summary>
    /// A function defined in Python.
    /// </summary>
    public class PyFunction : ITrivialCallable
    {
        public FuncDecl decl;
        public PyModule module;
        public PyObject @class;

        public PyFunction(FuncDecl decl, PyModule module)
        {
            this.decl = decl;
            this.module = module;
        }

        public override string ToString()
        {
            return $"{decl.code.name}()";
        }
    }

    public class PyFunctionType : PyTypeObject
    {
        public override string Name
        {
            get => "function";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override Type CSType => typeof(PyFunction);
    }
}
