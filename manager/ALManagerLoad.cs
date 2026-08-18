namespace godot_openal;

public unsafe partial class ALManager
{
    // One ALBuffer per unique AudioStream resource, decoded on first use and reused by every
    // ALSource3D that references the same stream. Keyed by the AudioStream instance rather than
    // a file path since a stream can be created at runtime without a resource_path.
    Dictionary<AudioStream, ALBuffer> DecodedStreams = [];

    public ALBuffer GetOrCreateBuffer(AudioStream stream)
    {
        if (DecodedStreams.TryGetValue(stream, out var buffer))
            return buffer;

        buffer = new ALBuffer(ALContext, stream);
        DecodedStreams[stream] = buffer;
        return buffer;
    }
}
