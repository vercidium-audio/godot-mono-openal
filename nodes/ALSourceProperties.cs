using OpenALSource = global::OpenAL.managed.ALSource;

namespace godot_mono_openal;

public partial class ALSource : Node3D
{
    float _volume = 1;
    float _pitch = 1;
    bool _looping = false;
    AudioStream[] _streams = [];
    bool _playbackNoRepeat = true;

    protected void UpdateProperty<T>(ref T field, T value, Action<T, OpenALSource> updateAction) where T : struct
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

    /// <summary>Whether the sound plays indefinitely on loop</summary>
    [Export]
    public bool Looping
    {
        get => _looping;
        set => UpdateProperty(ref _looping, value, (v, source) => source.SetLooping(v));
    }

    /// <summary>The pool of sounds to pick from each time this source plays. Decoded on demand via Godot's own AudioStream/import pipeline and cached per-resource by ALManager.</summary>
    [Export]
    public AudioStream[] Streams
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

    /// <summary>Script-only alias for <see cref="Streams"/>, matching AudioStreamPlayer3D's single "stream" property. Lets a script written against AudioStreamPlayer3D keep working unmodified after converting to this node. Reads back the first entry of <see cref="Streams"/>; writes replace <see cref="Streams"/> with a one-entry array.</summary>
    public AudioStream Stream
    {
        get => _streams.Length == 0 ? null : _streams[0];
        set => Streams = [value];
    }

    /// <summary>Script-only alias for <see cref="Pitch"/>, matching AudioStreamPlayer3D's "pitch_scale" property.</summary>
    public float PitchScale
    {
        get => Pitch;
        set => Pitch = value;
    }

    /// <summary>Script-only alias for <see cref="Volume"/>, matching AudioStreamPlayer(3D)'s logarithmic "volume_db" property. Converts through LinearToDb/DbToLinear since <see cref="Volume"/> is linear.</summary>
    public float VolumeDb
    {
        get => Mathf.LinearToDb(Volume);
        set => Volume = Mathf.DbToLinear(value);
    }
}
