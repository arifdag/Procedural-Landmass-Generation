using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    private ConcurrentQueue<ThreadInfo> dataQueue =
        new ConcurrentQueue<ThreadInfo>();

    private static ThreadedDataRequester instance;

    private void Awake()
    {
        instance = FindObjectOfType<ThreadedDataRequester>();
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate { instance.DataThread(generateData, callback); };
        Thread thread = new Thread(threadStart);
        thread.Start();
    }

    void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        dataQueue.Enqueue(new ThreadInfo(callback, data));
    }

    
    private void Update()
    {
        if (dataQueue.Count > 0)
        {
            for (int i = 0; i < dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = new ThreadInfo();
                dataQueue.TryDequeue(out threadInfo);
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    
    struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
