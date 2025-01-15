
# RFG zone file format
This file describes the zone format used by RFG. These files have either the `.rfgzone_pc` or `.layer_pc` extension. They contain a list of objects and their properties for a single map zone. Multiplayer and Wrecking Crew maps all consist of one zone. Singleplayer maps consist of many zones. 

This document is very incomplete. RFG mapping is still in an early state with very basic tools. As the tools develop we'll be able to test out different properties and improve this file.

# Format description
This section hasn't been written yet. Please refer to [ZoneFile.bf](https://github.com/Moneyl/RfgTools/blob/main/src/Formats/Zones/ZoneFile.bf).

# Object types
This section lists all zone object types and the properties they support. Zone objects also have the properties of any type they inherit. This applies recursively. E.g. If type `C` inherits `B` which inherits `A`. Then `C` has all the properties of `B` and `A`. The property names are the unique identifiers the game uses for them. There are some intentional typos in the names since the typos are hardcoded into the vanilla game.

## General objects
These types are used in all game modes.


### **object**
The base type inherited by all other objects.


**just_pos** (*Vec3*, Type=5, Size=12):

Object position. If the object has this property then `orient` is set to the identity matrix.


**op** (Type=5, Size=48, Optional):

Position (Vec3) and orient (Mat33). This will be ignored if the object also has a `just_pos` property.


**display_name** (*string*, Type=4, Optional):

It's unknown if this has any use in the release version of the game. This might be a remnant from CLOE. The game seems to store the name at runtime, so it might be useful for locating objects when we eventually have in game debugging tools.

----------------

### **obj_zone** (*inherits [object](#object)*)
Each zone has one of these objects. They sit at the center of the zone and define the terrain file to use for that zone.


**ambient_spawn** (*string*, Type=4, Optional):

Name of the `ambient_spawn_info.xtbl` entry used by this object.


**spawn_resource** (*string*, Type=4, Optional):

Name of the `spawn_resource.xtbl` entry used by this object.

Default = `Default`


**terrain_file_name** (*string*, Type=4):

Base name for the terrain files used by this zone. E.g. If it equals `mp_crescent` then the terrain files should be `mp_crescent.cterrain_pc`, `mp_crescent_0.ctmesh_pc`, etc.


**wind_min_speed** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose. Might related to the ambient wind effect seen on some maps.

Default = `50.0`


**wind_max_speed** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose. Might related to the ambient wind effect seen on some maps.

Default = `80.0`

----------------

### **object_bounding_box** (*inherits [object](#object)*)
General bounding box used by the game for mission scripting.


**bb** (*bbox*, Type=5, Size=24):

Local space bounding box. 


**bounding_box_type** (*enum(string)*, Type=4, Optional):

Likely used to determine what logic the game should run on it.

Default = `None`

Options:
- `GPS Target`
- `None`


----------------

### **object_dummy** (*inherits [object](#object)*)
Dummy object used for scripting.


**dummy_type** (*enum(string)*, Type=4):

It's currently unknown what effect each of these types has.

Options:
- `None`
- `Tech Reponse Pos`
- `VRail Spawn`
- `Demo Master`
- `Cutscene`
- `Air Bomb`
- `Rally`
- `Barricade`
- `Reinforced_Fence`
- `Smoke Plume`
- `Demolition`

----------------

### **player_start** (*inherits [object](#object)*)
Spawn location for the player.


**indoor** (*bool*, Type=5, Size=1):

Purpose unknown.


**mp_team** (*enum(string)*, Type=4, Optional):

The team the player is placed in when they spawn.

Default: `None`

Options:
- `None`
- `Neutral`
- `Guerilla`
- `EDF`
- `Civilian`
- `Marauder`


**initial_spawn** (*bool*, Type=5, Size=1):

Purpose unknown.

Default = `false`


**respawn** (*bool*, Type=5, Size=1):

Purpose unknown.

Default = `false`


**checkpoint_respawn** (*bool*, Type=5, Size=1):

Purpose unknown.

Default = `false`


**activity_respawn** (*bool*, Type=5, Size=1):

Purpose unknown.

Default = `false`


**mission_info** (*string*, Type=4, Optional):

