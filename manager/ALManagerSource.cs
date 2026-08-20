using OpenALSource = global::OpenAL.managed.ALSource;

namespace godot_mono_openal;

public unsafe partial class ALManager
{
    public virtual bool TryCreateSource(AudioStream stream, bool spatialised, out OpenALSource source)
    {
        if (stream == null)
        {
            source = null;
            return false;
        }

        var buffer = GetOrCreateBuffer(stream);
        return buffer.TryCreateSource(spatialised, out source);
    }
}
