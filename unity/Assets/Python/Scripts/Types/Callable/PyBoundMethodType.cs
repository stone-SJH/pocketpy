using System;

namespace Python
{
    public class PyBoundMethod
    {
        public object self;
        public object func;
        public string mname;

        public PyBoundMethod(object self, object func, string mname)
        {
            this.self = self;
            this.func = func;
            this.mname = mname;
        }
    }

    public class PyBoundMethodType : PyTypeObject
    {
        public override string Name
        {
            get => "method-wrapper";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override System.Type CSType => typeof(PyBoundMethod);

        [PythonBinding]
        public object __eq__(PyBoundMethod self, object other)
        {
            PyBoundMethod otherMethod = other as PyBoundMethod;
            if (otherMethod == null) return VM.NotImplemented;
            return self.self == otherMethod.self && self.func == otherMethod.func;
        }

        [PythonBinding]
        public object __repr__(PyBoundMethod self)
        {
            return $"<method-wrapper '{self.mname}' of {self.self.GetPyType(vm).Name} object at {self.GetHashCode()}>";
        }
    }
}
