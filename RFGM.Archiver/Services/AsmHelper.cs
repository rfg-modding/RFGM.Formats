using System.Collections.Immutable;

namespace RFGM.Archiver.Services;

public static class AsmHelper
{
    /// <summary>
    /// Recognize standalone maps: mp_rift.vpp_pc, mpdlc_division.vpp_pc, mp_common.vpp_pc, wc10.vpp_pc, wcdlc7.vpp_pc, 1.vpp_pc (custom maps)
    /// </summary>
    public static bool IsMultiplayerVpp(string vpp) => vpp.StartsWith("mp", StringComparison.OrdinalIgnoreCase)
                                                       || vpp.StartsWith("wc", StringComparison.OrdinalIgnoreCase)
                                                       || int.TryParse(vpp.Split('.').First(), out _);

    /// <summary>
    /// Vanilla files not referenced in any asm that should not be included as new primitives
    /// </summary>
    public static bool IsIgnoredFile(string vpp, string fileName)
    {
        var value = fileName.ToLowerInvariant();
        if (IgnoredVpps.Contains(vpp))
        {
            return true;
        }

        if (IgnoredSuffixes.Any(x => value.EndsWith(x)))
        {
            return true;
        }

        //p_*.rfgzone_pc
        if (value.StartsWith("p_") && value.EndsWith(".rfgzone_pc"))
        {
            return true;
        }

        // this vpp has a lot of clutter
        if (vpp.Equals("interface.vpp_pc", StringComparison.OrdinalIgnoreCase))
        {
            return value.StartsWith("font") || IgnoredInterfaceFiles.Contains(value);
        }

        return false;
    }

    /// <summary>
    /// Archives that store files not referenced in any asm
    /// </summary>
    private static readonly ImmutableHashSet<string> IgnoredVpps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "steam.vpp_pc",
        "sounds_r.vpp_pc",
        "anims.vpp_pc",
        "dlcp01_anims.vpp_pc",
        "dlcp01_misc.vpp_pc",
        "dlcp02_misc.vpp_pc",
        "dlcp03_misc.vpp_pc",
        "misc.vpp_pc",
        "dlcp01_voices_en_us.vpp_pc",
        "voices_de_de.vpp_pc",
        "voices_en_us.vpp_pc",
        "voices_es_es.vpp_pc",
        "voices_fr_fr.vpp_pc",
        "voices_it_it.vpp_pc",
        "voices_ru_ru.vpp_pc",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Files that are not referenced in any asm
    /// </summary>
    private static readonly ImmutableHashSet<string> IgnoredSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".asm_pc",
        ".str2_pc",
        ".precache",
        ".scriptx",
        ".xtbl",
        "_containers.cpeg_pc",
        // all gpu files
        ".gefct_pc",
        ".gchk_pc",
        ".gstch_pc",
        ".gsmesh_pc",
        ".gvbm_pc",
        ".gpeg_pc",
        ".gcmesh_pc",
        ".gcar_pc",
        ".gterrain_pc",
        ".gtmesh_pc",
        ".gtmesh_pc",

    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Files in interface vpp that are not referenced in any asm
    /// </summary>
    private static readonly ImmutableHashSet<string> IgnoredInterfaceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "compass.cvbm_pc",
        "rfg_ui_controls_sony.vint_doc",
        "rfg_ui_controls_xbox.vint_doc",
        "rfg_ui_hud_asd.vint_doc",
        "rfg_ui_hud_health_sony.vint_doc",
        "rfg_ui_menu_sony.vint_doc",
        "ug-debug.cvbm_pc",
        "ug-debug.vf3_pc",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Archives that store files directly without str2 containers
    /// </summary>
    public static readonly ImmutableHashSet<string> PlainVpps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "mp_common.vpp_pc",
        "dlc01_precache.vpp_pc",
        "terr01_precache.vpp_pc"
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Vanilla game has invalid primitive name that can be fixed
    /// </summary>
    public static readonly IReadOnlyDictionary<string,string> FixableVanillaNames = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
    {
        {"interface-badges.cvbm_pc","interface-badges.cpeg_pc"}
    };

