using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

public class Loom : MonoBehaviour {
    public static int maxThreads = 8;
    static int numThreads;

    private static Loom _current;
    private int _count;
    public static Loom Current {
        get {
            Initialize();
            return _current;
        }
    }

    void Awake() {
        _current = this;
        initialized = true;
    }

    static bool initialized;

    static void Initialize() {
        if (!initialized) {

            if (!Application.isPlaying)
                return;
            initialized = true;
            var g = new GameObject("Loom");
            _current = g.AddComponent<Loom>();
        }

    }

    private List<Action> _actions = new List<Action>();
    private List<Action> _spread = new List<Action>();
    private Dictionary<string, AsyncRunner> _AsynAction = new Dictionary<string, AsyncRunner>();

    public struct DelayedQueueItem {
        public float time;
        public Action action;
    }

    private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

    List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

    public static void QueueOnMainThread(Action action, bool spreadOut = false) {
        QueueOnMainThread(action, 0f, spreadOut);
    }

    public static void QueueOnMainThread(Action action, float time, bool spreadOut = false) {
        if (time != 0) {
            if (Current._delayed != null) {
                lock (Current._delayed) {
                    Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action });
                }
            }
        }
        else {
            if (spreadOut) {
                lock (Current._spread) {
                    Current._spread.Add(action);
                }
            }
            else {
                if (Current._actions != null) {
                    lock (Current._actions) {
                        Current._actions.Add(action);
                    }
                }
            }
        }
    }

    public static void AddAsyncThread(string thread) {
        if (Current._AsynAction != null){
            lock (Current._AsynAction) {
                try {
                    if (!Current._AsynAction.ContainsKey(thread)) {
                        //ConsoleWpr.LogDebug("Created thread: " + thread);
                        AsyncRunner _runner = new AsyncRunner(thread);
                        Current._AsynAction.Add(thread, _runner);
                    }
                }
                catch (Exception e) {
                    //ConsoleWpr.LogError("\nMessage: " + e.Message + "\nFunction: AddAsyncThread\nThread: " + thread);
                }
            }
        }
    }

    public static void QueueAsyncTask(string thread, Action e) {
        lock (Current._AsynAction) {
            try {
                if (Current._AsynAction.ContainsKey(thread)) {
                    Current._AsynAction[thread].AddAsyncTask(e);
                }
                //else
                    //ConsoleWpr.LogError("failed to locate thread " + thread);
            }
            catch (Exception ex) {
                //ConsoleWpr.LogError("\nMessage: " + ex.Message + "\nFunction: QueueAsyncTask\nThread: " + thread);
            }
        }
    }

    public static Thread GetThreadRef(string thread) {
        lock (Current._AsynAction) {
            if (Current._AsynAction.ContainsKey(thread)) {
                return Current._AsynAction[thread].thread;
            }
            else
                return null;
        }
    }

    public static string GetThreadName(Thread thread) {
        foreach (string runner in Current._AsynAction.Keys) {
            if (Current._AsynAction[runner].thread.Equals(thread)) {
                return runner;
            }
        }
        return null;
    }

    public static bool ThreadExists(string thread) {
        return Current._AsynAction.ContainsKey(thread);
    }

    public static Thread RunAsync(Action a) {
        Initialize();
        while (numThreads >= maxThreads) {
            Thread.Sleep(1);
        }
        Interlocked.Increment(ref numThreads);
        ThreadPool.QueueUserWorkItem(RunAction, a);
        a = null;
        return null;
    }

    private static void RunAction(object action) {
        try {
            ((Action)action)();
        }
        catch {
        }
        finally {
            Interlocked.Decrement(ref numThreads);
        }

    }

    void OnDisable() {
        if (_current == this) {

            _current = null;
        }
    }

    List<Action> _currentActions = new List<Action>();

    List<Action> _spreadOutActions = new List<Action>();

    int currentSetSize = 0;
    int currentSelection = 0;
    int tickCounter = 0;

    // Update is called once per frame
    void Update() {
        lock (_actions) {
            _currentActions.Clear();
            _currentActions.AddRange(_actions);
            _actions.Clear();
        }
        for (int i = 0; i < _currentActions.Count; i++) {
            _currentActions[i]();
            _currentActions[i] = null;
        }

        if (Input.GetKey(KeyCode.Alpha5)) {
            foreach (string thread in _AsynAction.Keys) {
                if (_AsynAction[thread].Actions.Count > 0) {
                    //Console.LogDebug(_AsynAction[thread].threadName + ": functions detected: " + _AsynAction[thread].Actions.Count + ", " + _AsynAction[thread]._currentActions.Count);
                }
            }
        }

        /*if (_spreadOutActions.Count == 0 && _spread.Count > 0) {
            lock (_spread) {
                _spreadOutActions.Clear();
                _spreadOutActions.AddRange(_spread);
                _spread.Clear();
                StartCoroutine(SpreadOut());
            }
        }*/

        if (_spreadOutActions.Count == 0 && _spread.Count > 0) {
            lock (_spread) {
                tickCounter = 0;
                currentSelection = 0;
                currentSetSize = _spread.Count;
                _spreadOutActions.Clear();
                _spreadOutActions.AddRange(_spread);
                _spread.Clear();
            }
        }
        else if (_spreadOutActions.Count > 0) {
            tickCounter++;
            if (currentSelection < currentSetSize && tickCounter % 2 == 0) {
                _spreadOutActions[currentSelection]();
                _spreadOutActions[currentSelection] = null;
                currentSelection++;
            }
        }

        lock (_delayed) {
            _currentDelayed.Clear();
            _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
            foreach (var item in _currentDelayed)
                _delayed.Remove(item);
        }
        foreach (var delayed in _currentDelayed) {
            delayed.action();
        }
    }

    void OnApplicationQuit() {
        if (_actions != null) {
            _actions.Clear();
            _actions = null;
        }
        if (_AsynAction != null) {
            foreach (AsyncRunner runner in _AsynAction.Values) {
                runner.Dispose();
            }
            _AsynAction.Clear();
            _AsynAction = null;
        }
        if (_currentActions != null) {
            _currentActions.Clear();
            _currentActions = null;
        }
        if (_delayed != null) {
            _delayed.Clear();
            _delayed = null;
        }
        if (_currentDelayed != null) {
            _currentDelayed.Clear();
            _currentDelayed = null;
        }
    }

    IEnumerator SpreadOut() {
        for (int i = 0; i < _spreadOutActions.Count; i++) {
            _spreadOutActions[i]();
            _spreadOutActions[i] = null;
            yield return new WaitForEndOfFrame();
        }
        _spreadOutActions.Clear();
    }
}
