global using static godot_mono_openal.Logger;

global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using OpenAL;
global using OpenAL.managed;
global using Godot;

namespace godot_mono_openal;

public static class Helper
{
    public static Vector3 PitchYawToVector3(float pitch, float yaw)
    {
        Vector3 direction = new()
        {
            X = Mathf.Cos(pitch) * Mathf.Sin(yaw),
            Y = -Mathf.Sin(pitch),
            Z = Mathf.Cos(pitch) * Mathf.Cos(yaw)
        };

        return direction.Normalized();
    }
}

internal static class Logger
{
    internal static void Log(string message)
    {
        var prefixed = $"[godot-mono-openal] {message}";

        Console.WriteLine(prefixed);
        GD.Print(prefixed);
    }

    internal static void LogWarning(string message)
    {
        var prefixed = $"[godot-mono-openal] {message}";

        Console.WriteLine(prefixed);
        GD.PushWarning(prefixed);
    }

    internal static void LogError(string message)
    {
        var prefixed = $"[godot-mono-openal] {message}";

        Console.Error.WriteLine(prefixed);
        GD.PushError(prefixed);
    }
}