namespace godot_mono_openal;

public unsafe partial class ALManager
{
    // AL resources
    ALDevice ALDevice;
    ALContext ALContext;
    ALCaptureDevice ALCaptureDevice;

    // Device cache
    List<string> OutputDeviceList = [];
    List<string> InputDeviceList = [];

    // Intermediate listener data
    public Vector3 listenerPosition;
    public Vector3 listenerVelocity;
    public Vector3 listenerOrientation;
    public Vector3 listenerUp;

    // Keep track of all currently playing sources, so we can destroy them when they've finished playing
    List<ALSource> ActiveSources = [];

    // Callbacks for when the device is destroyed (e.g. when switching audio devices)
    HashSet<Action> OnDeviceDestroyedCallbacks = [];

    // Callbacks for when the device is recreated (e.g. after switching audio devices)
    HashSet<Action> OnDeviceRecreatedCallbacks = [];

    /// <summary>
    /// Register a callback to be invoked when the OpenAL device is destroyed.
    /// Use this to clean up any OpenAL resources (filters, effects, etc.)
    /// </summary>
    public void RegisterDeviceDestroyedCallback(Action callback)
    {
        if (callback == null)
            throw new ArgumentException("callback cannot be null");

        OnDeviceDestroyedCallbacks.Add(callback);
    }

    /// <summary>
    /// Unregister a previously registered device destroyed callback.
    /// </summary>
    public void UnregisterDeviceDestroyedCallback(Action callback)
    {
        if (callback == null)
            throw new ArgumentException("callback cannot be null");

        OnDeviceDestroyedCallbacks.Remove(callback);
    }

    /// <summary>
    /// Register a callback to be invoked when the OpenAL device is recreated.
    /// Use this to recreate any OpenAL resources (filters, effects, etc.) after the device is ready.
    /// </summary>
    public void RegisterDeviceRecreatedCallback(Action callback)
    {
        if (callback == null)
            throw new ArgumentException("callback cannot be null");

        OnDeviceRecreatedCallbacks.Add(callback);
    }

    /// <summary>
    /// Unregister a previously registered device recreated callback.
    /// </summary>
    public void UnregisterDeviceRecreatedCallback(Action callback)
    {
        if (callback == null)
            throw new ArgumentException("callback cannot be null");

        OnDeviceRecreatedCallbacks.Remove(callback);
    }
}
