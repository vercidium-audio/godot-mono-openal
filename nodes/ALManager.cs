namespace godot_mono_openal;

[Tool]
public unsafe partial class ALManager : Node
{
    public static ALManager instance;

    // Lazily creates the singleton on first access, rather than relying on plugin.gd's
    // _add_singleton() to have already added one - EditorPlugin/tree timing during startup isn't
    // reliable enough to guarantee that's happened by the time game code (e.g. a VAWorld node)
    // checks GodotOpenALEnabled from its own _EnterTree().
    //
    // Initialisation happens directly here (setting `instance`, CreateDeviceAndContext) rather
    // than deferring to _EnterTree - AddChild() does NOT call _EnterTree() synchronously when
    // called reentrantly from within the scene tree's own tree-enter pass (which is exactly the
    // case here, since this is normally reached from another node's _EnterTree()); Godot queues
    // the notification instead, so waiting on it would leave `instance` null and cause infinite
    // recreation. _EnterTree() below is now just a safety net for the ALManager plugin.gd adds
    // eagerly in the editor.
    //
    // Never lazily creates one in the editor - editor code should only ever see an instance if
    // plugin.gd's own _add_singleton() created one for editor-side tooling (device list, etc).
    public static ALManager Instance
    {
        get
        {
            if (instance == null && !Engine.IsEditorHint())
            {
                instance = new ALManager { Name = nameof(ALManager) };
                ((SceneTree)Engine.GetMainLoop()).Root.AddChild(instance);

                instance.CreateDeviceAndContext();
            }

            return instance;
        }
    }

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

        // Already fully set up via Instance (the common path at game runtime) - nothing to do.
        if (instance == this)
            return;

        if (instance != null)
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
    ALDistanceModel _distanceModel = ALDistanceModel.InverseDistance;
    float _metersPerUnit = 1;
    float _speedOfSound = 343;
    int _outputDeviceIndex;
    Vector3 _listenerPosition;
    Vector3 _listenerVelocity;
    float _listenerPitch;
    float _listenerYaw;
    bool _reverbOnly;

    // MasterVolume/DistanceModel/MetersPerUnit/SpeedOfSound/ReverbOnly/ListenerPosition/
    // ListenerVelocity/ListenerPitch/ListenerYaw are runtime-API-only (no inspector UI),
    // matching native's shape - call the Set* methods below directly from code.

    public Vector3 ListenerPosition
    {
        get => _listenerPosition;
        set => UpdateProperty(ref _listenerPosition, value, SetListenerPosition);
    }

    public Vector3 ListenerVelocity
    {
        get => _listenerVelocity;
        set => UpdateProperty(ref _listenerVelocity, value, SetListenerVelocity);
    }

    public float ListenerPitch
    {
        get => _listenerPitch;
        set => UpdateProperty(ref _listenerPitch, value, SetListenerPitch);
    }

    public float ListenerYaw
    {
        get => _listenerYaw;
        set => UpdateProperty(ref _listenerYaw, value, SetListenerYaw);
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set => UpdateProperty(ref _masterVolume, MathF.Max(0, value), SetListenerGain);
    }

    public ALDistanceModel DistanceModel
    {
        get => _distanceModel;
        set => UpdateProperty(ref _distanceModel, value, SetDistanceModel);
    }

    public float MetersPerUnit
    {
        get => _metersPerUnit;
        set => UpdateProperty(ref _metersPerUnit, MathF.Max(0, value), SetMetersPerUnit);
    }

    public float SpeedOfSound
    {
        get => _speedOfSound;
        set => UpdateProperty(ref _speedOfSound, MathF.Max(0, value), SetSpeedOfSound);
    }

    public bool ReverbOnly
    {
        get => _reverbOnly;
        set => UpdateProperty(ref _reverbOnly, value, SetReverbOnly);
    }

    // MaximumAuxiliarySends, SampleRate, HRTFEnabled, MaximumMonoSources and MaximumStereoSources
    // are read once from Project Settings (audio/vaudio/*) during CreateDeviceAndContext() - see
    // ALManagerDevice.cs - matching native's read_settings_from_project_settings(); they're not
    // settable at runtime there either, since ALManager's only bound device-switching method
    // (set_output_device) reuses whatever these were at initialize() time.

    // Read once from Project Settings (audio/vaudio/output_device) during
    // CreateDeviceAndContext() - see ALManagerDevice.cs - no longer an inspector-editable
    // property, matching native's output device now only being configurable via Project
    // Settings (or ALManager.SetOutputDevice at runtime).
    string _outputDeviceName;

    static void UpdateProperty<T>(ref T field, T value, Action<T> updateAction = null) where T : struct
    {
        if (!field.Equals(value))
        {
            field = value;
            updateAction?.Invoke(value);
        }
    }

}