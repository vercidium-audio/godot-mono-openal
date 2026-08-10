using System.Linq;

namespace godot_openal;

public partial class ALSource3D : Node3D
{
    // Signals
    [Signal]
    public delegate void FinishedEventHandler();

    static List<string> SupportedFileExtensions = ["ogg", "wav", "mp3", "flac"];

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new List<string>();

        foreach (var soundName in Streams)
        {
            if (soundName.Any(char.IsDigit))
            {
                warnings.Add($"Sound file should not contain digits: {soundName}");
            }
            else if (soundName.Contains('.'))
            {
                string soundPath = $"{Constants.AudioPath}/{soundName}";

                if (!FileAccess.FileExists(soundPath))
                {
                    warnings.Add($"Sound file not found: {soundPath}");
                }
            }
            else
            {
                // Guess the file type
                var anyExist = false;

                foreach (var extension in SupportedFileExtensions)
                {
                    string soundPath = $"{Constants.AudioPath}/{soundName}.{extension}";

                    if (FileAccess.FileExists(soundPath))
                    {
                        anyExist = true;
                        break;
                    }
                }

                if (!anyExist)
                    warnings.Add($"Sound file not found: {soundName}. File types attempted: {string.Join(", ", SupportedFileExtensions)}");
            }
        }

        return warnings.ToArray();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            return;

        for (int i = sources.Count - 1; i >= 0; i--)
        {
            var s = sources[i];

            if (s.Finished())
            {
                s.Dispose();
                sources.RemoveAt(i);
                
                if (sources.Count == 0)
                    EmitSignal(SignalName.Finished);

                continue;
            }

            if (!Relative)
                s.SetPosition(GlobalPosition);
        }
    }

    public override void _ExitTree()
    {
        filter?.Delete();

        foreach (var s in sources)
            s.Dispose();

        sources.Clear();
    }
}