The name of an entry in `missions.xtbl`. Exact use case unknown. Likely used to indicate checkpoint spawn locations during SP missions. This property is only loaded by the game if `checkpoint_respawn` is true.

----------------

### **trigger_region** (*inherits [object](#object)*)
Used to create cliff and landmine killzones in SP and MP. Its triggers when the player enters it. Likely used in SP mission scripting as well.


**trigger_shape** (*enum(string)*, Type=4, Optional):

The shape of the trigger region.

Default = `box`

Options:
- `box`
- `sphere`


**bb** (*bounding box*, Type=5, Size=24):

Only loaded by the game when `trigger_shape` is `box`. Its a local space bounding box.


**outer_radius** (*float*, Type=5, Size=4):

Only loaded by the game when `trigger_shape` is `sphere`. Its a local space bounding box.


**enabled** (*bool*, Type=5, Size=1):

Allows mappers to enable/disable the region.


**region_type** (*enum(string)*, Type=4, Optional):

The action that should occur when the region is entered by a player.

Default = `default`

Options:
- `default`
- `kill human`


**region_kill_type** (*enum(string)*, Type=4, Optional):

How to kill those who enter the region when `region_type` is `kill human`.

Default = `cliff`

Options:
- `cliff`
- `mine`


**trigger_flags** (*flags(string)*, Type=4, Optional)

Disables the trigger region during a mission or activity, tested with the cliff in the tutorial.

Options:
- `not_in_activity`
- `not_in_mission`

----------------

### **object_mover** (*inherits [object](#object)*)
Base class for movers. The capabilities of this type versus other movers is currently unknown.


**building_type** (*flags(string)*, Type=4)

Purpose unknown.

Options:
- `Dynamic`
- `Force_Field`
- `Bridge`
- `Raid`
- `House`
- `Player_Base`
- `Communications`


**dest_checksum** (*uint*, Type=5, Size=4):

Purpose unknown.


**gameplay_props** (*string*, Type=4, Optional):

The name of an entry in `gameplay_properties.xtbl`.

Default = `Default`


**flags** (*uint*, Type=5, Size=4):

Bitflags stored in a 32bit integer. The value of each bit is unknown. If this property is present `chunk_flags` will be ignored. This property offers more control, however we don't currently know what any of the flags do.


**chunk_flags** (*flags(string)*, Type=4, Optional)

Likely used by the game to give buildings special behavior. Untested at the time of writing. It appears that you can use `gameplay_properties.xtbl` to apply these flags too.

Options:
- `child_gives_control`
- `building`
- `dynamic_link`
- `world_anchor`
- `no_cover`
- `propaganda`
- `kiosk`
- `touch_terrain`
- `supply_crate`
- `mining`
- `one_of_many`
- `plume_on_death`
- `invulnerable`
- `inherit_damage_pct`
- `regrow_on_stream`
- `casts_drop_shadow`
- `disable_collapse_effect`
- `force_dynamic`
- `show_on_map`
- `regenerate`
- `casts_shadow`


**dynamic_object** (*bool*, Type=5, Size=1):

Purpose unknown.


**chunk_uid** (*uint*, Type=5, Size=4):

Purpose unknown.


**props** (*string*, Type=4, Optional):

The name of an entry in `level_objects.xtbl` or the equivalent xtbls for the DLC.

Default = `Default`


**chunk_name** (*string*, Type=4, Optional):

The name of the asset container this object should use, with the `.rfgchunkx` extension excluded. The asset containers are defined in the asm_pc file for that map. This is only loaded by the game if `props` exists and has a valid value.


**uid** (*uint*, Type=5, Size=4):

UID of the destroyable to use in the assets mesh. cchk_pc mesh files can contain multiple variants of a building known as destroyables. This is only loaded if `props` and `chunk_name` are valid and the streaming system manages to find the asset specified by `chunk_name`.


**shape_uid** (*uint*, Type=5, Size=4):

Purpose unknown. This is only loaded if `props` and `chunk_name` are valid and the streaming system manages to find the asset specified by `chunk_name`.


**team** (*string*, Type=4, Optional):

The team assigned to the mover. This is only loaded if `props` and `chunk_name` are valid and the streaming system manages to find the asset specified by `chunk_name`.

