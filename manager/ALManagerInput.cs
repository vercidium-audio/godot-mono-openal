namespace godot_mono_openal;

public unsafe partial class ALManager
{
    void RecreateCaptureDevice()
    {
        Debug.Assert(MicrophoneEnabled);
        Debug.Assert(ALCaptureDevice != null);

        CloseCaptureDevice();
        InitialiseCaptureDevice();
    }

    void CloseCaptureDevice()
    {
        ALCaptureDevice?.Close();
        ALCaptureDevice = null;
    }

    void InitialiseCaptureDevice()
    {
        Debug.Assert(ALCaptureDevice == null);

        // No microphones available
        if (InputDeviceList.Count == 0)
        {
            LogWarning($"Unable to initialise input device as there are no devices available");
            return;
        }

        ALCaptureDevice = new(new()
        {
            SampleRate = _sampleRate,
            DeviceName = InputDeviceName,
            DataCallback = (a, b) => OnMicrophoneData?.Invoke(a, b),
            LogCallback = GD.PushWarning,
        });

        ALCaptureDevice.CaptureStart();
    }
}
