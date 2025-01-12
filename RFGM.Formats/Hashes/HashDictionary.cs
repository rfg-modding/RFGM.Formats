namespace RFGM.Formats.Hashes;

//Has a big list of strings that it hashes at runtime. The hashes are compared to hashes stored in the game files to try and determine the original string
public class HashDictionary
{
    //Each dictionary uses a different hashing function
    private static Dictionary<uint, string> _stringDictionary0 = new();
    private static Dictionary<uint, string> _stringDictionary1 = new();
    private static Dictionary<uint, string> _stringDictionary2 = new();
    private static object _initLock = new();

    public static bool Initialized = false;

    public static void Initialize()
    {
        lock (_initLock)
        {
            if (Initialized)
                return;

            foreach (string str in _sourceStrings)
            {
                _stringDictionary0[Hash.HashVolitionCRC(str, 0)] = str;
                _stringDictionary1[Hash.HashVolitionCRCAlt(str, 1)] = str;
                _stringDictionary2[Hash.HashVolition(str)] = str;
            }
            
            Initialized = true;
        }
    }

    public static string? FindOriginString(uint hash)
    {
        if (!Initialized)
            Initialize();

        if (_stringDictionary0.TryGetValue(hash, out string? result))
            return result;
        if (_stringDictionary1.TryGetValue(hash, out result))
            return result;
        if (_stringDictionary2.TryGetValue(hash, out result))
            return result;

        return null;
    }

