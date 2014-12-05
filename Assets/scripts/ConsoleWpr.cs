using UnityEngine;
using System.Collections;
using System.Threading;

/// <summary>
/// author: nug700
/// A thread safe mod api wrapper for the Console class in 
/// Assembly-CSharp-firstpass so modders can access the 
/// console with only a reference to Assembly-CSharp.
/// </summary>

public static class ConsoleWpr
{

    [LuaFunc("", "Log", "Print text to console.", "input")]
    public static void Log(object message)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.Log(message);
        });
    }

    [LuaFunc("", "LogFormat", "Print text to console.", "input", "params")]
    public static void LogFormat(string format, params object[] args)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.LogFormat(format, args);
        });
    }

    public static void Log(object message, DConsole.MessageType messageType)
    {
        DConsole.Log(message, messageType);
    }

    public static void Log(object message, Color displayColor)
    {
        DConsole.Log(message, displayColor);
    }

    public static void Log(object message, DConsole.MessageType messageType, Color displayColor)
    {
        DConsole.Log(message, messageType, displayColor);
    }

    [LuaFunc("", "LogWarning", "Print warning to console.", "input")]
    public static void LogWarning(object message)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.LogWarning(message);
        });
    }

    [LuaFunc("", "LogError", "Print error to console.", "input")]
    public static void LogError(object message)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.LogError(message);
        });
    }

    public static void LogError(System.Exception message)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.LogError(message);
        });
    }

    [LuaFunc("", "LogSystem", "Print text to console in system format.", "input")]
    public static void LogSystem(object message)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.LogSystem(message);
        });
    }

    [LuaFunc("", "LogDebug", "Print text to console with debug format.", "input")]
    public static void LogDebug(object message)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.LogDebug(message);
        });
    }

    [LuaFunc("", "LogInfo", "Print text to console with info format.", "input")]
    public static void LogInfo(object message)
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.LogInfo(message);
        });
    }

    [LuaFunc("", "Clear", "Clears the console.")]
    public static void Clear()
    {
        Loom.QueueOnMainThread(() =>
        {
            DConsole.Clear();
        });
    }

    [LuaFunc("", "Execute", "Executes a command.", "command")]
    public static string Execute(string commandString)
    {
        string retVal = string.Empty; 
        ManualResetEvent resetEvent = new ManualResetEvent(false);
        resetEvent.Reset();
        Loom.QueueOnMainThread(() =>
        {
            retVal = DConsole.Execute(commandString);
            resetEvent.Set();
        });
        resetEvent.WaitOne();
        return retVal;
    }

    [LuaFunc("", "Commands", "Lists all available commands.")]
    public static string[] Commands()
    {
        string[] retVal = null;
        ManualResetEvent resetEvent = new ManualResetEvent(false);
        resetEvent.Reset();
        Loom.QueueOnMainThread(() =>
        {
            retVal = DConsole.Commands();
            resetEvent.Set();
        });
        resetEvent.WaitOne();
        return retVal;
    }
}



