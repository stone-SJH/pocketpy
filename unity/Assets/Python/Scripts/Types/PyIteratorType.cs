using System;
using System.Collections;

namespace Python
{
    public class PyIterator : IEnumerator
    {
        IEnumerator proxy;

        public PyIterator(IEnumerator proxy)
        {
            this.proxy = proxy;
        }

        public object Current => proxy.Current;

        public bool MoveNext()
        {
            return proxy.MoveNext();
        }

        public void Reset()
        {
            proxy.Reset();
        }
    }

    public class PyIteratorType : PyTypeObject
    {
        public override string Name
        {
            get => "iterator";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override Type CSType => typeof(PyIterator);

        [PythonBinding]
        public object __iter__(PyIterator self)
        {
            return self;
        }
    }

}
