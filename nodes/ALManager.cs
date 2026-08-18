namespace godot_mono_openal;

[Tool]
public unsafe partial class ALManager : Node
{
    public static ALManager instance;
    public bool Initialised => ALDevice != null;

    public override void _EnterTree()
    {
        // Log to both - in case we're launched from vs2026 or from the Godot Editor
        OpenAL.Logger.Log = (message) =>
        {
            Console.WriteLine(message);
            GD.Print(message);
        };
        OpenAL.Logger.Error = (message) =>
        {
            Console.Error.WriteLine(message);
            GD.PushError(message);
        };

        // Ensure lists are up to date
        RefreshDeviceLists();
        NotifyPropertyListChanged();

        if (Engine.IsEditorHint())
            return;

        if (instance != null && instance != this)
        {
            LogWarning($"The ALManager node is already initialised. You can only have one ALManager node");
            QueueFree();
            return;
        }

        instance = this;

        if (!Initialised)
        {
            CreateDeviceAndContext();
        }
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            return;

        ALContext.Process();
        DisposeFinishedSources();

        if (MicrophoneEnabled)
            ALCaptureDevice?.Update();
    }

    public override void _ExitTree()
    {
        if (Initialised)
        {
            Debug.Assert(instance != null);
            Debug.Assert(ALDevice != null);
            Debug.Assert(ALContext != null);

            CancelLoadingAndDestroy();
            instance = null;
        }
    }

    float _masterVolume = 1;
    bool _hrtfEnabled = true;
    ALDistanceModel _distanceModel = ALDistanceModel.InverseDistance;
    float _metersPerUnit = 1;
    float _speedOfSound = 343;
    int _maximumMonoSources = 16;
    int _maximumStereoSources = 240;
    int _maximumAuxiliarySends = 1;
    int _sampleRate = 44100;
    int _outputDeviceIndex;
    int _inputDeviceIndex;
    bool _microphoneEnabled;
    int _microphoneThreshold;
    Vector3 _listenerPosition;
    Vector3 _listenerVelocity;
    float _listenerPitch;
    float _listenerYaw;
    bool _reverbOnly;

    [Export]
    public Vector3 ListenerPosition
    {
        get => _listenerPosition;
        set => UpdateProperty(ref _listenerPosition, value, SetListenerPosition);
    }

    [Export]
    public Vector3 ListenerVelocity
    {
        get => _listenerVelocity;
        set => UpdateProperty(ref _listenerVelocity, value, SetListenerVelocity);
    }

    [Export]
    public float ListenerPitch
    {
        get => _listenerPitch;
        set => UpdateProperty(ref _listenerPitch, value, SetListenerPitch);
    }

    [Export]
    public float ListenerYaw
    {
        get => _listenerYaw;
        set => UpdateProperty(ref _listenerYaw, value, SetListenerYaw);
    }

    [Export]
    public float MasterVolume
    {
        get => _masterVolume;
        set => UpdateProperty(ref _masterVolume, MathF.Max(0, value), SetListenerGain);
    }

    [Export]
    public bool HRTFEnabled
    {
        get => _hrtfEnabled;
        set => UpdateProperty(ref _hrtfEnabled, value, SetHRTFEnabled);
    }

    [Export]
    public ALDistanceModel DistanceModel
    {
        get => _distanceModel;
        set => UpdateProperty(ref _distanceModel, value, SetDistanceModel);
    }

    [Export]
    public float MetersPerUnit
    {
        get => _metersPerUnit;
        set => UpdateProperty(ref _metersPerUnit, MathF.Max(0, value), SetMetersPerUnit);
    }

    [Export]
    public float SpeedOfSound
    {
        get => _speedOfSound;
        set => UpdateProperty(ref _speedOfSound, MathF.Max(0, value), SetSpeedOfSound);
    }

    [Export]
    public bool ReverbOnly
    {
        get => _reverbOnly;
        set => UpdateProperty(ref _reverbOnly, value, SetReverbOnly);
    }

