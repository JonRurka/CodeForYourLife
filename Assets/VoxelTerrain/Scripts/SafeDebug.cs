using UnityEngine;
using System.Collections;

public static class SafeDebug {

    public static void Log(object message) {
        Loom.QueueOnMainThread(() => {
            Debug.Log(message);
        });
    }

    public static void LogWarning(object message) {
        Loom.QueueOnMainThread(() => {
            Debug.LogWarning(message);
        });
    }

    public static void LogError(object message) {
        Loom.QueueOnMainThread(() => {
            Debug.LogError(message);
        });
    }

    public static void LogException(System.Exception message) {
        Loom.QueueOnMainThread(() => {
            Debug.LogException(message);
        });
    }
}
