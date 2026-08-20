namespace godot_mono_openal;

public unsafe partial class ALManager
{
    void DestroyAllAudioSources(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is ALSource source)
                source.OnDeviceDestroyed();

            DestroyAllAudioSources(child);
        }
    }

    public void DestroyAll()
    {
        // Sanity check
        if (ALDevice == null || ALContext == null)
        {
            Debug.Assert(false);
            return;
        }

        // Delete sources before effects
        DestroyAllAudioSources(GetTree().Root);

        // Invoke device destroyed callbacks (e.g. for cleaning up reverb effects)
        foreach (var callback in OnDeviceDestroyedCallbacks)
            callback.Invoke();

        // Delete microphone device
        CloseCaptureDevice();
        ALCaptureDevice = null;

        // Delete context
        AL.MakeContextCurrent(IntPtr.Zero);
        ALContext.Destroy();
        ALContext = null;

        // Delete device
        ALDevice.Close();
        ALDevice = null;
    }

    public void CancelLoadingAndDestroy()
    {
        // Tell the background sound-loading threads to stop loading
        ALBuffer.CancelLoadingSounds = true;

        // Wait for all threads to finish
        foreach (var buffer in DecodedStreams.Values)
            buffer.WaitForTask();

        DecodedStreams.Clear();
        ALBuffer.CancelLoadingSounds = false;

        // Delete everything - unfortunately we can't copy data from buffers in one OpenAL context to another. We need to re-decode every AudioStream :(
        // RecreateDevice() (ALManagerDevice.cs) only falls back to this when ALDevice.Reopen
        // (ALC_SOFT_reopen_device) isn't available on the current device.
        DestroyAll();
    }
}