    [Export]
    public int MaximumMonoSources
    {
        get => _maximumMonoSources;
        set => UpdateProperty(ref _maximumMonoSources, Math.Max(0, value), (v) => CantChangeAtRuntime("MaximumMonoSources", _maximumMonoSources, v));
    }

    [Export]
    public int MaximumStereoSources
    {
        get => _maximumStereoSources;
        set => UpdateProperty(ref _maximumStereoSources, Math.Max(0, value), (v) => CantChangeAtRuntime("MaximumStereoSources", _maximumStereoSources, v));
    }

    [Export]
    public int MaximumAuxiliarySends
    {
        get => _maximumAuxiliarySends;
        set => UpdateProperty(ref _maximumAuxiliarySends, Math.Max(0, value), (v) => CantChangeAtRuntime("MaximumAuxiliarySends", _maximumAuxiliarySends, v));
    }

    [Export]
    public int SampleRate
    {
        get => _sampleRate;
        set => UpdateProperty(ref _sampleRate, Math.Max(0, value), SetSampleRate);
    }

    [Export]
    public bool MicrophoneEnabled
    {
        get => _microphoneEnabled;
        set => UpdateProperty(ref _microphoneEnabled, value, SetMicrophoneEnabled);
    }

    [Export]
    public int MicrophoneThreshold
    {
        get => _microphoneThreshold;
        set => UpdateProperty(ref _microphoneThreshold, Math.Max(0, value));
    }

    string _outputDeviceName;
    string _inputDeviceName;

    // Properties for internal access to device names
    string OutputDeviceName
    {
        get
        {
            return _outputDeviceName;
        }
        set
        {
            _outputDeviceName = value;
            RecreateDevice();
        }
    }

    string InputDeviceName
    {
        get
        {
            return _inputDeviceName;
        }
        set
        {
            _inputDeviceName = value;

            if (ALCaptureDevice != null)
                RecreateCaptureDevice();
        }
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        var properties = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (OutputDeviceList.Count == 0 || InputDeviceList.Count == 0)
            RefreshDeviceLists();

        properties.Add(new Godot.Collections.Dictionary
        {
            { "name", "OutputDeviceName" },
            { "type", (int)Variant.Type.String },
            { "usage", (int)(PropertyUsageFlags.Default) },
            { "hint", (int)PropertyHint.Enum },
            { "hint_string", OutputDeviceList.Count > 0 ? string.Join(",", OutputDeviceList) : "" }
        });

        properties.Add(new Godot.Collections.Dictionary
        {
            { "name", "InputDeviceName" },
            { "type", (int)Variant.Type.String },
            { "usage", (int)(PropertyUsageFlags.Default) },
            { "hint", (int)PropertyHint.Enum },
            { "hint_string", InputDeviceList.Count > 0 ? string.Join(",", InputDeviceList) : "" }
        });

        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (property == "InputDeviceName")
        {
            var value = _inputDeviceName ?? "";
            return value;
        }

        if (property == "OutputDeviceName")
        {
            var value = _outputDeviceName ?? "";
            return value;
        }

        return default;
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == "InputDeviceName")
        {
            _inputDeviceName = value.AsString();
            RecreateCaptureDevice();
            return true;
        }

        if (property == "OutputDeviceName")
        {
            _outputDeviceName = value.AsString();
            RecreateDevice();
            return true;
        }

        return false;
    }

    // samples is a short*, i.e. 2 bytes per sample
    public delegate void MicrophoneDataCallback(IntPtr samples, int sampleCount);
    public event MicrophoneDataCallback OnMicrophoneData;

    static void UpdateProperty<T>(ref T field, T value, Action<T> updateAction = null) where T : struct
    {
        if (!field.Equals(value))
        {
            field = value;
            updateAction?.Invoke(value);
        }
    }

    void CantChangeAtRuntime<T>(string property, T currentValue, T newValue)
    {
        if (Initialised)
            LogWarning($"The {property} property cannot be changed at runtime - please restart for changes to take effect. Current value: {currentValue}, new value: {newValue}");
    }
}