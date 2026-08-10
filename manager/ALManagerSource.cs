namespace godot_openal;

public unsafe partial class ALManager
{
    public virtual bool TryCreateSource(AudioStream stream, bool spatialised, out ALSource source)
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
