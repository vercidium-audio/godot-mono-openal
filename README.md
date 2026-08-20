# OpenAL Plugin for Mono Godot

This plugin provides custom nodes for using OpenAL Soft directly in Mono Godot, bypassing Godot's built-in audio system.

## Setup Instructions

### 1. Clone the Plugin

Clone `godot-mono-openal` to the `your_game/addons/` folder:

```bash
cd your_game
mkdir addons
cd addons
git clone git@github.com:vercidium-audio/godot-mono-openal.git`
```

### 2. Enable the Plugin

1. Open your Godot project
2. Ensure your C# solution is created: `Project > Tools > C# > Create C# Solution`
3. Enable `godot_mono_openal` in `Project > Project Settings > Plugins`

If you get the below error, make sure you've created a C# solution first (step 2 above):

```
[godot-mono-openal] No C# solution found. Please create a C# solution first (Project → Tools → C# → Create C# Solution)
```

After creating a C# project, disable and enable the `godot-mono-openal` plugin in `Project > Project Settings > Plugins` to perform setup.

### 3. Automatic Dependency Setup

The plugin setup script in `addons/godot-mono-openal/plugin.gd` will perform some setup logic for you.

First, it will add this text to your project's `.csproj` file:

```xml
<PropertyGroup>
    <!-- Allow unsafe code (required for buffering audio data to OpenAL Soft) -->
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>

<ItemGroup>
    <!-- C# bindings for OpenAL Soft -->
    <PackageReference Include="openal_soft_bindings" Version="1.0.10" />
</ItemGroup>
```

Second, it will copy `soft_oal.dll` or `libopenal.so.1` (depending on your operating system) to your project root, which is where your project searches for `.dll` files when it runs.

### 4. Customise the ALManager

The plugin registers `ALManager` as an Engine singleton (not a scene/autoload), and overrides Godot's inbuilt audio system. Its output device, reverb send count, sample rate and HRTF are configured via `Project > Project Settings > General > Audio > Vaudio`:

- `output_device` - which OpenAL device to use, or "System Default"
- `max_reverb_sends` - number of auxiliary sends available per source
- `sample_rate` - device sample rate, or "System Default"
- `hrtf_enabled` - whether to request HRTF from the driver

> If you don't see these settings, you may not have enabled the plugin correctly. See 'Step 2. Enable the Plugin' above, and make sure "Advanced Settings" is toggled on in the Project Settings dialog.

To verify your installation worked, the `output_device` setting should show a real device name once you've enabled the plugin (rather than just "System Default").

Everything else (master volume, distance model, meters per unit, speed of sound, listener position/orientation, etc.) is runtime-only and not exposed as a Project Setting or inspector property - call the corresponding methods/properties on `ALManager.instance` from code, e.g. `ALManager.instance.MasterVolume = 0.5f;`.

### 5. Play a Sound

Create an `ALSource3D` node and set its `Streams` array to one or more `AudioStream` resources (e.g. drag `.ogg`/`.wav`/`.mp3` files from the FileSystem dock into the Inspector). Each entry is decoded via Godot's own AudioStream/import pipeline, so any format Godot can import is supported. To play the sound, invoke `.Play()` on the node via GDScript or C# - if `Streams` has more than one entry, one is picked at random each time (see `PlaybackNoRepeat` to avoid repeats).