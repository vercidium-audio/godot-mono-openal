namespace godot_mono_openal;

public unsafe partial class ALManager
{
    void CreateDeviceAndContext()
    {
        // Shouldn't be initialising in the editor
        Debug.Assert(!Engine.IsEditorHint());

        Debug.Assert(ALContext == null);
        Debug.Assert(ALDevice == null);

        // Early exit - no speakers or headphones
        if (OutputDeviceName == "")
            return;

        // Create an OpenAL device
        ALDevice = new(OutputDeviceName);


        // Create an OpenAL context
        var settings = new ALContextSettings()
        {
            HRTFEnabled = HRTFEnabled,
            HRTFID = 0,
            SampleRate = SampleRate,
            MaximumAuxiliarySends = MaximumAuxiliarySends,
            MaximumMonoSources = MaximumMonoSources,
            MaximumStereoSources = MaximumStereoSources,
            LogFunction = GD.PushWarning,
        };

        ALContext = new(ALDevice, settings);


        // Set initial properties
        SetMetersPerUnit(MetersPerUnit);
        SetSpeedOfSound(SpeedOfSound);
        SetListenerGain(MasterVolume);
        SetDistanceModel(DistanceModel);


        // Create an OpenAL capture device
        if (MicrophoneEnabled)
            InitialiseCaptureDevice();
    }

    void RecreateDevice()
    {
        // Don't create OpenAL devices when changing properties in the editor
        if (!Initialised)
            return;

        CancelLoadingAndDestroy();
        CreateDeviceAndContext();

        // Invoke device recreated callbacks (e.g. for recreating reverb effects)
        foreach (var callback in OnDeviceRecreatedCallbacks)
            callback.Invoke();
    }

    void RefreshDeviceLists()
    {
        InputDeviceList = AL.GetStringList(IntPtr.Zero, AL.ALC_CAPTURE_DEVICE_SPECIFIER);

        if (InputDeviceList.Count == 0)
        {
            InputDeviceName = "";
        }
        else
        {
            // Auto-select for the first time
            if (string.IsNullOrEmpty(InputDeviceName))
            {
                InputDeviceName = InputDeviceList[0];
            }
            // Handle disconnect - switch to a different device
            else if (!InputDeviceList.Contains(InputDeviceName))
            {
                LogWarning($"Input device '{InputDeviceName}' disconnected. Switching to '{InputDeviceList[0]}'");
                InputDeviceName = InputDeviceList[0];
            }
        }

        OutputDeviceList = AL.GetStringList(IntPtr.Zero, AL.ALC_ALL_DEVICES_SPECIFIER);

        if (OutputDeviceList.Count == 0)
        {
            OutputDeviceName = "";
        }
        else
        {
            // Auto-select for the first time
            if (string.IsNullOrEmpty(OutputDeviceName))
            {
                OutputDeviceName = OutputDeviceList[0];
            }
            // Handle disconnect - switch to a different device
            else if (!OutputDeviceList.Contains(OutputDeviceName))
            {
                LogWarning($"Output device '{OutputDeviceName}' disconnected. Switching to '{OutputDeviceList[0]}'");
                OutputDeviceName = OutputDeviceList[0];
            }
        }
    }
}