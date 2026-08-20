@tool
extends EditorPlugin

const PACKAGE_REFERENCES = """	<ItemGroup>
		<PackageReference Include="openal_soft_bindings" Version="1.0.9" />
	</ItemGroup>"""

const PROPERTY_GROUP = """	<PropertyGroup>
		<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
	</PropertyGroup>"""

const DLL_SOURCE_WINDOWS = "addons/godot-mono-openal/soft_oal.dll"
const DLL_SOURCE_LINUX = "addons/godot-mono-openal/libopenal.so.1"

const ALManagerScript = preload("nodes/ALManager.cs")

# "audio/vaudio/*" Project Settings
const DEFAULT_DEVICE_LABEL = "System Default"

func _enter_tree():
	add_custom_type("ALSource3D", "Node3D", preload("nodes/ALSource3D.cs"), null)

	# Connect to project settings to detect solution generation
	if not ProjectSettings.settings_changed.is_connected(_on_settings_changed):
		ProjectSettings.settings_changed.connect(_on_settings_changed)

	# Run setup immediately in case solution already exists
	_setup_project()

	# Register audio/vaudio/* Project Settings
	_register_project_settings()
	ALManagerScript.call("RefreshDeviceLists")

	print("[godot-mono-openal] Plugin setup complete")

func _exit_tree():
	remove_custom_type("ALSource3D")

	if ProjectSettings.settings_changed.is_connected(_on_settings_changed):
		ProjectSettings.settings_changed.disconnect(_on_settings_changed)

	print("[godot-mono-openal] Plugin disabled")

func _on_settings_changed():
	_setup_project()

func _setup_project():
	var project_name = ProjectSettings.get_setting("application/config/name")
	var csproj_path = "res://%s.csproj" % project_name
	
	# Check if .csproj exists
	if not FileAccess.file_exists(csproj_path):
		push_error("[godot-mono-openal] No C# solution found. Please create a C# solution (Project → Tools → C# → Create C# Solution) and then re-enable this plugin")
		return
	
	# Read the .csproj file
	var file = FileAccess.open(csproj_path, FileAccess.READ)
	if not file:
		return
	
	var content = file.get_as_text()
	file.close()
	
	# Check if packages are already added
	if "openal_soft_bindings" in content:
		_copy_dll()
		return
	
	# Find the closing </Project> tag and insert our packages before it
	var insert_pos = content.rfind("</Project>")
	if insert_pos == -1:
		push_error("[godot-mono-openal] Could not find a </Project> tag in the .csproj file")
		return
	
	# Build the content to insert (PropertyGroup + ItemGroup)
	var insert_content = "\n" + PROPERTY_GROUP + "\n\n" + PACKAGE_REFERENCES + "\n"
	
	# Insert before </Project>
	var new_content = content.substr(0, insert_pos) + insert_content + content.substr(insert_pos)
	
	# Write back to file
	file = FileAccess.open(csproj_path, FileAccess.WRITE)
	if file:
		file.store_string(new_content)
		file.close()
		print("[godot-mono-openal] Added NuGet packages to ", csproj_path)
	
	_copy_dll()

func _copy_dll():
	var source_path: String
	var dest_path: String
	var lib_name: String

	if OS.get_name() == "Windows":
		source_path = DLL_SOURCE_WINDOWS
		dest_path = "res://soft_oal.dll"
		lib_name = "soft_oal.dll"
	elif OS.get_name() == "Linux":
		source_path = DLL_SOURCE_LINUX
		dest_path = "res://libopenal.so.1"
		lib_name = "libopenal.so.1"
	else:
		return

	# Check if library already exists at destination
	if FileAccess.file_exists(dest_path):
		return

	# Copy the library
	if FileAccess.file_exists(source_path):
		var result = DirAccess.copy_absolute(source_path, dest_path)
		if result == OK:
			print("[godot-mono-openal] Copied %s to project root" % lib_name)
		else:
			push_error("[godot-mono-openal] Failed to copy %s: %s" % [lib_name, result])
	else:
		push_error("[godot-mono-openal] Source library not found at ", source_path)

func _register_project_settings():
	# output_device: stored as DEFAULT_DEVICE_LABEL, not "", so the strict PROPERTY_HINT_ENUM
	# dropdown below always has a current value among its own entries.
	# ALManager.cs translates DEFAULT_DEVICE_LABEL back to "" ("driver default") when it reads
	# this setting, and rebuilds the hint_string below (via ProjectSettings.AddPropertyInfo)
	# once the real device list is known from OpenAL - registered with just the default label
	# here since GDScript has no OpenAL binding of its own to enumerate devices this early.
	if not ProjectSettings.has_setting("audio/vaudio/output_device"):
		ProjectSettings.set_setting("audio/vaudio/output_device", DEFAULT_DEVICE_LABEL)

	ProjectSettings.set_initial_value("audio/vaudio/output_device", DEFAULT_DEVICE_LABEL)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/output_device",
		"type": TYPE_STRING,
		"hint": PROPERTY_HINT_ENUM,
		"hint_string": DEFAULT_DEVICE_LABEL,
	})

	# max_reverb_sends: dev-only setting (not end-user-facing), default 1
	if not ProjectSettings.has_setting("audio/vaudio/max_reverb_sends"):
		ProjectSettings.set_setting("audio/vaudio/max_reverb_sends", 1)

	ProjectSettings.set_initial_value("audio/vaudio/max_reverb_sends", 1)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/max_reverb_sends",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_RANGE,
		"hint_string": "1,16,or_greater",
	})

	# sample_rate: 0 means "driver default" - never shown to the user as 0.
	if not ProjectSettings.has_setting("audio/vaudio/sample_rate"):
		ProjectSettings.set_setting("audio/vaudio/sample_rate", 0)

	ProjectSettings.set_initial_value("audio/vaudio/sample_rate", 0)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/sample_rate",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_ENUM,
		"hint_string": "System Default:0,22050,44100,48000,96000",
	})

	# hrtf_enabled: default true
	if not ProjectSettings.has_setting("audio/vaudio/hrtf_enabled"):
		ProjectSettings.set_setting("audio/vaudio/hrtf_enabled", true)

	ProjectSettings.set_initial_value("audio/vaudio/hrtf_enabled", true)

	# max_mono_sources/max_stereo_sources: project-level settings set by the developer, matching
	# the native Godot plugin's register_types.cpp - read once at device-open time, can't be
	# changed at runtime.
	if not ProjectSettings.has_setting("audio/vaudio/max_mono_sources"):
		ProjectSettings.set_setting("audio/vaudio/max_mono_sources", 16)

	ProjectSettings.set_initial_value("audio/vaudio/max_mono_sources", 16)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/max_mono_sources",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_RANGE,
		"hint_string": "0,256,or_greater",
	})

	if not ProjectSettings.has_setting("audio/vaudio/max_stereo_sources"):
		ProjectSettings.set_setting("audio/vaudio/max_stereo_sources", 240)

	ProjectSettings.set_initial_value("audio/vaudio/max_stereo_sources", 240)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/max_stereo_sources",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_RANGE,
		"hint_string": "0,256,or_greater",
	})
