using System;

namespace Python.Modules
{
    public class RandomModule
    {
        private Random _random;

        public void RegisterRandomModule(VM vm)
        {
            PyModule random = vm.NewModule("random");
            _random = new Random();

            vm.BindFunc(random, "seed",  (vm1, args) =>
            {
                _random = new Random((int)args[0]);
                return VM.None;
            });

            vm.BindFunc(random, "randint",  (vm1, args) =>
            {
                int a = (int) args[0];
                int b = (int) args[1];
                return _random.Next(a, b);
            });

            vm.BindFunc(random, "rand",  (vm1, args) => (float)_random.NextDouble());

            vm.BindFunc(random, "uniform",  (vm1, args) =>
            {
                float a = Utils.Cast<float>(args[0]);
                float b = Utils.Cast<float>(args[1]);

                double sample = _random.NextDouble();
                return (float)(sample * (b - a) + a);
            });

            vm.Exec(Utils.LoadPythonLib("random"), "<random>", CompileMode.EXEC_MODE, random);
        }
    }
}
