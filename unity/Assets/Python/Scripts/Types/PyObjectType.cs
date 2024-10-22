using System;
using System.Collections.Generic;
using System.Text;

namespace Python
{
    public class PyMappingProxy
    {
        public PyObject obj;

        public PyMappingProxy(PyObject obj)
        {
            this.obj = obj;
        }

        public Dictionary<string, object> attr()
        {
            return obj.attr;
        }
    }

    public class PyMappingProxyType : PyTypeObject
    {
        public override string Name
        {
            get => "mappingproxy";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }

        public override Type CSType => typeof(PyMappingProxy);

        [PythonBinding]
        public string __repr__(PyMappingProxy mp)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("mappingproxy(");
            builder.Append(ReprDict(new PyDict(mp.attr(), vm)));
            builder.Append(")");
            return builder.ToString();
        }
    }


    public class PyObjectType : PyTypeObject
    {
        public override string Name
        {
            get => "object";

            set => throw new NotImplementedException("object type cannot be renamed.");
        }
        public override Type CSType => typeof(object);

        public override object GetBaseType() => VM.None;

        [PythonBinding]
        public object __new__(PyTypeObject type, params object[] _)
        {
            return new PyDynamic(type);
        }

        [PythonBinding]
        public object __repr__(object value)
        {
            return $"<{value.GetPyType(vm).Name} object at {value.GetHashCode()}>";
        }

        [PythonBinding]
        public object __eq__(object self, object other)
        {
            return self == other;
        }

        [PythonBinding]
        public int __hash__(object self)
        {
            return self.GetHashCode();
        }

        [PythonBinding(BindingType.Getter)]
        public object __dict__(object obj)
        {
            if (obj is PyObject pyobj)
            {
                if (obj is PyTypeObject)
                {
                    return (obj as PyObject).mappingproxy;
                }
                PyDict dict = new PyDict(pyobj.attr, vm);
                return dict;
            }

            vm.AttributeError(obj, "__dict__");
            return VM.None;
        }
    }
}