Default: `None`

Options:
- `None`
- `Neutral`
- `Guerilla`
- `EDF`
- `Civilian`
- `Marauder`


**control** (*float*, Type=5, Size=4):

Likely how much control is reduced when the building is control. This is only loaded when the 2nd and 16th bits of `flags` are false. This property is untested. `gameplay_properties.xtbl` also has a `control` field so you should try that one first.

----------------

### **general_mover** (*inherits [object_mover](#object_mover-inherits-object)*)
Another class of movers. The exact difference between this and other movers isn't known yet. Though typically simpler map objects like explosive barrels will be a `general_mover`.


**gm_flags** (*uint*, Type=5, Size=4):

Bitflags stored in 32 bit integer. Purpose and flags unknown.


**original_object** (*uint*, Type=5, Size=4):

Purpose unknown.


**ctype** (*uint*, Type=5, Size=4):

Most likely a collision filter used by the physics engine. The exact behavior isn't known yet. It may be based on collision layers, where each bit toggles whether an item collides with that layer or not. This property is only loaded if bit 2 of `flags` (inherited from [object_mover](#object_mover-inherits-object)) is off.

The game has quite a few presets. Though if it works as theorized above you could define your own by manually setting which layers it collides with. It's likely that many of these aren't useful for map editing since some appear to only be used for raycasting and object collection. Here are the known presets:
- World = `65538`
- Player = `6`
- NPC = `7`
- HumanMinimum = `63`
- Turret = `589832`
- Vehicle = `9`
- Walker = `12`
- WalkerHull = `14`
- Flyer = `15`
- Ragdoll = `131077`
- Mover = `3`
- Item = `589828`
- OnlyForObjectCollection = `393275`
- HumanHitDetect = `393265`
- Projectile = `327696`
- ProjectileAttached = `393233`
- PersonalShield = `393234`
- ObjectCollector = `19`
- ObjectCollectorNoItems = `20`
- ObjectCollectorVehiclesOnly = `21`
- Navmesh = `23`
- VehicleNavmesh = `24`
- NoCollision = `393238`
- HumanOnLadder = `50`
- InitialPosition = `524327`
- Normal = `40`
- NormalFast = `393257`
- Lite = `393258`
- UltraLite = `393259`
- WorldOnly = `393260`
- RayDestroyable = `45`
- Stress = `46`
- RayDefault = `25`
- RayTerrainOnly = `27`
- RayWorldOnly = `28`
- RayWalkingSurfaceOnly = `29`
- Keyframed = `458788`
- RayNavmeshOnly = `31`
- RayRagdollOnly = `32`
- RayVehicleNavmeshOnly = `34`
- RayVehicleEnterExit = `35`
- RayCity = `30`
- RayPlayerCover = `33`
- DeadHuman = `393253`
- PhysicalObject = `393254`
- PhysicalObjectSelf = `38`
- Camera = `48`
- HumanNotOtherHumans = `52`
- WorldAnchor = `393270`
- DynamicLink = `393271`
- DeleteObject = `56`
- RayVehicleWheel = `57`
- EffectRigidBody = `58`
- ClothSim = `60`
- InvisibleBarrier = `393277`
- ForceField = `62`


**idx** (*uint*, Type=5, Size=4):

