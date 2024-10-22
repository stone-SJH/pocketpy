using System;
using UnityEngine;

namespace Python.Modules
{
    public class MathModule
    {
        private const float pi = 3.1415926535897932384f;
        private const float e = 2.7182818284590452354f;
        private const float inf = float.PositiveInfinity;
        private const float nan = float.NaN;

        public void RegisterMathModule(VM vm)
        {
            PyModule math = vm.NewModule("math");

            math["pi"] = pi;
            math["e"] = e;
            math["inf"] = inf;
            math["nan"] = nan;

            vm.BindFunc(math, "ceil", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.CeilToInt(value);
            });

            vm.BindFunc(math, "fabs", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Abs(value);
            });

            vm.BindFunc(math, "floor", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.FloorToInt(value);
            });

            vm.BindFunc(math, "fsum", (vm1, args) =>
            {
                float result = 0;
                foreach (var arg in args)
                {
                    result += Utils.Cast<float>(arg);
                }

                return result;
            });

            vm.BindFunc(math, "gcd", (vm1, args) =>
            {
                int a = Utils.Cast<int>(args[0]);
                int b = Utils.Cast<int>(args[1]);
                a = Math.Abs(a);
                b = Math.Abs(b);
                while (b != 0)
                {
                    int t = b;
                    b = a % b;
                    a = t;
                }

                return a;
            });

            vm.BindFunc(math, "isfinite", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return !(value.Equals(Mathf.Infinity) || value.Equals(Mathf.NegativeInfinity));
            });

            vm.BindFunc(math, "isinf", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return value.Equals(Mathf.Infinity) || value.Equals(Mathf.NegativeInfinity);
            });

            vm.BindFunc(math, "isnan", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return value.Equals(float.NaN);
            });

            vm.BindFunc(math, "isclose", (vm1, args) =>
            {
                float a = Utils.Cast<float>(args[0]);
                float b = Utils.Cast<float>(args[1]);
                return Mathf.Approximately(a, b);
            });

            vm.BindFunc(math, "exp", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Exp(value);
            });

            vm.BindFunc(math, "log", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Log(value);
            });

            vm.BindFunc(math, "log2", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Log(value, 2);
            });

            vm.BindFunc(math, "Log10", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Log10(value);
            });

            vm.BindFunc(math, "pow", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                float p = Utils.Cast<float>(args[1]);
                return Mathf.Pow(value, p);
            });

            vm.BindFunc(math, "sqrt", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Sqrt(value);
            });

            vm.BindFunc(math, "acos", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Acos(value);
            });

            vm.BindFunc(math, "asin", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Asin(value);
            });

            vm.BindFunc(math, "atan", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Atan(value);
            });

            vm.BindFunc(math, "atan2", (vm1, args) =>
            {
                float y = Utils.Cast<float>(args[0]);
                float x = Utils.Cast<float>(args[1]);
                return Mathf.Atan2(y, x);
            });

            vm.BindFunc(math, "cos", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Cos(value);
            });

            vm.BindFunc(math, "sin",(vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Sin(value);
            });

            vm.BindFunc(math, "tan", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return Mathf.Tan(value);
            });

            vm.BindFunc(math, "degrees",  (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return value * 180 / pi;
            });

            vm.BindFunc(math, "radians", (vm1, args) =>
            {
                float value = Utils.Cast<float>(args[0]);
                return value * pi / 180;
            });

            vm.BindFunc(math, "factorial", (vm1, args) =>
            {
                int value = Utils.Cast<int>(args[0]);
                if (value < 0)
                    vm1.ValueError("factorial() not defined for negative values");
                int result = 1;
                for (int i = 2; i <= value; i++)
                    result *= i;
                return result;
            });
        }
    }
}
