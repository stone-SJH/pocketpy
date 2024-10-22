using System;

namespace Python
{
    public delegate object NativeFuncC(VM vm, object[] args);

    public class CSharpLambda : ITrivialCallable
    {
        public string mname;
        public NativeFuncC f;

        public CSharpLambda(string methodName, NativeFuncC f)
        {
            this.mname = methodName;
            this.f = f;
        }
    }

    public class CSharpLambdaType : PyTypeObject
    {
        public override string Name
        {
            get => "CSharpLambda";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override System.Type CSType => typeof(CSharpLambda);

        [PythonBinding]
        public object __call__(CSharpLambda self, params object[] args)
        {
            return self.f(vm, args);
        }
    }
}