Known internally as "subpiece index". Purpose unknown. This property is only loaded if bit 2 of `flags` (inherited from [object_mover](#object_mover-inherits-object)) is on.


**mtype** (*uint*, Type=5, Size=4):

Known internally as "move type". Not all uses known. This property is only loaded if bit 2 of `flags` (inherited from [object_mover](#object_mover-inherits-object)) is on.

- Fixed = `0` (default)
- Normal = `1` (unknown)
- Lite = `2` (unknown)
- Ultra Lite = `3` (unknown)
- World Only = `4` (no collision but you can still destroy it with a hammer for example)
- No Collision = `5` (no collision)


**destruct_uid** (*uint*, Type=5, Size=4):

Purpose unknown.


**hitpoints** (*int*, Type=5, Size=4, Optional):

Purpose unknown. Only loaded in certain conditions which currently are not known.


**dislodge_hitpoints** (*int*, Type=5, Size=4, Optional):

Purpose unknown. Only loaded in certain conditions which currently are not known.

----------------

### **rfg_mover** (*inherits [object_mover](#object_mover-inherits-object)*)
This is the type used for most destructible structures like small walls, and bridges, and walkways.


**mtype** (*uint*, Type=5, Size=4):

Called "move type" internally. Not all uses known.

Options:
- Fixed = `0` (default)
- Normal = `1` (unknown)
- Lite = `2` (unknown)
- Ultra Lite = `3` (unknown)
- World Only = `4` (no collision but you can still destroy it with a hammer for example)
- No Collision = `5` (no collision)


**rfg_flags** (*uint*, Type=5, Size=4):

Bitflags used to define a bunch of object state. The behavior of each bit is currently unknown. Some of these are clearly for runtime use. This properly most likely isn't intended for use in zone files. It probably is used to save runtime state of movers in save files. Still listing this property for completeness sake. The bit names in order are:
- `under_stress`
- `run_stress_on_stream`
- `collapse_effect`
- `invulnerable`
- `game_destroyed`
- `fully_destroyed`
- `large_explosion`
- `pristine`
- `mp_no_fade_destroy`
- `render_xform_dirty`
- `is_ai_structure`
- `is_ai_building`
- `is_ai_subpiece`
- `is_ai_composite`
- `updating_anchors`
- `plume_on_death`
- `one_of_many`
- `mining`
- `supply_crate`
- `regrow_on_stream_begin`
- `regrow_on_stream_finish`
- `inherit_damage_pct`
- `rebuild`
- `propaganda`
- `building`


**damage_percent** (*float*, Type=5, Size=4, Optional):

Purpose unknown.


**salvage_num_pieces** (*int*, Type=5, Size=4, Optional):

Most likely the number of salvage pieces it drops when destroyed. Untested. It's most likely easier to use the variable with the same name in `gameplay_properties.xtbl`


**game_destroyed_pct** (*float*, Type=5, Size=4, Optional):

Purpose unknown. It's most likely easier to use the variable with the same name in `gameplay_properties.xtbl`


**stream_out_play_time** (*int*, Type=5, Size=4, Optional):

Purpose unknown.


**world_anchors** (*buffer*, Type=6, Size=Variable, Optional):

A variable sized binary buffer. Exact contents unknown. Likely physics engine related data. Removing this from rfg movers causes them to be very unstable and sometimes fall through the terrain or explode into chunks.


**dynamic_links** (*buffer*, Type=6, Size=Variable, Optional):

A variable sized binary buffer. Purpose unknown.

----------------

### **shape_cutter** (*inherits [object](#object)*)
Used to damage or cut holes into destructible objects ahead of time.


**flags** (*int16*, Type=5, Size=2):

Bitflags stored in a 16 bit integer. Flags unknown.


**outer_radius** (*float*, Type=5, Size=4, Optional):

Purpose unknown.


**source** (*uint*, Type=5, Size=4, Optional):

Believed to be an object handle. 


**owner** (*uint*, Type=5, Size=4, Optional):

Believed to be an object handle.


**exp_info** (*u8*, Type=5, Size=1, Optional):

Unique ID of an explosion stored in an 8 bit integer.

----------------

### **object_effect** (*inherits [object](#object)*)
Plays an effect such as smoke, fire, explosion, sparks, etc. Also can play sounds if the effect has one in `effects.xtbl` and the `sound` string is used.


**sound_alr** (*string*, Type=?, ?):

Untested.


**sound** (*string*, Type=?, ?):

The name of the sound in `effects.xtbl`.


**visual** (*string*, Type=?, ?):

The name of the effect in `effects.xtbl`.


**looping** (*bool*, Type=5, Size=1):

Controls if the effect will play once or continuously.


**effect_type** (*string[67]*, Type=4, Optional):

The name of an entry in `effects.xtbl`.


**Note:** This class has several other properties that aren't documented for the moment. They essentially do the same thing as `effect_type` but using runtime data. They aren't documented for now since they appear to be for runtime use only and `effect_type` overrides them.

----------------

### **item** (*inherits [object](#object)*)
Base class for pickup objects like weapons and MP flags.


**item_type** (*string[255]*, Type=4, Optional):

The name of an entry in `items_3d.xtbl`.


**preplaced** (*bool*, Type=5, Size=1, Optional):

Tested in multiplayer & this doesn't stop the weapon from spawning but you can use the gamemode flags to disable weapon spawns without deleting them.


**respawns** (*bool*, Type=5, Size=1, Optional):

Causes the weapon to not respawn again during a match after it has been picked up for the first time.


**respawn** (*bool*, Type=5, Size=1, Optional):

Untested.


**mass_override** (*float*, Type=5, Size=4, Optional):

Untested.


**flags** (*uint*, Type=5, Size=4, Optional):

32bit integer holding bitflags. Untested.


**ctype** (*uint*, Type=5, Size=4, Optional):

Labelled by the game as "collision type". Exact values unknown and untested. May be related to the values for the ctype field on [general_mover](#general_mover-inherits-object_mover).


**Note:** This class has several other properties that aren't documented for the moment. They essentially do the same thing as `item_type` but using runtime data. They aren't documented for now since they appear to be for runtime use only and `item_type` overrides them.

----------------

### **weapon** (*inherits [item](#item-inherits-object)*)
A weapon that the player can pick up by running into.


**weapon_type** (*string[255]*, Type=4, Optional):

The name of an entry in `weapons.xtbl`. Only loaded if `item_type` isn't present.


**Note:** This class has some runtime properties that were excluded. Just like `item` and `object_effect` do.

----------------

### **ladder** (*inherits [object](#object)*)
A ladder. Players and NPCs can climb it.


**ladder_rungs** (*int*, Type=5, Size=4):

The number of rungs on the ladder. Haven't tested changing it yet.


**ladder_enabled** (*bool*, Type=5, Size=1):

The ladders I checked in both SP & MP lacked this prop, tested adding it, disabling, enabling and both the player and NPCs could still climb it. Might only be used at runtime to disable ladders when the building has been destroyed enough?

----------------

### **obj_light** (*inherits [object](#object)*)
Lights up the environment. There's only one in the SP map and it hasn't been tested much in MP, so the limits of these lights aren't well known.


**atten_start** (*float*, Type=5, Size=4, Optional):

Effects light intensity falloff. Untested.


**atten_end** (*float*, Type=5, Size=4, Optional):

Effects light intensity falloff. Untested.


**atten_range** (*float*, Type=5, Size=4, Optional):

Effects light intensity falloff. Untested. Only loaded if `atten_end` isn't present. Setting this effectively sets `atten_end` to `atten_start + atten_range`.


**light_flags** (*flags(string)*, Type=4, Optional):

Behavior unknown.

Options:
- `use_clipping`
- `daytime`
- `nighttime`


**type_enum** (*enum(string)*, Type=4, Optional):

The shape/direction of the light.

Options:
- `spot`
- `omni`


**clr_orig** (*vec3*, Type=5, Size=12):

Most likely the lights RGB color. Untested.


**hotspot_size** (*float*, Type=5, Size=4, Optional):

Behavior unknown.


**hotspot_falloff_size** (*float*, Type=5, Size=4, Optional):

Behavior unknown.


**min_clip** (*vec3*, Type=5, Size=12):

Behavior unknown. Possibly uses to limit the area the object lights along with `max_clip`. Untested.


**max_clip** (*vec3*, Type=5, Size=12):

Behavior unknown. Only loaded if `min_clip` is present.


**clip_mesh** (*int*, Type=5, Size=4):

Appears to be the hash of a mesh files name. Untested.

----------------

## MP only objects
These types are only used in multiplayer maps.

### **multi_object_marker** (*inherits [object](#object)*)
Used to mark objects for several MP game modes. For example: siege targets, flag capture zones, or king of the hill targets.


**bb** (*bbox*, Type=5, Size=24):

Local space bounding box. 


**marker_type** (*enum(string)*, Type=4):

The type of marker this is. E.g. spawn node, siege target, etc.

Default: `None`

Options:
- `None`
- `Siege target`
- `Backpack rack`
- `Spawn node`
- `Flag capture zone`
- `King of the Hill target`
- `Spectator camera`


**mp_team** (*enum(string)*, Type=4):

Team the object is owned by. Not loaded if `marker_type` is `None`.

Default: `None`

Options:
- `None`
- `Neutral`
- `Guerilla`
- `EDF`
- `Civilian`
- `Marauder`

Note: In multiplayer the only valid teams are `Neutral`, `Guerilla` & `EDF`. Other teams are singleplayer only.

**priority** (*int*, Type=5, Size=4):

- For `Siege target` children it determines which siege objects are unlocked to destroy, eg if you set it to <code>1</code> it would be the first target the attackers can destroy, you can have multiple targets unlocked by using the same number and if you wanted to have targets unlocked one at a time you would use <code>1</code>, <code>2</code>, <code>3</code> and so on. All other `multi_object_markers` use a value of `4294967295` and it appears to have no use unless the `marker_type` is set to `Siege target`. If you use a value of `4294967295` for `Siege target` children they are also unlocked at the same time.

**backpack_type** (*enum(string)*, Type=4):

Type of backpack to provide the players. Only loaded if `marker_type` is `Backpack rack`.

Default: `Jetpack`

Options:
- `Commando`
- `Fleetfoot`
- `Heal`
- `Jetpack`
- `Kaboom`
- `Rhino`
- `Shockwave`
- `Stealth`
- `Thrust`
- `Tremor`
- `Vision`


**num_backpacks** (*int*, Type=5, Size=4, Max=3):

The number of backpacks the rack can provide to players.


**random_backpacks** (*bool*, Type=5, Size=1):

Tested and it doesn't randomize the backpack on the rack. Might be related to the random backpack option in multiplayer custom game settings?


**group** (*int*, Type=5, Size=4):

Untested.

----------------

### **multi_object_flag** (*inherits [item](#item-inherits-object)*)
Spawn point of capture the flag targets.


**mp_team** (*enum(string)*, Type=4):

Team the object is owned by.

Default: `None`

Options:
- `None`
- `Neutral`
- `Guerilla`
- `EDF`
- `Civilian`
- `Marauder`

----------------

## SP only objects
These types are only used in single player zones. One exception is `navpoint`, `cover_node`, `constraint`, and `object_action_node` objects found in the Nordic Special map. This is thought to be a mistake or some objects left behind from the remaster developers learning the mapping tools. The section is incomplete since the Nanoforge rewrite doesn't support opening and editing single player maps yet. It'll be updated when SP support is added.


### **navpoint** (*inherits [object](#object)*)
Thought to be used by AI for navigation.


**navpoint_data** (*buffer*, Type=6, Size=Variable, Optional):

Combined buffer with several fields that make up a navpoint (fields not yet documented). Should be able to figure out the fields by looking at how the game reads from the buffer in navpoint::serialize() when the navpoint_data field is present.


**nav_type** (*u8*, Type=5, Size=1, Optional):

Only read if `navpoint_data` is not present. Alternative field for `navpoint_type`. Stored in a u8 instead of a string.

Options:
- `ground` = 0
- `patrol` = 1
- `pedestrian` = 2
- `floating` = 3
- `supply dropoff` = 4
- `road` = 5
- `road jump` = 6
- `road jump dest` = 7
- `road jump start` = 8
- `road checkpoint` = 9
- `path` = 10
- `transition` = 11
- `ladder transition` = 12
- `ladder` = 13
- `stairs` = 14
- `transition jump` = 15
- `transition leap over` = 16
- `road bridge` = 22
- `path door` = 23
- `transition door` = 24


**navpoint_type** (*enum(string)*, Type=4, Optional):

Only read if `navpoint_data` and `nav_type` are not present. In that case it is required.

Options:
- `ground`
- `patrol`
- `pedestrian`
- `floating`
- `supply dropoff`
- `road`
- `road jump`
- `road jump dest`
- `road jump start`
- `road checkpoint`
- `path`
- `transition`
- `ladder transition`
- `ladder`
- `stairs`
- `transition jump`
- `transition leap over`
- `road bridge`
- `path door`
- `transition door`


**outer_radius** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose. Only read if `navpoint_data` is not present.

Default = `0.5`


**speed_limit** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose. Likely used by vehicle AI to control speed. Only read if `navpoint_data` is not present.

Default = `10.0`


**dont_follow_road** (*bool*, Type=5, Size=1, Optional):

Unconfirmed purpose. Only loaded if type `nav_type` and `navpoint_type` are not `patrol`, and `navpoint_data` is not present.


**ignore_lanes** (*bool*, Type=5, Size=1, Optional):

Unconfirmed purpose. Only loaded if type `nav_type` and `navpoint_type` are not `patrol`, and `navpoint_data` is not present.


**obj_links** (*buffer*, Type=6, Size=Variable, Optional):

Unconfirmed purpose.


**navlinks** (*buffer*, Type=6, Size=Variable, Optional):

Unconfirmed purpose. Only loaded if `obj_links` isn't present.


**nav_orient** (*Mat33*, Type=5, Size=36, Optional):

Unconfirmed purpose. Only loaded if the type is `road checkpoint`.

----------------

### **cover_node** (*inherits [object](#object)*)
Thought to be used for the players cover system.


**covernode_data** (*buffer*, Type=6, Size=Variable, Optional):

Combined buffer with several fields that make up a cover_node (fields not yet documented). Should be able to figure out the fields by looking at how the game reads from the buffer in cover_node::serialize() when the covernode_data field is present.


**cnp** (*buffer*, Type=6, Size=Variable, Optional):

Appears to be an old version of `covernode_data` with different fields/layout.


**def_angle_left** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose.

Default = `90.0`


**def_angle_right** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose.

Default = `90.0`


**off_angle_left** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose.


**angle_left** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose. Only loaded if `off_angle_left` isn't present. It's required in that case.


**off_angle_right** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose.


**angle_right** (*float*, Type=5, Size=4, Optional):

Unconfirmed purpose. Only loaded if `off_angle_right` isn't present. It's required in that case.


**binary_flags** (*u16*, Type=5, Size=2, Optional):

Bitflags in u16. It's not known if these are meant to be set in zone files or if they're mainly runtime flags.

Here are the known bits in the first byte. Each flag is 1 bit:
- `disabled_reason`
- `dynamic`
- `zone`
- `vehicle`
- `crouch`

Here are the known bits in the second byte. Each flag is 1 bit:
- `fire_popup`
- `fire_left`
- `fire_right`
- `off_navmesh`
- `on_mover`


**stance** (*string*, Type=4, Optional):

Purpose unconfirmed. If it contains `crouching` it activates the 7th bit (2^7) on the first byte of the u16 described for `binary_flags`. We don't know what that flag is at the moment. It looks like any values other than `crouching` get ignored.


**firing_flags** (*string*, Type=4, Optional):

If `stance` contains `crouching` and this property contains `standing` then the `disabled_reason` bit in the flags described in `binary_flags` gets enabled.
If this field contains `left` then the `dynamic` bit is enabled.
If this field contains `right` then the `zone` bit is enabled

----------------

### **constraint** (*inherits [object](#object)*)
Used for physics contraints like rotating joints on windmills.


**template** (*buffer*, Type=5, Size=156, Optional):

Has a bunch of fields defining the constraint type and its configuration. Not yet documented in this file. See the `constraint_template` struct.


**host_handle** (*uint*, Type=5, Size=4, Optional):

Purpose unconfirmed.


**child_handle** (*uint*, Type=5, Size=4, Optional):

Purpose unconfirmed.


**host_index** (*uint*, Type=5, Size=4, Optional):

Purpose unconfirmed.


**child_index** (*uint*, Type=5, Size=4, Optional):

Purpose unconfirmed.


**host_hk_alt_index** (*uint*, Type=5, Size=4, Optional):

Purpose unconfirmed. Appears to be a runtime only value or a value only used when the game saves and loads a SP game.


**child_hk_alt_index** (*uint*, Type=5, Size=4, Optional):

Purpose unconfirmed. Appears to be a runtime only value or a value only used when the game saves and loads a SP game.

----------------

### **object_squad** (*inherits [object](#object)*)

----------------

### **object_turret_spawn_node** (*inherits [object](#object)*)

----------------

### **object_air_strike_defense_node** (*inherits [object](#object)*)
Start location for a cancelled or incomplete activity.

----------------

### **object_spawn_region** (*inherits [object](#object)*)

----------------

### **object_demolitions_master_node** (*inherits [object](#object)*)

----------------

### **object_convoy** (*inherits [object](#object)*)

----------------

### **object_convoy_end_point** (*inherits [object](#object)*)

----------------

### **object_courier_end_point** (*inherits [object](#object)*)

----------------

### **object_vehicle_spawn_node** (*inherits [object](#object)*)

----------------

### **object_riding_shotgun_node** (*inherits [object](#object)*)

----------------

### **object_area_defense_node** (*inherits [object](#object)*)

----------------

### **object_action_node** (*inherits [object](#object)*)
Used for action nodes like the weapons locker and upgrade table.


**animation_type** (*string*, Type=4):

References an entry in `action_node_types.xtbl`. Should match the `Name` field of one of the entries. None of the other fields will load if this is not valid.


**high_priority** (*bool*, Type=5, Size=1, Optional):

Purpose unknown.


**infinite_duration** (*bool*, Type=5, Size=1, Optional):

Purpose unknown.


**disabled** (*int*, Type=5, Size=4, Optional):

Purpose unknown. Sets an int32 field `disabled_by` on the object. Could be referencing an object handle, but those are unsigned integers.


**run_to** (*bool*, Type=5, Size=1, Optional):

Purpose unknown.


**obj_links** (*buffer*, Type=6, Size=Variable, Optional):

Unconfirmed purpose. The conditions for this to be loaded are not known.


**links** (*buffer*, Type=6, Size=Variable, Optional):

Unconfirmed purpose. If the game tries to load `obj_links` and can't find it, then it tries to load this.


----------------

### **object_raid_node** (*inherits [object](#object)*)

----------------

### **object_guard_node** (*inherits [object](#object)*)

----------------

### **object_house_arrest_node** (*inherits [object](#object)*)

----------------

### **object_safehouse** (*inherits [object](#object)*)

----------------

### **object_activity_spawn** (*inherits [object](#object)*)

----------------

### **object_squad_spawn_node** (*inherits [object](#object)*)

----------------

### **object_upgrade_node** (*inherits [object](#object)*)

----------------

### **object_path_road** (*inherits [object](#object)*)

----------------

### **object_bftp_node** (*inherits [object](#object)*)

----------------

### **object_mission_start_node** (*inherits [object](#object)*)

----------------

### **marauder_ambush_region** (*inherits [object](#object)*)

----------------

### **object_roadblock_node** (*inherits [object](#object)*)

----------------

### **object_restricted_area** (*inherits [object](#object)*)

----------------

### **object_delivery_node** (*inherits [object](#object)*)

----------------

### **object_ambient_behavior_region** (*inherits [object](#object)*)

----------------

### **object_npc_spawn_node** (*inherits [object](#object)*)

----------------

### **object_patrol** (*inherits [object](#object)*)

----------------

## Runtime objects
These types aren't found in vanilla zone files. They're only created by the game at runtime. No one has tested adding one of these to a map at the time of writing since Nanoforge doesn't support them yet. The section is incomplete for the same reason.

### **multi_object_backpack** (*inherits [item](#item-inherits-object)*)
MP backpack object. Spawned by a `multi_object_marker` object with `marker_type` equal to `Backpack rack`.


**backpack_type** (*enum(string)*, Type=4, Optional):

Type of backpack to provide the players. References an entry in mp_backpacks.xtbl.

----------------

### **projectile** (*inherits [item](#item-inherits-object)*)

----------------

### **vehicle** (*inherits [object](#object)*)

----------------

### **automobile** (*inherits [vehicle](#vehicle-inherits-object)*)

----------------

### **walker** (*inherits [vehicle](#vehicle-inherits-object)*)

----------------

### **flyer** (*inherits [vehicle](#vehicle-inherits-object)*)

----------------

### **human** (*inherits [object](#object)*)

----------------

### **npc** (*inherits [human](#human-inherits-object)*)

----------------

### **player** (*inherits [human](#human-inherits-object)*)

----------------

### **turret** (*inherits [object](#object)*)

----------------

### **object_debris** (*inherits [object](#object)*)

----------------

### **district** (*inherits [object](#object)*)
