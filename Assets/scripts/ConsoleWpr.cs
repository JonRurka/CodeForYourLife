using UnityEngine;
using System.Collections;

/// <summary>
/// author: nug700
/// A thread safe mod api wrapper for the CoreConsole class.
/// </summary>
namespace modCore {
    public static class ConsoleWpr {

        public static void Log(object message) {
            Loom.QueueOnMainThread(() => {
                DConsole.Log(message);
            });
        }

        public static void LogFormat(string format, params object[] args) {
            Loom.QueueOnMainThread(() => {
                DConsole.LogFormat(format, args);
            });
        }

        public static void Log(object message, DConsole.MessageType messageType) {
            DConsole.Log(message, messageType);
        }

        public static void Log(object message, Color displayColor) {
            DConsole.Log(message, displayColor);
        }

        public static void Log(object message, DConsole.MessageType messageType, Color displayColor) {
            DConsole.Log(message, messageType, displayColor);
        }

        public static void LogWarning(object message) {
            Loom.QueueOnMainThread(() => {
                DConsole.LogWarning(message);
            });
        }

        public static void LogError(object message) {
            Loom.QueueOnMainThread(() => {
                DConsole.LogError(message);
            });
        }

        public static void LogError(System.Exception message) {
            Loom.QueueOnMainThread(() => {
                DConsole.LogError(message);
            });
        }

        public static void LogSystem(object message) {
            Loom.QueueOnMainThread(() => {
                DConsole.LogSystem(message);
            });
        }

        public static void LogDebug(object message) {
            Loom.QueueOnMainThread(() => {
                DConsole.LogDebug(message);
            });
        }

        public static void LogInfo(object message) {
            Loom.QueueOnMainThread(() => {
                DConsole.LogInfo(message);
            });
        }

        public static void Clear() {
            Loom.QueueOnMainThread(() => {
                DConsole.Clear();
            });
        }

        public static string Execute(string commandString) {
            string retVal = string.Empty;
            Loom.QueueOnMainThread(() => {
                retVal = DConsole.Execute(commandString);
            });
            return retVal;
        }

        public static string[] Commands() {
            string[] retVal = null;
            Loom.QueueOnMainThread(() => {
                retVal = DConsole.Commands();
            });
            return retVal;
        }
    }
}
