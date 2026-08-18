namespace godot_mono_openal;

public static class ALExtensions
{
    public static void SetPosition(this ALSource source, Vector3 v)  => AL.Sourcefv(source.ID, AL.AL_POSITION, [v.X, v.Y, v.Z]);
    public static void SetVelocity(this ALSource source, Vector3 v)  => AL.Sourcefv(source.ID, AL.AL_VELOCITY, [v.X, v.Y, v.Z]);
    public static void SetDirection(this ALSource source, Vector3 v) => AL.Sourcefv(source.ID, AL.AL_DIRECTION, [v.X, v.Y, v.Z]);
}