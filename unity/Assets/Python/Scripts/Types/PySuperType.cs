using System;

namespace Python
{
    public class PySuper
    {
        public object first;
        public PyTypeObject second;

        public PySuper(object first, PyTypeObject second)
        {
            this.first = first;
            this.second = second;
        }
    }

    public class PySuperType : PyTypeObject
    {
        public override string Name
        {
            get => "super";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override System.Type CSType => typeof(PySuper);
    }
}
