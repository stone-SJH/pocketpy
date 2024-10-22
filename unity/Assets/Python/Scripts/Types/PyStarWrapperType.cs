using System;

namespace Python
{
    public class PyStarWrapper
    {
        public object obj;
        public int level;

        public PyStarWrapper(object obj, int level)
        {
            this.obj = obj;
            this.level = level;
        }
    }

    public class PyStarWrapperType : PyTypeObject
    {
        public override string Name
        {
            get => "_star_wrapper";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override System.Type CSType => typeof(PyStarWrapper);
    }
}
