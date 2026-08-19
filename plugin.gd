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

const SINGLETON_NAME = "ALManager"

var _al_manager: Node

func _enter_tree():
	add_custom_type("ALSource3D", "Node3D", preload("nodes/ALSource3D.cs"), null)
	add_custom_type("ALManager", "Node", preload("nodes/ALManager.cs"), null)

	# Connect to project settings to detect solution generation
	if not ProjectSettings.settings_changed.is_connected(_on_settings_changed):
		ProjectSettings.settings_changed.connect(_on_settings_changed)

	# Run setup immediately in case solution already exists
	_setup_project()

	# Register the ALManager Engine singleton
	_add_singleton()

	print("[godot-mono-openal] Plugin setup complete")

func _exit_tree():
	remove_custom_type("ALSource3D")
	remove_custom_type("ALManager")

	# Remove the ALManager Engine singleton
	_remove_singleton()

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

func _add_singleton():
	# Check if the singleton is already registered (e.g. plugin reload)
	if Engine.has_singleton(SINGLETON_NAME):
		return

	# ALManager still needs a per-frame tick for ALContext.Process()/DisposeFinishedSources()/
	# capture device updates - it gets one by being added as a direct child of the root viewport
	# (not a scene autoload, so it survives scene changes without appearing in any scene's tree
	# or requiring a .tscn file), which still drives its _Process override normally.
	_al_manager = preload("nodes/ALManager.cs").new()
	_al_manager.name = SINGLETON_NAME
	get_tree().root.add_child.call_deferred(_al_manager)

	Engine.register_singleton(SINGLETON_NAME, _al_manager)
	print("[godot-mono-openal] Registered ALManager singleton")

func _remove_singleton():
	if Engine.has_singleton(SINGLETON_NAME):
		Engine.unregister_singleton(SINGLETON_NAME)

	if is_instance_valid(_al_manager):
		_al_manager.queue_free()
	_al_manager = null

	print("[godot-mono-openal] Removed ALManager singleton")
