using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject Character;

    // Start is called before the first frame update
    void Start()
    {
        //MoveForward(Character, 100, 1);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TestDebugOutput()
    {
        Debug.Log("Debug output from c# Controller");
    }

    public void MoveForward(GameObject go, float dis, float speed)
    {
        float waitSeconds = dis / speed;
        // Debug.LogError("PythonWorkingThread: " + Thread.CurrentThread.ManagedThreadId);
        StartMoveForward(go, Vector3.forward, speed, waitSeconds).AsTask().GetAwaiter().GetResult();
        // Debug.LogError("PythonWorkingThread: " + Thread.CurrentThread.ManagedThreadId);
    }

    public void Say(int arg)
    {
        Debug.Log("Say 1i: " + arg);
    }

    public void Say(string arg)
    {
        Debug.Log("Say 1s: " + arg);
    }

    public void Say(params object[] args)
    {
        foreach (var arg in args)
        {
            Debug.Log("Say: " + arg);
        }
    }

    async UniTask StartMoveForward(GameObject go, Vector3 targetPosition, float speed, float waitSeconds)
    {
        await UniTask.SwitchToMainThread();
        // Debug.LogError("UnityAPIWorkingThread: " + Thread.CurrentThread.ManagedThreadId);
        // Loop until we're within Unity's vector tolerance of our target.
        while(go.transform.position != targetPosition) {

            // Move one step toward the target at our given speed.
            go.transform.position = Vector3.MoveTowards(
                go.transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            // Wait one frame then resume the loop.
            await UniTask.Yield();
        }

        // We have arrived. Ensure we hit it exactly.
        go.transform.position = targetPosition;
        await UniTask.SwitchToThreadPool();
        // Debug.LogError("PythonWorkingThread: " + Thread.CurrentThread.ManagedThreadId);
    }
}
