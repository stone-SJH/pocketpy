using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Python;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public class PyDemo : MonoBehaviour
{
    public Controller Controller;
    VM vm;
    PyModule mod;

    private Thread th1;
    private Thread th2;

    const string pyscript = @"
import random

def start():
    speed = -1.3
    speed2 = speed + 3.2 - 0.2*6.2/3
    dis = 10.1
    print(speed2, dis)
    controller.MoveForward(controller.Character, dis, speed2)
    print('done\n')
    controller.Say('hello')
    controller.Say('hello', 123, 'world', 23.3, None)
    controller.Say3213('hello', 123, 'world', 23.3, None)
print(controller)

list = [1, 23, 3.3, 201, 2, 'i']
list2 = [1, 2, 3, 4, 5, 6, 7]
random.seed(11231)
print(random.randint(1, 19))
print(random.rand())
print(random.uniform(9, 9.9))


#print(list)
#random.shuffle(list)
#print(list)
#print(random.choice(list))
";

    private const string pyscript2 = @"
import random

list = [1, 23, 3.3, 201, 2, 'i']
list2 = [1, 2, 3, 4, 5, 6, 7]
random.seed(11231)
print(random.randint(1, 19))
print(random.rand())
print(random.uniform(9, 9.9))
";
    private const string pyscript3 = @"
a = 1
print(a)

class A:
    def __init__(self, a, b):
        self.a = a
        self.b = b

    def add(self):
        return self.a + self.b

    def sub(self):
        return self.a - self.b

for i in range(1000000):
    a = A(1, 2)
    c = a.add() - a.sub()
    #print(a)

def b():
    print('bbbb')

b()
print(controller)
";
    private const string pyscript4 = @"
print(controller)
print(a)
b()
";

    // Start is called before the first frame update
    void Start()
    {
        // List<object> a = new List<object>();
        // a.Add(1);
        // a.Add(2);
        // a.Add(a);
        //
        // List<object> b = new List<object>();
        // b.Add(1);
        // b.Add(2);
        // b.Add(b);
        //
        // if (b == a)
        // {
        //     Debug.Log("true");
        // }

        // return;
        try
        {
            Debug.Log("MainThread: " + Thread.CurrentThread.ManagedThreadId);
            vm = new VM();

            //vm.CsharpFunctionListener +=CsharpFuncCollector;
            vm.RegisterAutoType<Controller>(null, "xxxxControllerxxxx");
            vm.RegisterAutoType<GameObject>();


            // th1 = new Thread((o =>
            // {
            //     Profiler.BeginThreadProfiling("python working thread", "working thread1");
            //     VM _vm = (VM) o;
            //     while (_vm.isRunning)
            //         Thread.Sleep(100);
            //     PyModule testmod1 = _vm.NewModule("test1");
            //     testmod1["controller"] = Controller;
            //     _vm.Exec(pyscript3, "pydemo3.py", CompileMode.EXEC_MODE, testmod1);
            //     Debug.Log(_vm.DeleteModule("test1"));
            //     Profiler.EndThreadProfiling();
            // }));
            // th1.Start(vm);

            //test moveforward
            vm.main["controller"] = Controller;

            vm.Exec(pyscript, "pydemo.py");

             Thread th = new Thread((o =>
             {
                 VM _vm = (VM) o;
                 while (_vm.isRunning)
                     Thread.Sleep(100);
                 _vm.Exec(pyscript, "pydemo.py", CompileMode.EXEC_MODE, null);
                 while (_vm.isRunning)
                     Thread.Sleep(100);
                 try
                 {
                     _vm.CallExternal(vm.main["start"]);
                 }
                 catch (CsharpException ex)
                 {
                     Debug.LogError(ex.Message);
                 }

                 while (_vm.isRunning)
                     Thread.Sleep(100);
                 _vm.CallExternal(vm.main["start"]);
             }));
             th.Start(vm);
//
             //run testcases
             PyModule testmod = vm.NewModule("test");
             string testScript = Utils.LoadPythonLib("pytest");
             Stopwatch sw = new Stopwatch();
             sw.Start();
             var co = vm.Compile(testScript, "pytest.py", CompileMode.EXEC_MODE);
             sw.Stop();
             Debug.LogError("2000+ Compile Time: " + sw.ElapsedMilliseconds);

             th = new Thread((o =>
             {
                 VM _vm = (VM) o;
                 while (_vm.isRunning)
                     Thread.Sleep(10);
                 _vm.Exec(co, testmod);
             }));
             th.Start(vm);

            //run threading test
            const string source = @"
def is_prime(x):
  if x < 2:
    return False
  for i in range(2, x//2 + 1):
    if x % i == 0:
      return False
  return True

primes = [i for i in range(2, 1000) if is_prime(i)]
print(primes)
";
            Stopwatch sw2 = new Stopwatch();
            sw2.Start();
            var co2 = vm.Compile(source, "prime.py", CompileMode.EXEC_MODE);
            sw2.Stop();
            Debug.LogError("10 Compile Time: " + sw2.ElapsedMilliseconds);
            
            for (int i = 0; i < 10; i++)
            {
                th = new Thread((o =>
                {
                    VM _vm = (VM) o;
                    while (_vm.isRunning)
                        Thread.Sleep(100);
                    _vm.Exec(co2, testmod);
                }));
                th.Start(vm);
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            Debug.LogError(e.StackTrace);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // vm.Call(mod["update"]);


        if (Input.GetKeyDown(KeyCode.A))
        {
            th2 = new Thread((o =>
            {
                Profiler.BeginThreadProfiling("python working thread", "working thread2");
                VM _vm = (VM)o;
                while (_vm.isRunning)
                    Thread.Sleep(100);
                PyModule testmod1 = _vm.NewModule("test1");
                _vm.Exec(pyscript4, "pydemo4.py", CompileMode.EXEC_MODE, testmod1);
                Debug.Log(_vm.DeleteModule("test1"));
                Profiler.EndThreadProfiling();
            }));
            th2.Start(vm);
        }
    }

    private void OnDestroy()
    {
        if (th1 != null)
        {
            th1.Abort();
        }

        if (th2 != null)
        {
            th2.Abort();
        }
    }

    public void CsharpFuncCollector(string funcname, object[] args)
    {
        StringBuilder csfunc = new StringBuilder();
        csfunc.Append($"C# function name: {funcname}, args:[");
        // if (args.Length != 0)
        // {
        //     csfunc.Append(args[0]);
        //     for (int i = 1; i < args.Length; i++)
        //     {
        //         csfunc.Append(", ");
        //         csfunc.Append(args[i]);
        //     }
        // }
        csfunc.Append("]\n");
        Debug.LogWarning(csfunc.ToString());
    }

}
