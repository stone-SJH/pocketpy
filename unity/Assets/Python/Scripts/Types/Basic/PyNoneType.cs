// unset

using System;

namespace Python
{
    public class PyNone : PyObject
    {
        //for c# usage
        public override string ToString()
        {
            return "None";
        }
    }

    public class PyNoneType : PyTypeObject
    {
        private static volatile PyNone instance;
        private static object @lock = new object();
        public static PyNone None
        {
            get
            {
                if (instance == null)
                {
                    lock (@lock)
                    {
                        if (instance == null)
                            instance = new PyNone();
                    }
                }

                return instance;
            }
        }


        public override Type CSType => typeof(PyNone);
        public override string Name
        {
            get => "NoneType";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }

        public override object GetBaseType()
        {
            return None;
        }

        [PythonBinding]
        public string __str__(PyNone None)
        {
            return "None";
        }

        [PythonBinding]
        public string __repr__(PyNone None)
        {
            return "None";
        }

        [PythonBinding]
        public object __eq__(PyNone self, object other)
        {
            if (vm.PyIsNone(other))
                return true;

            return VM.NotImplemented;
        }
    }
}
