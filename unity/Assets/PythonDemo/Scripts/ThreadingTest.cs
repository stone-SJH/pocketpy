using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Python;
using UnityEngine;

/*
 * About Multi-threading
 *
 * GIL
 *
 *
 * c# threading(Multi-VM-instance)
 *
 *
 *
 */
public class ThreadingTest : MonoBehaviour
{

    private volatile VM VM_thread1;
    private volatile VM VM_thread2;
    private volatile VM VM_thread3;
    private volatile VM VM_thread4;

    private Thread th1;
    private Thread th2;

    // Start is called before the first frame update
    void Start()
    {
        VM_thread1 = new VM();
        VM_thread2 = new VM();
        VM_thread3 = new VM();
        VM_thread4 = new VM();


        th1 = new Thread(new ParameterizedThreadStart(RunPy));
        // vm.Exec(code);
        // vm.Exec(code);
        // vm.Exec(code);
        // vm.Exec(code);
        th1.Start(VM_thread1);
        // th2.Start(VM_thread1);
        // th3.Start(VM_thread3);
        // th4.Start(VM_thread4);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            VM_thread1.KeyboardInterrupt();

            th1 = new Thread(new ParameterizedThreadStart(RunPy));
            th1.Start(VM_thread1);
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

    static void RunPy(object obj)
    {
        VM vm = (VM) obj;
        const string source = @"
def is_prime(x):
  if x < 2:
    return False
  for i in range(2, x//2 + 1):
    if x % i == 0:
      return False
  return True

sum = 0

for i in range(2, 1000):
    if is_prime(i):
        sum = sum + i
        print(sum)

#primes = [i for i in range(2, 100000) if is_prime(i)]
#print(primes)
";
        CodeObject code = vm.Compile(source, "main.py", CompileMode.EXEC_MODE);
        vm.Exec(code);
        Debug.Log(vm.main["sum"]);
    }
}