    /// <summary>
    /// Vanilla game has primitives that don't exist as files
    /// </summary>
    public static readonly ImmutableHashSet<string> MissingVanillaPrimitives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "effects.vpp_pc/vfx_containers.asm_pc/des_gravel_shockwave_a.cefct_pc/des_gravel_shockwave_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/des_foliage_shockwave_a.cefct_pc/des_foliage_shockwave_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/des_foliage_shockwave_a.cefct_pc/vfx_wood_pulp_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/des_foliage_shockwave_a.cefct_pc/vfx_wood_twig_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/des_foliage_shockwave_a.cefct_pc/vfx_leaf_bit_02.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/des_foliage_shockwave_a.cefct_pc/vfx_leaf_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/des_foliage_shockwave_a.cefct_pc/vfx_wood_twig_02.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dust_rolling_a.cefct_pc/dust_rolling_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/red_light_pulse_a.cefct_pc/red_light_pulse_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/smoke_raid_marker.cefct_pc/smoke_raid_marker.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dust_skirting_a.cefct_pc/dust_skirting_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dust_skirting_a.cefct_pc/vfx_blood_decal_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_transformer_a.cefct_pc/dth_transformer_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_propane_tank_sm_shards_a.cefct_pc/dth_propane_tank_sm_shards_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/andy_test.cefct_pc/andy_test.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_heat_vent_md_a.cefct_pc/dth_heat_vent_md_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_edf_door_glitch_a.cefct_pc/env_edf_door_glitch_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_edf_door_glitch_a.cefct_pc/vfx_edf_logo_02.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_edf_door_glitch_a.cefct_pc/vfx_edf_logo_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_edf_door_a.cefct_pc/env_edf_door_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_edf_door_a.cefct_pc/vfx_edf_logo_02.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_edf_door_a.cefct_pc/vfx_edf_logo_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_miner_door_a.cefct_pc/env_miner_door_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_miner_door_glitch_a.cefct_pc/env_miner_door_glitch_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_smoke_misc_a.cefct_pc/env_smoke_misc_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_smoke_misc_c.cefct_pc/env_smoke_misc_c.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_smoke_misc_d.cefct_pc/env_smoke_misc_d.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_smoke_misc_e.cefct_pc/env_smoke_misc_e.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_dust_blowing_a.cefct_pc/env_dust_blowing_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_dust_blowing_small_a.cefct_pc/env_dust_blowing_small_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_dust_blowing_b.cefct_pc/env_dust_blowing_b.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_dust_blowing_c.cefct_pc/env_dust_blowing_c.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_generic_sm_exp_a.cefct_pc/dth_generic_sm_exp_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_generic_md_exp_a.cefct_pc/dth_generic_md_exp_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/chad_test.cefct_pc/chad_test.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_lab_screen_a.cefct_pc/env_lab_screen_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_lab_screen_a.cefct_pc/vfx_grid_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_lab_screen_a.cefct_pc/vfx_grad_disc_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_bottle_b.cefct_pc/dth_bottle_b.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_chimney_fire_a.cefct_pc/env_chimney_fire_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_light_blink_red_a.cefct_pc/env_light_blink_red_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_xray_flicker_a.cefct_pc/env_xray_flicker_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_xray_flicker_a.cefct_pc/vfx_grad_disc_01.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/wep_edf_artillery_smoke_a.cefct_pc/wep_edf_artillery_smoke_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/wep_bullethit_cardboard_a.cefct_pc/wep_bullethit_cardboard_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/wep_bullethit_sewage_b.cefct_pc/wep_bullethit_sewage_b.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_potted_plant_1_a.cefct_pc/dth_potted_plant_1_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_lightpole_4a_cor_a.cefct_pc/env_lightpole_4a_cor_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_tank_water_a.cefct_pc/dth_tank_water_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_tank_water_a.cefct_pc/metal_wkcyr008_n.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_tank_water_a.cefct_pc/metal_wkcyr009_n.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_tank_water_a.cefct_pc/metal_wkcyr009_s.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_tank_water_a.cefct_pc/metal_wkcyr008_d.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_tank_water_a.cefct_pc/metal_wkcyr008_s.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/dth_tank_water_a.cefct_pc/metal_wkcyr009_d.cvbm_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/wep_bullethit_water_a.cefct_pc/wep_bullethit_water_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_edf_security_a.cefct_pc/env_edf_security_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_wall_electronic_004_a.cefct_pc/env_wall_electronic_004_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/gp_nano_pedestal_dth.cefct_pc/gp_nano_pedestal_dth.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_ext_light_corona_a.cefct_pc/env_ext_light_corona_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_computer_console_a_e.cefct_pc/env_computer_console_a_e.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_tower_comm_short_a.cefct_pc/env_tower_comm_short_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_lightpole_3b_cor_a.cefct_pc/env_lightpole_3b_cor_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_mnt_beacon_lights_a.cefct_pc/env_mnt_beacon_lights_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_light_0105_antenna_a.cefct_pc/env_light_0105_antenna_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/env_wall_electronic_003_a.cefct_pc/env_wall_electronic_003_a.cefct_pc",
        "effects.vpp_pc/vfx_containers.asm_pc/icon_bftp_bomb_a.cefct_pc/icon_bftp_bomb_a.cefct_pc",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
}
