namespace godot_openal;

public partial class ALSource3D : Node3D
{
    float _volume = 1;
    float _pitch = 1;
    float _maxDistance = 100;
    float _referenceDistance = 8;
    bool _looping = false;
    bool _relative = false;
    string[] _streams = [];
    bool _playbackNoRepeat = true;

    void UpdateProperty<T>(ref T field, T value, Action<T, ALSource> updateAction) where T : struct
    {
        if (!field.Equals(value))
        {
            field = value;

            if (updateAction != null)
                foreach (var s in sources)
                    updateAction.Invoke(value, s);
        }
    }

    /// <summary>The volume of the sound</summary>
    [Export(PropertyHint.Range, "0.0,10.0")]
    public float Volume
    {
        get => _volume;
        set => UpdateProperty(ref _volume, MathF.Max(0, value), (v, source) => source.SetGain(v));
    }

    /// <summary>The pitch of the sound</summary>
    [Export(PropertyHint.Range, "0.0,10.0")]
    public float Pitch
    {
        get => _pitch;
        set => UpdateProperty(ref _pitch, MathF.Max(0, value), (v, source) => source.SetPitch(v));
    }

    /// <summary>The max distance that the sound can be heard at. Also affected by the falloff model in <see cref="ALManager"/></summary>
    [Export]
    public float MaxDistance
    {
        get => _maxDistance;
        set => UpdateProperty(ref _maxDistance, MathF.Max(0, value), (v, source) => source.SetMaxDistance(v));
    }

    /// <summary>The distance that sound volume falloff starts at</summary>
    [Export]
    public float ReferenceDistance
    {
        get => _referenceDistance;
        set => UpdateProperty(ref _referenceDistance, MathF.Max(0, value), (v, source) => source.SetReferenceDistance(v));
    }

    /// <summary>Whether the sound plays indefinitely on loop</summary>
    [Export]
    public bool Looping
    {
        get => _looping;
        set => UpdateProperty(ref _looping, value, (v, source) => source.SetLooping(v));
    }

    /// <summary>Whether the sound is spatialised or relative to the listener. Set to true for music, ambience and sounds your own character makes.</summary>
    [Export]
    public bool Relative
    {
        get => _relative;
        set => UpdateProperty(ref _relative, value, (v, source) => source.SetRelative(v));
    }

    /// <summary>The pool of sound names to pick from each time this source plays, loaded from the res://audio folder. To use a different folder, set the `audio/openal_sound_folder.custom` setting in Project Settings</summary>
    [Export]
    public string[] Streams
    {
        get => _streams;
        set
        {
            _streams = value ?? [];
            UpdateConfigurationWarnings();
        }
    }

    /// <summary>When true and Streams has more than one entry, the same entry is never picked twice in a row</summary>
    [Export]
    public bool PlaybackNoRepeat
    {
        get => _playbackNoRepeat;
        set => _playbackNoRepeat = value;
    }
}
