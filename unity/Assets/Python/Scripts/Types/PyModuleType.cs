using System;

namespace Python
{
    public class PyModule : PyObject
    {
        public string name { get; private set; }

        public PyModule(string name)
        {
            this.name = name;
        }
    }

    public class PyModuleType : PyTypeObject
    {
        public override string Name
        {
            get => "module";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override System.Type CSType => typeof(PyModule);
    }
}
