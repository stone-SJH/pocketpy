using System;
using System.Reflection;

namespace Python
{
    /// <summary>
    /// A method defined in C# which is lazy evaluated.
    /// </summary>
    public class CSharpLazyMethod : ITrivialCallable
    {
        public Type type;
        public string name;
        public bool isStatic;

        public CSharpLazyMethod(Type type, string name, bool isStatic)
        {
            this.type = type;
            this.name = name;
            this.isStatic = isStatic;
        }

        public object Invoke(VM vm, object[] args)
        {
            object self = null;
            BindingFlags flags;
            if (isStatic)
            {
                flags = BindingFlags.Static | BindingFlags.Public;
            }
            else
            {
                flags = BindingFlags.Instance | BindingFlags.Public;
                self = args[0];
                args = args.SubArray(1);
            }
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++) types[i] = args[i].GetType();
            var method = this.type.GetMethod(this.name, flags, null, types, null);
            if (method != null)
                return method.Invoke(self, args);

            //We use methods with exactly (object[]) parameters as fallbacks. If such a method exists from the current type, the args are packaged and called.
            Type[] fallbackTypes = new Type[1] {typeof(object[])};
            method = this.type.GetMethod(this.name, flags, null, fallbackTypes, null);
            if (method == null)
                vm.TypeError("cannot find a overload method with the given arguments");
            return method.Invoke(self, new object[1] {args});
        }
    }

    public class CSharpLazyMethodType : PyTypeObject
    {
        public override string Name
        {
            get => "CSharpLazyMethod";

            set => throw new NotImplementedException("built-in type cannot be renamed.");
        }
        public override Type CSType => typeof(CSharpLazyMethod);

        [PythonBinding]
        public string __repr__(CSharpLazyMethod value)
        {
            string prefix = value.isStatic ? "static_" : "";
            return $"<{prefix}method '{value.name}' of {value.type.FullName}>";
        }
    }

}
