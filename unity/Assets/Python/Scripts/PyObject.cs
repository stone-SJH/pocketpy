using System;
using System.Collections.Generic;

namespace Python
{
    public static class Version
    {
        public const string Frontend = "2.0.1";
    }


    public class NotImplementedType { internal NotImplementedType() { } }
    public class StopIterationType { internal StopIterationType() { } }
    public class EllipsisType { internal EllipsisType() { } }
    public class YieldType {internal YieldType() {}}

    public class PyObject
    {
        public PyMappingProxy mappingproxy;

        public Dictionary<string, object> attr = new Dictionary<string, object>();

        public PyObject()
        {
            mappingproxy = new PyMappingProxy(this);
        }

        public object this[string key]
        {
            get => attr[key];
            set => attr[key] = value;
        }

    }
}
