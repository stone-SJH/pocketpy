using System;
using System.Collections.Generic;

namespace Python
{
    public class PyGenerator
    {
        public Frame frame;
        public int state;
        public List<object> s_backup;

        public PyGenerator(Frame frame, List<object> s)
        {
            this.frame = frame;
            this.s_backup = s;
            this.state = 0;
        }
    }

    public class PyGeneratorType : PyTypeObject
    {
        public override string Name
        {
            get => "generator";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override Type CSType => typeof(PyGenerator);

        [PythonBinding]
        public object __iter__(PyGenerator self)
        {
            return self;
        }
    }
}
