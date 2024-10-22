using System;

namespace Python
{
    public class PySlice
    {
        public object start;
        public object stop;
        public object step;

        public PySlice(object start, object stop, object step)
        {
            this.start = start;
            this.stop = stop;
            this.step = step;
        }
    }

    public class PySliceType : PyTypeObject
    {
        public override string Name
        {
            get => "slice";

            set => throw new NotImplementedException("slice type cannot be renamed.");
        }
        public override System.Type CSType => typeof(PySlice);


        [PythonBinding]
        public object __new__(PyTypeObject type, params object[] args)
        {
            if (args.Length == 0)
            {
                vm.TypeError("slice expected at least 1 argument, got 0");
                return VM.None;
            }
            if (args.Length == 1)
            {
                return new PySlice(VM.None, args[0], VM.None);
            }
            if (args.Length == 2)
            {
                return new PySlice(args[0], args[1], VM.None);
            }
            if (args.Length == 3)
            {
                return new PySlice(args[0], args[1], args[2]);
            }
            vm.TypeError("slice expected at most 3 arguments, got " + args.Length);
            return VM.None;
        }

        [PythonBinding]
        public bool __eq__(object self, object other)
        {
            if (self is PySlice lhs && other is PySlice rhs)
            {
                if (!vm.PyEquals(lhs.start, rhs.start)) return false;
                if (!vm.PyEquals(lhs.step, rhs.step)) return false;
                if (!vm.PyEquals(lhs.stop, rhs.stop)) return false;

                return true;
            }
            return false;
        }

        [PythonBinding]
        public object __repr__(object value)
        {
            if (value is PySlice s)
                return $"slice({vm.PyRepr(s.start)}, {vm.PyRepr(s.stop)}, {vm.PyRepr(s.step)})";

            return VM.None;
        }

        [PythonBinding(BindingType.Getter)]
        public object start(PySlice slice)
        {
            return slice.start;
        }

        [PythonBinding(BindingType.Getter)]
        public object stop(PySlice slice)
        {
            return slice.stop;
        }

        [PythonBinding(BindingType.Getter)]
        public object step(PySlice slice)
        {
            return slice.step;
        }
    }
}