    private static string[] _sourceStrings =
    [
        //District names
        "Tutorial", "Parker", "Dust", "Oasis", "Badlands", "Manufacturing", "End Game", "Eos",

        //Class names
        "rfg_mover", "shape_cutter", "object_effect", "district", "multi_object_backpack",
        "multi_object_flag", "multi_object_marker", "object_action_node", "player", "object_patrol",
        "navpoint", "cover_node", "general_mover", "constraint", "item", "npc", "object_squad",
        "ladder", "obj_zone", "object_restricted_area", "object_safehouse", "trigger_region",
        "projectile", "turret", "object_convoy", "object_vehicle_spawn_node", "object_npc_spawn_node",
        "object_squad_spawn_node", "object_turret_spawn_node", "object_spawn_region",
        "object_demolitions_master_node", "object_activity_spawn", "object_area_defense_node",
        "object_bftp_node", "object_house_arrest_node", "object_mission_start_node",
        "player_start", "object_riding_shotgun_node", "object_upgrade_node", "object_debris",
        "object_raid_node", "object_air_strike_defense_node", "object_bounding_box",
        "object_dummy", "object_convoy_end_point", "object_path_road", "object_courier_end_point",
        "object_delivery_node", "object_guard_node", "obj_light", "marauder_ambush_region",
        "object_roadblock_node", "object_ambient_behavior_region", "automobile", "flyer",
        "walker", "weapon",

        //Additional class names
        "CLOE_ONLY", "edit_obj", "obj_chunk_ref", "stitch_piece", "decal", "terrain_decal",
        "layer", "navpoint", "constraint_point", "constraint_hinge", "constraint_hindge",
        "constraint_prism", "constraint_motor", "navmesh_cutout", "invisible_wall",
        "collision_box", "note", "object_foliage", "force_field_door", "effect_streaming_node",

        //Prop names (note: props = properties)
        //Object props
        "pos", "just_pos", "orient", "display_name", "explosion_event", "op",

        //edit_obj
        "name", "game_parent", "game_parent_obj_num", "pos", "orient", "persistent", "auto_restore",
        "mp_modes", "stick_to_terrain", "terrain_morphing", "terrain_falloff", "terrain_radius",

        //Zone props
        "name", "terrain", "district", "zone_hash", "spawn_resource", "spawn_resource_data", "terrain_file_name",
        "ambient_spawn", "invisible_walls", "wind_min_speed", "wind_max_speed", "zone_flags",

        //object_foliage
        "scale", "color_lerp",

        //force_field_door
        "SCALE_Y_BOTTOM",

        //effect_streaming_node
        "no_stub",

        //Invisible wall
        "extrude_dir", "extend_downward", "play_effect", "marker_distance", "marker_density", "marker_model",

        //note
        "note_text",

        //Subzone props
        "zone", "srid", "bmin", "bmax",

        //Cref props (What's a cref? Chunk ref?)
        "display_name", "chunk", "properties", "gameplay_properties", "importance",
        "team", "squads", "squad", "display_name", "building_type", "game_destroyed_pct",
        "display_radius", "min_pa_radius", "revision", "touch_terrain",

        //Stitch piece
        "chunk", "piece_flags", "unwalkable",

        //Navmesh cutout
        "light", "remove_cells", "pf_type",

        //decal
        "bb", "decal_entry",

        //terrain_decal
        "bb", "use_diffuse", "diffuse", "normal", "specular", "spec_power",
        "spec_alpha", "rotation_restriction", "scale_restriction",

        //Object mover props
        "chunk_name", "uid", "chunk_uid", "shape_uid", "props", "gameplay_props",
        "civilian_count", "flags", "building_type_internal", "building_type",
        "force_dynamic", "casts_shadows", "chunk_flags", "dynamic_object",
        "decal_list", "casts_drop_shadows", "importance_internal", "importance",
        "team_internal", "team", "control", "lod_off_distance", "dest_checksum",

        //Rfg mover props
        "rfg_flags", "mtype", "shape", "donor_shape", "shape_state", "shape_state_size",
        "squads", "squad", "show_on_map", "destruct_uid", "damage_percent",
        "game_destroyed_pct", "fully_destroyed_percent", "regenerate", "salvage_material",
        "salvage_num_pieces", "invulnerable", "plume_on_death", "one_of_many", "mining",
        "supply_crate", "stream_out_play_time", "world_anchors", "dynamic_links", "building",

        //General mover props
        "c_obj_id", "mtype", "ctype", "gm_flags", "mass", "com", "tensor", "idx", "sh_ptr",
        "destruct_uid", "original_object", "hitpoints", "dislodge_hitpoints",

        //Constraint props
        "template", "chunk_name", "idx", "host_handle", "child_handle", "host_index",
        "child_index", "host_hk_alt_index", "child_hk_alt_index", "outer_radius",
        "point_constraint_type", "spring_length", "min_x", "max_x", "min_y", "max_y",
        "min_z", "max_z", "threshold", "limited", "max_ang", "min_ang", "friction",
        "max", "min", "angular_speed", "gain",

        //Ladder props
        "ladder_rungs", "ladder_enabled",

        //Object path road props
        "path_road_struct", "path_road_data", "path_road_flags", "next", "prev",
        "ambient", "speed_limit", "persistent", "edf_only", "marauder", "guerrilla",
        "one_way", "other_way", "special", "reinforcements", "GPS",

        //Vehicle props
        "vehicle_type", "type", "veh_flags", "pos_at_ground", "initial_speed",
        "move_at_stream_flags", "immediate_spawn",

        //Cover node props
        "dist", "angle_left", "angle_right", "def_angle_left", "def_angle_right",
        "off_angle_left", "off_angle_right", "stance", "firing_flags", "cover_type",
        "binary_flags", "covernode_data", "cnp",

        //Guard node props
        "guarded_object", "angle_left", "angle_right", "no_defensive_combat", "group_id",
        "rendermesh",

        //Squad spawn node props
        "spawn_set", "spawn_prob", "night_spawn_prob", "default_orders", "default_orders_id",
        "respawn_speed", "raid_spawn", "hands_off_raid_squad", "miner_persona", "safehouse_vip",
        "squad_def", "no_reassignment", "dead_body", "special_vehicle", "special_npc",
        "disable_ambient_parking", "player_vehicle_respawn", "radio_operator",

        //Activity spawn node props
        "activity_type",

        //House arrest node props
        "disabled", "house_arrest_type",

        //Ambient behavior region props
        "behavior", "outer_radius",

        //Dummy object props
        "dummy_type", "dummy_type_internal", "rendermesh",

        //Obj_light props
        "type_enum", //Todo: Use type enum and flags enum from rfg_zonex_format.txt
        "light_type", "clr_orig", "atten_start", "atten_end", "atten_range", "min_clip",
        "max_clip", "clip_mesh", "aspect", "hotspot_size", "hotspot_falloff_size",
        "fvec", "light_flags", "parent_name", "show_attenuation_spheres", "show_clip_box",
        "bb",

        //Navpoint props
        "navpoint_type", "nav_type", "navlinks", "ref", "link_status", "cross_zone_link",
        "cross_zone_num", "dont_follow_road", "ignore_lanes", "outer_radius", "speed_limit",
        "navpoint_data", "npp", "nav_orient", "branch_type", "links",

        //Action node props
        "links", "obj_links", "animation_type", "high_priority", "infinite_duration",
        "disabled", "run_to", "rendermesh", "tint", "outer_radius", "rotation_restriction",

        //Patrol node props
        "patroled_object", //Game spelled it that way. I know how to spell, I swear!
        "patrol_start", "looping_patrol", "convoy_patrol", "courier_patrol",
        "ASD_truck_partol", //What the fuck is this spelling rfg?
        "override_patrol", "combat_ready", "marauder_raid", "no_check_in", "orders",
        "squad_def_index", "spawn_status", "original_pos", "respawn",

        //Roadblock node props
        "squad_vehicle", "vehicle_type", "roadblock_type", "use_object_orient", "bb",

        //Squad object props
        "vehicle_ref", "squad_flags", "npc_flags", "human_flags", "vehicle_flags",
        "vehicle_hp_mult", "node_handle", "map_icon_type",

        //Convoy end node props
        "convoy_type", "convoy_disabled", "convoy_suspended", "home_district",
        "activity_completed",

        //Convoy object props
        "convoy_def", "convoy_patrol",

        //Courier end point object props
        "courier_type", "end_game_only", "courier_completed",

        //Courier object props
        "courier_def", "courier_node", "courier_patrol", "courier_start",

        //Restricted area props
        "disabled", "allow_ambient_peds", "yellow_num_points", "yellow_polygon",
        "yellow_num_triangles", "yellow_triangles", "warning_num_points", "warning_polygon",
        "warning_num_triangles", "warning_triangles", "warning_radius",
        "bb", "area_type", //Maybe they had a restricted area drawing tool?

        //Raid node props
        "raid_type", "persistent", "rendermesh", "tint",

        //Demolitions master node props
        "demolitions_master_type", "demolitions_master_best_time",
        "demolitions_master_complete", "demolitions_master_discovered",
        "demolitions_master_par_beaten", "demolitions_master_times_completed",

        //Air strike defense node props
        "air_strike_defense_type",

        //Riding shotgun node props
        "riding_shotgun_type", "riding_shotgun_completed",

        //Area defense node props
        "area_defense_type", "area_defense_completed",

        //Delivery node props
        "delivery_type", "pair_number", "start_node", "delivery_flags",
        "best_time",

        //Bftp node props
        "tag_node", "pair_number", "bftp_flags", "collected", "enabled_bomb",

        //Marauder ambush region props
        "bb", "enabled", "day_trigger_prob", "night_trigger_prob",
        "min_ambush_squads", "max_ambush_squads",

        //Trigger region props
        "outer_radius", "trigger_shape", "region_type", "region_kill_type",
        "trigger_flags",

        //Projectile object props
        "prj_owner_h", "prj_w_info", "prj_pflags", "prj_target", "prj_vel",
        "prj_cur_vel", "prj_end_pos", "prj_duration", "prj_age_timer",
        "prj_start_pos", "prj_max_vel", "prj_detonate_ts", "prj_client_nonce",
        "prj_fired_in_slew",

        //Turret props
        "tinfo", "turret_type", "attach_tag", "rotation_limit", "weapon_owner",
        "rendermesh_weapon", "gun_pos", "gun_orient",

        //Npc props
        "npc_type", "npc_flags",

        //Human props
        "persona_name", "npc_info", "importance", "priority", "manager",
        "creator", "in_vehicle", "load_out", "human_flags", "ambient_behavior",
        "h_int_exp_ts", "h_ack_ts", "h_health_ts", "h_look_at_fin_ts",
        "h_reported_ts", "h_restore_inv", "nav_cell",

        //Multi-weapon props
        "respawns",

        //Multi-team/player props
        "game_data_handle", "multi-team",

        //Item props
        "item_type", "item_info", "flags", "ctype", "mass_override",
        "deletion_timer", "item_prop_type", "respawn", "respawns",
        "preplaced",

        //Obj item props (unsure of difference between this and normal objects)
        "item_srid", "salvage_material",

        //Collision box
        "material",

        //Effect object props
        "effect_type", "looping", "host_handle", "host_tag", "position",
        "orientation", "volume", "attached_to_camera",
        "attached_to_camera_orient", "visual", "sound", "sound_alr",
        "visual_handle", "sound_handle", "sound_alr_handle", "source_handle",
        "inner_radius", "outer_radius",

        //Streamed effect props
        "streamed_effect",

        //Weapon props
        "weapon_type", "wep_info", "wep_info_name", "wep_mag_rnds",
        "wep_res_rnds", "wep_flags", "wep_refire_delay",
        "wep_reload_delay", "wep_last_fired_timer",

        //Debris object props
        "c_info",

        //Character instance props
        "render_scale", "char_scale",

        //Skybox props
        "c_info",

        //Player start props
        "indoors", "indoor", "mp_team", "initial_spawn", "respawn",
        "checkpoint_respawn", "activity_respawn", "mission_info",

        //Object spawn region props
        "bb", "team", "npc_only", "vehicle_only",

        //Bounding box object props
        "bounding_box_type",

        //OCC props (obj_occluder)
        "oc_shape", "bb",

        //Shape cutter props
        "exp_info", "source", "owner", "flags",
        "shape_cutter_type", "outer_radius", "w_info",

        //Mission start node props
        "mission_info", "enabled", "autostart",

        //Safehouse props
        "zone_serialized", "player_start", "vehicle_spawns",
        "buildings", "trigger_regions", "enabled",
        "disabled_flags", "visible", "display_name_tag",
        "display_name_hash",

        //District props
        "control", "morale", "control_max", "morale_max",
        "grid_index", "index", "grid_color", "VFX_tint",
        "liberated", "liberated_play_line", "district_flags",

        //Multi-marker props
        "bb", "marker_type", "mp_team", "priority", "backpack_type",
        "num_backpacks", "random_backpacks", "group",

        //Multi flag capture zone props
        "mp_team",

        //Ambient spawn node props
        "ped_spawn_density", "ped_spawn_group", "veh_spawn_density",
        "veh_spawn_group", "driver_spawn_group",

        //Object upgrade node props
        "upgrade_type",

        //Mp flag props
        "mp_team",

        //Backpack object props
        "backpack_type",

        //Material names
        "therm_rocket_launcher_high",
        "sniper_rifle_high",
        "sledge_war_high",
        "sledge_upgrade_3_high",
        "sledge_upgrade_2_high",
        "sledge_upgrade_1_high",
        "sledge_tit_high",
        "sledge_stu_high",
        "sledge_sil_high",
        "sledge_pla_high",
        "sledge_ost_high",
        "sledge_gol_high",
        "sledge_bro_high",
        "sledge_bat_high",
        "sledge_axe_high",
        "sledgehammer_high",
        "singularity_grenade_high",
        "rpg_high",
        "rocket_turret_high",
        "remote_detonator_high",
        "rail_driver_high",
        "proximity_mine_high",
        "pistol_high",
        "nanorifle_high",
        "marauder_shotgun_high",
        "marauder_hammer_high",
        "marauder_gutter_high",
        "hmg_weapon_high",
        "harpoon_turret_high",
        "gt_weapon_high",
        "grinder_high",
        "golden_hammer_high",
        "gauss_rifle_high",
        "enforcer_high",
        "edfrt_weapon_high",
        "autoblaster_high",
        "assault_rifle_high",
        "arc_welder_high",
        "therm_rocket_launcher_high.mat_pc",
        "sniper_rifle_high.mat_pc",
        "sledge_war_high.mat_pc",
        "sledge_upgrade_3_high.mat_pc",
        "sledge_upgrade_2_high.mat_pc",
        "sledge_upgrade_1_high.mat_pc",
        "sledge_tit_high.mat_pc",
        "sledge_stu_high.mat_pc",
        "sledge_sil_high.mat_pc",
        "sledge_pla_high.mat_pc",
        "sledge_ost_high.mat_pc",
        "sledge_gol_high.mat_pc",
        "sledge_bro_high.mat_pc",
        "sledge_bat_high.mat_pc",
        "sledge_axe_high.mat_pc",
        "sledgehammer_high.mat_pc",
        "singularity_grenade_high.mat_pc",
        "rpg_high.mat_pc",
        "rocket_turret_high.mat_pc",
        "remote_detonator_hig.mat_pch",
        "rail_driver_high.mat_pc",
        "proximity_mine_high.mat_pc",
        "pistol_high.mat_pc",
        "nanorifle_high.mat_pc",
        "marauder_shotgun_high.mat_pc",
        "marauder_hammer_high.mat_pc",
        "marauder_gutter_high.mat_pc",
        "hmg_weapon_high.mat_pc",
        "harpoon_turret_high.mat_pc",
        "gt_weapon_high.mat_pc",
        "grinder_high.mat_pc",
        "golden_hammer_high.mat_pc",
        "gauss_rifle_high.mat_pc",
        "enforcer_high.mat_pc",
        "edfrt_weapon_high.mat_pc",
        "autoblaster_high.mat_pc",
        "assault_rifle_high.mat_pc",
        "arc_welder_high.mat_pc",
        "sledgehammer",
        "sledgehammer.csmesh_pc",

        //Material constant names
        "saturation",
        "__dummy0",
        "_dummy0",
        "_dummy1",
        "dummy0",
        "dummy1",
        "dummy2",
        "dummy3",
        "dummy4",
        "time",
        "ambient",
        "diffuse",
        "backAmbient",
        "fogColor",
        "shadowFadeParams",
        "targetDimensions",
        "indirectLightParams",
        "substanceSpecAlphaScale",
        "substanceSpecPowerScale",
        "substanceDiffuseScale",
        "substanceFresnelAlphaScale",
        "substanceFresnelPowerScale",
        "glassFresnelBias",
        "glassFresnelPower",
        "glassReflectionEnabled",
        "glassDirtEnabled",
        "lightColor",
        "attenuation",
        "hotspot",
        "Tint_color",
        "detailMapTilingScaleBiasBlend",
        "Terrain_layer_spec_alpha",
        "Terrain_layer_spec_power",
        "Diffuse_color_0",
        "Diffuse_color_1",
        "Diffuse_color_2",
        "Diffuse_color_3",
        "projTM",
        "viewTM",
        "camRot",
        "camPos",
        "fogDistance",
        "renderOffset",
        "renderOffset_dummy0",
        "lightDirOrPos",
        "spotLightDir",
        "Translation",
        "Terrain_subzone_offset",
        "Terrain_subzone_offset_Dummy0",
        "Diffuse_Color",
        "Specular_Color",
        "Decal_Map_1_Opacity",
        "Decal_Map_1_TilingU",
        "Decal_Map_1_TilingV",
        "Decal_Map_2_Opacity",
        "Normal_Map_Height",
        "Normal_map_TilingU",
        "Normal_map_TilingV",
        "Self_Illumination",
        "Specular_Alpha",
        "Specular_Map_TilingU",
        "Specular_Map_TilingV",
        "Specular_Power",
        "tm",
        "objTM",
        "instanceData",
        "Terrain_subzone_offset",
        "Terrain_subzone_offset_Dummy0",
        "Diffuse_Color",
        "Decal_Map_2_Opacity",
        "invProjTM",
        "zDimensions",
        "effectOpacity",
        "effectOpacity_Dummy0",
        "effectAmbient",
        "effectMaterialTint",
        "effectUvAnimTiling",
        "effectUvAnimTiling_Dummy0",
        "effectUvAnimTiling_Dummy1",
        "bones",
        "Terrain_layer0_scalwe_offset",
        "Terrain_layer0_scale_offset",
        "Terrain_layer1_scale_offset",
        "Terrain_layer2_scale_offset",
        "Terrain_layer3_scale_offset",
        "Camera_velocity",
        "Scale",
        "Mem_offset",
        "Mem_base",
        "World_xform",
        "World_xform0",
        "World_xform1",
        "World_xform2",
        "World_xform3",
        "xform0",
        "xform1",
        "xform2",
        "xform3",
        "invProjTM",
        "zDimensions",
        "effectOpacity",
        "effectAmbient",
        "effectMaterialTint",
        "effectUvAnimTiling",
        "rl_terrain_sidemap_single_t",
        "rl_terrain_sidemap_t",
        "rl_terrain_road_layer4_s",
        "rl_terrain_road_layer4_bs",
        "rl_terrain_road_layer3_bs",
        "rl_terrain_road_layer3_s",
        "rl_terrain_road_layer2_bs",
        "rl_terrain_road_layer2_s",
        "rl_terrain_landmark_lod_t",
        "rl_terrain_low_lod_t",
        "rl_terrain_height_mesh_lowlod_t",
        "rl_terrain_height_mesh_lowlod_ts",
        "rl_terrain_height_mesh_blend_t",
        "rl_terrain_height_mesh_blend_ts",
        "rl_terrain_height_mesh_t",
        "rl_terrain_height_mesh_ts",
        "rl_terrain_depth_only_t",
        "rl_terrain_depth_only_ts",
        "cloe_terrain_road_s",
        "cloe_landmark_s",
        "cloe_navmesh_s",
        "cloe_building_s",
        "rl_terrain_sidemap_single_t.fxo_kg",
        "rl_terrain_sidemap_t.fxo_kg",
        "rl_terrain_road_layer4_s.fxo_kg",
        "rl_terrain_road_layer4_bs.fxo_kg",
        "rl_terrain_road_layer3_bs.fxo_kg",
        "rl_terrain_road_layer3_s.fxo_kg",
        "rl_terrain_road_layer2_bs.fxo_kg",
        "rl_terrain_road_layer2_s.fxo_kg",
        "rl_terrain_landmark_lod_t.fxo_kg",
        "rl_terrain_low_lod_t.fxo_kg",
        "rl_terrain_height_mesh_lowlod_t.fxo_kg",
        "rl_terrain_height_mesh_lowlod_ts.fxo_kg",
        "rl_terrain_height_mesh_blend_t.fxo_kg",
        "rl_terrain_height_mesh_blend_ts.fxo_kg",
        "rl_terrain_height_mesh_t.fxo_kg",
        "rl_terrain_height_mesh_ts.fxo_kg",
        "rl_terrain_depth_only_t.fxo_kg",
        "rl_terrain_depth_only_ts.fxo_kg",
        "cloe_terrain_road_s.fxo_kg",
        "cloe_landmark_s.fxo_kg",
        "cloe_navmesh_s.fxo_kg",
        "cloe_building_s.fxo_kg",

        //Shader/material constants. There's likely some duplicates from previously defined hash strings
        "g_skinningVertexConstantsbuffer",
        "g_decalVertexConstantsbuffer",
        "g_instanceVertexConstantsbuffer",
        "projTM",
        "viewTM",
        "camRot",
        "camPos",
        "time",
        "fogDistance",
        "renderOffset",
        "renderOffset_dummy0",
        "targetDimensions",
        "g_lightingVertexConstants",
        "lightDirOrPos",
        "__dummy0",
        "spotLightDir",
        "attenuation",
        "hotspot",
        "g_skinningVertexConstants",
        "bones",
        "g_decalVertexConstants",
        "xvec",
        "xvec_Dummy0",
        "yvec",
        "yvec_Dummy0",
        "zvec",
        "zvec_Dummy0",
        "pos",
        "pos_Dummy0",
        "tint",
        "params1",
        "params2",
        "g_instanceVertexConstants",
        "objTM",
        "instanceData",
        "Terrain_subzone_offset",
        "Terrain_subzone_offset_Dummy0",
        "g_commonVertexConstantsbuffer",
        "g_lightingVertexConstantsbuffer",
        "Normal_Map_sampler",
        "Decal_diffuse_map_sampler",
        "Decal_Map_1_sampler",
        "Shadow_map_sampler_sampler",
        "Normal_Map_texture",
        "Decal_diffuse_map_texture",
        "Decal_Map_1_texture",
        "Shadow_map_sampler_texture",
        "g_commonFragmentConstantsbuffer",
        "g_lightingFragmentConstantsbuffer",
        "g_materialConstantsbuffer",
        "g_instanceFragmentConstantsbuffer",
        "g_commonFragmentConstants",
        "saturation",
        "dummy0",
        "dummy1",
        "ambient",
        "backAmbient",
        "fogColor",
        "shadowFadeParams",
        "indirectLightParams",
        "substanceSpecAlphaScale",
        "substanceSpecPowerScale",
        "substanceDiffuseScale",
        "substanceFresnelAlphaScale",
        "substanceFresnelPowerScale",
        "glassFresnelBias",
        "glassFresnelPower",
        "glassReflectionEnabled",
        "glassDirtEnabled",
        "dummy2",
        "dummy3",
        "dummy4",
        "g_lightingFragmentConstants",
        "lightColor",
        "g_instanceFragmentConstants",
        "alphaTestRef",
        "doExplicitAlphaTest",
        "doExplicitAlphaTest_dummy0",
        "doExplicitAlphaTest_dummy1",
        "g_materialConstants",
        "Diffuse_Color",
        "Glow_color",
        "Glow_fade_time",
        "Normal_Map_Height",
        "_dummy0",
        "_dummy1",
        "_dummy2",
        "Cap_value",
        "Normalize_flag",
        "Offset_value",
        "Terrain_scale",
        "Diffuse_Map_sampler",
        "Diffuse_Map_texture",
        "Landmark_Map_Size",
        "max_value",
        "min_value",
        "Channel_swizzle",
        "Inv_channel_swizzle",
        "Alpha_mask_0",
        "Alpha_mask_1",
        "Alpha_mask_2",
        "Alpha_mask_3",
        "Diffuse_color_0",
        "Diffuse_color_1",
        "Diffuse_color_2",
        "Diffuse_color_3",
        "Terrain_layer0_scale_offset",
        "Terrain_layer1_scale_offset",
        "Terrain_layer2_scale_offset",
        "Terrain_layer_spec_alpha",
        "Terrain_layer_spec_power",
        "Tile",
        "Do_cutout",
        "Edge_fade",
        "Use_spec_map",
        "Alpha_Map_0_sampler",
        "Normal_Map_0_sampler",
        "Normal_Map_1_sampler",
        "Normal_Map_2_sampler",
        "Normal_Map_3_sampler",
        "Diffuse_Map_0_sampler",
        "Diffuse_Map_1_sampler",
        "Diffuse_Map_2_sampler",
        "Diffuse_Map_3_sampler",
        "Alpha_map_1_sampler",
        "Alpha_map_2_sampler",
        "Cutout_map_sampler",
        "Alpha_Map_0_texture",
        "Normal_Map_0_texture",
        "Normal_Map_1_texture",
        "Normal_Map_2_texture",
        "Normal_Map_3_texture",
        "Diffuse_Map_0_texture",
        "Diffuse_Map_1_texture",
        "Diffuse_Map_2_texture",
        "Diffuse_Map_3_texture",
        "Alpha_map_1_texture",
        "Alpha_map_2_texture",
        "Cutout_map_texture",
        "base_sampler_sampler",
        "base_sampler_texture",
        "sampling_color_trans",
        "sampling_factor",
        "sampling_taps",
        "sampling_divisor",
        "Specular_Color",
        "Decal_Map_1_OffsetU",
        "Decal_Map_1_OffsetV",
        "Decal_Map_1_Opacity",
        "Decal_Map_1_TilingU",
        "Decal_map_1_TilingV",
        "Decal_Map_2_OffsetU",
        "Decal_Map_2_OffsetV",
        "Decal_Map_2_Opacity",
        "Diffuse_Map_OffsetU",
        "Diffuse_Map_OffsetV",
        "illum_cycle",
        "Normal_map_TilingU",
        "Normal_map_TilingV",
        "Self_Illumination",
        "Specular_Alpha",
        "Specular_Map_TilingU",
        "Specular_Map_TilingV",
        "Specular_Power",
        "Decal_Map_2_sampler",
        "Specular_Map_sampler",
        "Decal_Map_2_texture",
        "Specular_Map_texture",
        "Metal_Specular_Color",
        "Diffuse_Map_TilingU",
        "Diffuse_Map_TilingV",
        "Metal_Base_Shine",
        "Metal_Specular_Alpha",
        "Metal_Specular_Power",
        "BRDF_Diff_1",
        "BRDF_Diff_2",
        "BRDF_Diff_3",
        "Specular_falloff",
        "BRDF_Spec_1",
        "BRDF_Spec_2",
        "BRDF_Spec_3",
        "Environment_Map_Opacity",
        "Environment_Map_sampler",
        "Environment_Map_texture",
        "Decal_Map_1_Overlay",
        "BRDF_Diff_Map_sampler",
        "BRDF_Spec_Map_sampler",
        "BRDF_Diff_Map_texture",
        "BRDF_Spec_Map_texture",
        "Opacity",
        "BRDF_Map_TilingU",
        "BRDF_Map_TilingV",
        "BRDF_Mask_Scale",
        "BRDF_Mask_Map_sampler",
        "BRDF_Mask_Map_texture",
        "Alpha_Map_sampler",
        "Alpha_Map_texture",
        "Alpha_Picker_Sub_B",
        "Alpha_Picker_Sub_G",
        "Alpha_Picker_Sub_R",
        "Alpha_Picker_Top_B",
        "Alpha_Picker_Top_G",
        "Alpha_Picker_Top_R",
        "Shift_U",
        "Shift_V",
        "Reveal_time_ms",
        "Crack_pt_1",
        "Crack_pt_2",
        "Crack_pt_3",
        "Crack_start_pct",
        "Decal_Map_1_TilingV",
        "Clearcoat_Color",
        "Clearcoat_Dirt_Threshold",
        "Clearcoat_Falloff_Alpha",
        "Clearcoat_Falloff_Power",
        "Clearcoat_Specular_Alpha",
        "Clearcoat_Specular_Power",
        "reflection_map_sampler",
        "reflection_map_texture",
        "Skin_Color_Hemoglobin",
        "Skin_Color_Melanin",
        "Skin_Color_Unscattered",
        "Skin_Translucent_Power",
        "Skin_Translucent_Ramp_Off",
        "Specular_Alpha2",
        "Diffuse_Color_F",
        "Foliage_color",
        "FadeInE",
        "FadeInS",
        "FadeOutE",
        "FadeOutS",
        "Foliage_alphaClamp",
        "Foliage_EdgeFade",
        "Diffuse_Map_F_sampler",
        "Diffuse_Map_F_texture",
        "Heat_map_sampler",
        "Heat_map_texture",
        "Diffuse_Color2",
        "Alpha_1",
        "Alpha_2",
        "Diffuse_Map2_TilingU",
        "Diffuse_Map2_TilingV",
        "FringeMap_sampler",
        "FringeMap_texture",
        "mp_rim_color",
        "mp_fuel_gauge_max",
        "Diffuse_map_TilingU",
        "Diffuse_map_TilingV",
        "Orbital_map_sampler",
        "Orbital_map_texture",
        "Orbital_tint",
        "BRDF_Glancing_1",
        "Skin_Specular_Gloss",
        "Layer_strengths",
        "Scale",
        "skybox_position",
        "TOD_Light_Color_Front",
        "TOD_Light_Color_Rear",
        "TOD_Light_Dir",
        "_dummy3",
        "Cloud_U_Offset",
        "Cloud_Fade_Height",
        "Cloud_Full_Height",
        "Offset",
        "Rimlight_power",
        "Rimlight_scale",
        "_dummy4",
        "Layer01_map_sampler",
        "Layer23_map_sampler",
        "Layer01_map_texture",
        "Layer23_map_texture",
        "Mountain_color_back",
        "Mountain_color_front",
        "Mountain_fog_color",
        "Mountain_fog_density",
        "Meteor_strength",
        "Draw_distance",
        "Star_strength",
        "Star_diffuse_sampler",
        "Star_diffuse_texture",
        "Hemisphere_colors",
        "Diffuse_Map2_sampler",
        "Normal_Map2_sampler",
        "Diffuse_Map2_texture",
        "Normal_Map2_texture",
        "Normal_Map_Height2",
        "Specular_Power2",
        "Diffuse_Alpha",
        "Diffuse_Map_Amount",
        "Sweep_Color_A",
        "Sweep_Color_B",
        "Diffuse_Map_Luminosity",
        "Diffuse_Scroll_Speed_U",
        "Diffuse_Scroll_Speed_V",
        "Distort_Map_TileU",
        "Distort_Map_TileV",
        "Distort_Map_sampler",
        "Distort_Map_texture",
        "Glow_Color_1",
        "Alpha_Map_TileU",
        "Alpha_Map_TileV",
        "Edge_Glow_pct",
        "Glow_Color_2",
        "projTM_inv",
        "Inv_objTM",
        "Camera_lookat_world",
        "Eye_pos_local",
        "Eye_pos_world",
        "height_fall_off_base",
        "Height_fall_off_dir_scaled",
        "_dummy5",
        "_dummy6",
        "Outside_soft_edges",
        "Fog_density",
        "Near_far_clip_scale",
        "Viewer_is_outside",
        "_dummy7",
        "_dummy8",
        "Depth_sampler_sampler",
        "Depth_sampler_texture",
        "source_sampler",
        "source_texture",
        "backbuffer_texture_sampler",
        "backbuffer_texture_texture",
        "Tint",
        "Saturation",
        "g_particleVertexConstantsbuffer",
        "g_particleVertexConstants",
        "invProjTM",
        "zDimensions",
        "effectOpacity",
        "effectOpacity_Dummy0",
        "effectAmbient",
        "effectMaterialTint",
        "effectUvAnimTiling",
        "effectUvAnimTiling_Dummy0",
        "effectUvAnimTiling_Dummy1",
        "g_particleFragmentConstantsbuffer",
        "g_particleFragmentConstants",
        "colorCorrectionMatrix",
        "colorCorrectionMatrix_Dummy0",
        "effectDiffuse",
        "effectLightDir",
        "effectLightDir_dummy0",
        "coc_sampler_sampler",
        "depth_sampler_sampler",
        "coc_sampler_texture",
        "depth_sampler_texture",
        "Depth_sampling_offsets",
        "Focal_params",
        "Near_clip_params",
        "Override_blur_percent",
        "distortion_sampler_sampler",
        "blurred_sampler_sampler",
        "distortion_sampler_texture",
        "blurred_sampler_texture",
        "Distortion_scale",
        "Blur_scale",
        "Sampling_offsets",
        "mask_sampler_sampler",
        "mask_sampler_texture",
        "input_map_sampler",
        "input_map_texture",
        "Separated_axis",
        "KEEN_VERTEX_OUTPUT_TEXCOORD",
        "Base_sampler_sampler",
        "Base_sampler_texture",
        "Bloom_curve_values",
        "Luminance_conversion",
        "Adapt_time",
        "Bloom_amount",
        "Brightpass_offset",
        "Brightpass_threshold",
        "Eye_adaption_amount",
        "Eye_adaption_base",
        "Eye_fade_max",
        "Eye_fade_min",
        "HDR_level",
        "Lum_offset",
        "Lum_range",
        "Luminance_mask_max",
        "Luminance_max",
        "Luminance_min",
        "Debug_avg_lum",
        "Debug_bloom_buffer",
        "Debug_lummap",
        "Use_hdr_level",
        "Lum_adapt_sampler_sampler",
        "Lum_adapt_sampler_texture",
        "Light_shafts_brightpass_sampler_sampler",
        "Light_shafts_brightpass_sampler_texture",
        "particle_brightpass_sampler_sampler",
        "particle_brightpass_sampler_texture",
        "Bloom_add_texture_0_sampler_sampler",
        "Bloom_add_texture_1_sampler_sampler",
        "Bloom_add_texture_0_sampler_texture",
        "Bloom_add_texture_1_sampler_texture",
        "Particle_sampler_sampler",
        "Particle_sampler_texture",
        "Bloom_stage_0_sampler_sampler",
        "Bloom_stage_0_sampler_texture",
        "Bloom_stage_1_sampler_sampler",
        "Bloom_stage_2_sampler_sampler",
        "Bloom_stage_1_sampler_texture",
        "Bloom_stage_2_sampler_texture",
        "Bloom_stage_3_sampler_sampler",
        "Bloom_stage_4_sampler_sampler",
        "Bloom_stage_3_sampler_texture",
        "Bloom_stage_4_sampler_texture",
        "Mask_sampler_sampler",
        "Sun_sampler_sampler",
        "Mask_sampler_texture",
        "Sun_sampler_texture",
        "Logluv_sampler_sampler",
        "Logluv_sampler_texture",
        "Curr_to_prev",
        "Radial_blur_position",
        "Blur_clamp",
        "Radial_blur_radius",
        "dummy_var",
        "Outline_sampler_sampler",
        "Outline_sampler_texture",
        "Outline_color_0",
        "Outline_color_1",
        "extrude_amount",
        "particle_sampler_sampler",
        "edgemap_sampler_sampler",
        "particle_sampler_texture",
        "edgemap_sampler_texture",
        "backbuffer_sampler_sampler",
        "backbuffer_sampler_texture",
        "bloom_sampler_sampler",
        "bloom_sampler_texture",
        "Z_min_and_range",
        "Parametric_particle_constants",
        "Depth_buffer_sampler",
        "Particle_fog_sampler_sampler",
        "Depth_buffer_texture",
        "Particle_fog_sampler_texture",
        "Y_tex_sampler",
        "Cr_tex_sampler",
        "Cb_tex_sampler",
        "Y_tex_texture",
        "Cr_tex_texture",
        "Cb_tex_texture",
        "texel_offset",
        "uv_xform",
        "Focal_length",
        "Packed_params",
        "Resolution",
        "Depth_clip_params",
        "Angle_bias",
        "Num_steps",
        "Depth_sampler",
        "Random_sampler",
        "Depth_texture",
        "Random_texture",
        "Terrain_layer3_scale_offset",
        "Combine_map_sampler",
        "Combine_map_texture",
        "Edge_fade__use_spec_map",
        "Road_subzone_offset",
        "AA",
        "Terrain_layer0_xform",
        "Terrain_layer1_xform",
        "Layer1_spec_alpha",
        "Layer1_spec_power",
        "Terrain_layer2_xform",
        "Terrain_layer3_xform",
        "Mem_base",
        "World_xform",
        "Camera_velocity",
        "Mem_offset",
        "base_texture_sampler",
        "base_texture_texture",
        "unused_var",
        "Fog_of_war_mask_sampler",
        "Fog_texture_sampler",
        "Fog_of_war_mask_texture",
        "Fog_texture_texture",
        "Base_color",
        "Fog_layer1_offset_scale",
        "Fog_layer2_offset_scale",
        "Fog_layer1_tint",
        "Fog_layer2_tint",
        "Fog_opacity",
        "Faction_Colors",
        "Building_texture_sampler",
        "Building_alpha_map_sampler",
        "Building_texture_texture",
        "Building_alpha_map_texture",
        "Terrain_combine_map_sampler",
        "Terrain_overlay_map_sampler",
        "Terrain_combine_map_texture",
        "Terrain_overlay_map_texture",
        
        //The hash of this is the vanilla DistrictHash for all the MP and WC maps
        "default"
    ];
}