using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    private readonly ConcurrentQueue<ThreadInfo> _dataQueue =
        new ConcurrentQueue<ThreadInfo>();

    private static ThreadedDataRequester _instance;

    private void Awake()
    {
        _instance = FindObjectOfType<ThreadedDataRequester>();
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        void ThreadStart()
        {
            _instance.DataThread(generateData, callback);
        }

        Thread thread = new Thread(ThreadStart);
        thread.Start();
    }

    public static void RequestThread(Action requestThread, Action callback)
    {
        void ThreadStart()
        {
            requestThread?.Invoke();
            _instance._dataQueue.Enqueue(new ThreadInfo(() => callback(), null));
        }

        Thread thread = new Thread(ThreadStart);
        thread.Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        _dataQueue.Enqueue(new ThreadInfo(() => callback(data), data));
    }

    private void Update()
    {
        while (_dataQueue.TryDequeue(out var threadInfo))
        {
            threadInfo.Invoke();
        }
    }

    private class ThreadInfo
    {
        private readonly Action _callback;

        public ThreadInfo(Action callback, object parameter)
        {
            _callback = callback;
        }

        public void Invoke()
        {
            _callback();
        }
    }
}