namespace godot_mono_openal;

public unsafe partial class ALManager
{
    // Matches va_device_name.h's DEFAULT_DEVICE_LABEL in the native Godot plugin - the
    // audio/vaudio/output_device Project Setting stores this label rather than "" (a strict
    // PROPERTY_HINT_ENUM dropdown must always show the current value as one of its own entries),
    // translated back to "" ("driver default") only when read here.
    const string DefaultDeviceLabel = "System Default";

    int _maximumAuxiliarySends;
    int _sampleRate;
    bool _hrtfEnabled;
    int _maximumMonoSources;
    int _maximumStereoSources;

    // Reads audio/vaudio/output_device, audio/vaudio/max_reverb_sends, audio/vaudio/sample_rate,
    // audio/vaudio/hrtf_enabled, audio/vaudio/max_mono_sources and audio/vaudio/max_stereo_sources
    // once - matches native's read_settings_from_project_settings() in al_manager.cpp. plugin.gd's
    // _register_project_settings() registers these (with defaults) before the singleton is
    // created, so every setting already exists here.
    void ReadSettingsFromProjectSettings()
    {
        var deviceNameSetting = ProjectSettings.GetSetting("audio/vaudio/output_device").AsString();
        _outputDeviceName = deviceNameSetting == DefaultDeviceLabel ? "" : deviceNameSetting;

        _maximumAuxiliarySends = ProjectSettings.GetSetting("audio/vaudio/max_reverb_sends").AsInt32();
        _sampleRate = ProjectSettings.GetSetting("audio/vaudio/sample_rate").AsInt32();
        _hrtfEnabled = ProjectSettings.GetSetting("audio/vaudio/hrtf_enabled").AsBool();
        _maximumMonoSources = ProjectSettings.GetSetting("audio/vaudio/max_mono_sources").AsInt32();
        _maximumStereoSources = ProjectSettings.GetSetting("audio/vaudio/max_stereo_sources").AsInt32();
    }

    void CreateDeviceAndContext()
    {
        // Shouldn't be initialising in the editor
        Debug.Assert(!Engine.IsEditorHint());

        Debug.Assert(ALContext == null);
        Debug.Assert(ALDevice == null);

        ReadSettingsFromProjectSettings();

        // Create an OpenAL device - null (not "") means "use the driver default": the P/Invoke
        // marshals a C# null to a native NULL, which alcOpenDevice requires for its own "driver
        // default" behaviour, whereas "" marshals to a valid pointer to an empty C string and
        // fails - matches native's `device_name.empty() ? nullptr : device_name.c_str()`.
        ALDevice = new(string.IsNullOrEmpty(_outputDeviceName) ? null : _outputDeviceName);


        // Create an OpenAL context
        var settings = new ALContextSettings()
        {
            HRTFEnabled = _hrtfEnabled,
            HRTFID = 0,
            SampleRate = _sampleRate,
            MaximumAuxiliarySends = _maximumAuxiliarySends,
            MaximumMonoSources = _maximumMonoSources,
            MaximumStereoSources = _maximumStereoSources,
            LogFunction = GD.PushWarning,
        };

        ALContext = new(ALDevice, settings);


        // Set initial properties
        SetMetersPerUnit(MetersPerUnit);
        SetSpeedOfSound(SpeedOfSound);
        SetListenerGain(MasterVolume);
        SetDistanceModel(DistanceModel);
    }

    void RecreateDevice()
    {
        // Don't create OpenAL devices when changing properties in the editor
        if (!Initialised)
            return;

        // Prefer ALDevice.Reopen (ALC_SOFT_reopen_device) - it redirects the existing ALC
        // device/context to the new output device in place, so every existing AL object (sources,
        // buffers, filters, effects) stays valid and DecodedStreams doesn't need re-decoding.
        // Reopen itself returns false (no exception) if the extension isn't present on this
        // device, in which case fall back to the old CancelLoadingAndDestroy()+
        // CreateDeviceAndContext() teardown/recreate below, which invalidates all of those and
        // fires the device destroyed/recreated callbacks. Matches native's ALManager::reinitialize
        // (al_manager.cpp, vaudio-godot-native-openal-3d-source).
        ReadSettingsFromProjectSettings();

        var attribs = ALContext.GetAttribs(new()
        {
            HRTFEnabled = _hrtfEnabled,
            HRTFID = 0,
            SampleRate = _sampleRate,
            MaximumAuxiliarySends = _maximumAuxiliarySends,
            MaximumMonoSources = _maximumMonoSources,
            MaximumStereoSources = _maximumStereoSources,
        });

        if (ALDevice.Reopen(string.IsNullOrEmpty(_outputDeviceName) ? null : _outputDeviceName, attribs))
            return;

        CancelLoadingAndDestroy();
        CreateDeviceAndContext();

        // Invoke device recreated callbacks (e.g. for recreating reverb effects)
        foreach (var callback in OnDeviceRecreatedCallbacks)
            callback.Invoke();
    }

    void RefreshDeviceLists()
    {
        OutputDeviceList = AL.GetStringList(IntPtr.Zero, AL.ALC_ALL_DEVICES_SPECIFIER);

        // Rebuild audio/vaudio/output_device's PROPERTY_HINT_ENUM hint_string now that the real
        // device list is known - matches native's refresh_output_device_hint(). Registered with
        // just DefaultDeviceLabel by plugin.gd's _register_project_settings() until this runs.
        var devices = new List<string> { DefaultDeviceLabel };
        devices.AddRange(OutputDeviceList);

        ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
        {
            { "name", "audio/vaudio/output_device" },
            { "type", (int)Variant.Type.String },
            { "hint", (int)PropertyHint.Enum },
            { "hint_string", string.Join(",", devices) }
        });
    }
}