using System;

namespace Python
{
    public class PyExceptionType : PyTypeObject
    {
        public override string Name
        {
            get => "Exception";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override System.Type CSType => typeof(PyException);

        [PythonBinding]
        public object __new__(PyTypeObject type, params object[] _)
        {
            return new PyException(type);
        }
    }
}
