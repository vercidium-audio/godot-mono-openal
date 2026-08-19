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

const AUTOLOAD_NAME = "ALManager"
const AUTOLOAD_PATH = "res://addons/godot-mono-openal/autoload/ALManagerAutoload.tscn"
const AUTOLOAD_TEMPLATE_PATH = "res://addons/godot-mono-openal/autoload/ALManagerAutoload.tscn.example"

func _enter_tree():
	add_custom_type("ALSource3D", "Node3D", preload("nodes/ALSource3D.cs"), null)
	add_custom_type("ALManager", "Node", preload("nodes/ALManager.cs"), null)
	
	# Connect to project settings to detect solution generation
	if not ProjectSettings.settings_changed.is_connected(_on_settings_changed):
		ProjectSettings.settings_changed.connect(_on_settings_changed)
	
	# Run setup immediately in case solution already exists
	_setup_project()

	# Register autoload
	_add_autoload()

	print("[godot-mono-openal] Plugin setup complete")

func _exit_tree():
	remove_custom_type("ALSource3D")
	remove_custom_type("ALManager")

	# Remove autoload
	_remove_autoload()

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

func _add_autoload():
	# Check if autoload is already registered
	if ProjectSettings.has_setting("autoload/" + AUTOLOAD_NAME):
		return

	# Copy template if the autoload scene doesn't exist
	if not FileAccess.file_exists(AUTOLOAD_PATH):
		if FileAccess.file_exists(AUTOLOAD_TEMPLATE_PATH):
			DirAccess.copy_absolute(ProjectSettings.globalize_path(AUTOLOAD_TEMPLATE_PATH), ProjectSettings.globalize_path(AUTOLOAD_PATH))
			print("[godot-mono-openal] Created ALManagerAutoload.tscn from template")
		else:
			push_error("[godot-mono-openal] Template file not found at ", AUTOLOAD_TEMPLATE_PATH)
			return

	# Register the autoload
	add_autoload_singleton(AUTOLOAD_NAME, AUTOLOAD_PATH)
	print("[godot-mono-openal] Registered ALManager autoload")

func _remove_autoload():
	# Check if autoload exists before removing
	if not ProjectSettings.has_setting("autoload/" + AUTOLOAD_NAME):
		return

	# Remove the autoload
	remove_autoload_singleton(AUTOLOAD_NAME)
	print("[godot-mono-openal] Removed ALManager autoload")
