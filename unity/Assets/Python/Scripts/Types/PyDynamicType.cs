using System;

namespace Python
{
    public class PyDynamic : PyObject
    {
        public PyTypeObject type;

        public PyDynamic(PyTypeObject type)
        {
            this.type = type;
        }

        public override string ToString()
        {
            return $"<{type.Name} object at {this.GetHashCode()}>";
        }

        // ~PyDynamic()
        // {
        //     Debug.Log($"Destructor of <{type.Name} object at {this.GetHashCode()}> was called.");
        // }
    }

    public class PyDynamicType : PyTypeObject
    {
        public string mName;
        public PyTypeObject mBase;

        public PyDynamicType(string name, PyTypeObject baseType)
        {
            mName = name;
            mBase = baseType;
        }

        public override string Name
        {
            get => this.mName;

            set => this.mName = value;
        }
        public override Type CSType => mBase.CSType;
        public override object GetBaseType() => mBase;

        public override string ToString()
        {
            return $"<class '{Name}'>";
        }
    }

}
