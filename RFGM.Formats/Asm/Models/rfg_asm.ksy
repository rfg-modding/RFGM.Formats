meta:
  id: rfg_asm
  file-extension: asm_pc
  title: Red Faction Guerrilla Asset Assembler file
  endian: le
  bit-endian: le
doc: |
  ASM_PC is a format used by Volition games
doc-ref:
  - TODO
seq:
  - id: header
    type: header_block
  - id: containers
    type: container
    repeat: expr
    repeat-expr: header.num_containers

types:
  header_block:
    seq:
    - id: magic
      contents: [0xed, 0xfe, 0xef, 0xbe] # BEEFFEED in big-endian
    - id: version
      contents: [0x05, 0x00]
    - id: num_containers
      type: u2

  container:
    seq:
      - id: len_name
        type: u2
      - id: name
        type: str
        encoding: ASCII
        size: len_name
      - id: type
        type: u1
        enum: container_type
      - id: flags
        type: u2
        doc: "Kaitai flag-enums are weird, this field is mapped to an enum manually"
      - id: num_primitives
        type: u2
      - id: data_offset
        type: u4
      - id: num_sizes
        type: u4
      - id: len_compressed_data
        type: u4
      - id: sizes
        type: u4
        repeat: expr
        repeat-expr: num_sizes
      - id: primitives
        type: primitive
        repeat: expr
        repeat-expr: num_primitives
    enums:
      container_type:
        0: none
        1: glass
        2: effects_env
        3: effects_preload
        4: effects_dlc
        5: mp_effects
        6: layer_small
        7: layer_large
        8: audio
        9: cloth_sim
        10: decals
        11: decals_preload
        12: fsm
        13: ui
        14: env
        15: chunk
        16: chunk_preload
        17: stitch
        18: world
        19: human_head
        20: human
        21: player
        22: items
        23: items_preload
        24: items_mp_preload
        25: items_dlc
        26: weapon_large
        27: weapon_small
        28: skybox
        29: vehicle
        30: voice_persona
        31: always_loaded_voice_persona
        32: foliage
        33: ui_peg
        34: material_effect
        35: material_preload
        36: shared_backpack
        37: landmark_lod
        38: gps_preload
        39: num_container_types

  primitive:
    seq:
      - id: len_name
        type: u2
      - id: name
        type: str
        encoding: ASCII
        size: len_name
      - id: type
        type: u1
        enum: primitive_type
      - id: allocator
        type: u1
        enum: primitive_allocator
      - id: flags
        type: u1
        doc: "Kaitai flag-enums are weird, this field is mapped to an enum manually"
      - id: split_ext_index
        type: u1
        doc: 0 or 1, correlates to file being a cpu+gpu pair, BUT NOT ALWAYS
      - id: len_cpu
        type: u4
      - id: len_gpu
        type: u4
    enums:
      primitive_type:
        0: none
        1: peg
        2: chunk
        3: zone
        4: terrain
        5: static_mesh
        6: character_mesh
        7: foliage_mesh
        8: material
        9: cloth_sim
        10: vehicle
        11: vehicle_audio
        12: vfx
        13: wavebank
        14: foley_bank
        15: mesh_morph
        16: voice_persona
        17: anim_file
        18: vdoc
        19: lua_script
        20: localization
        21: terrain_high_lod
        22: landmark_lod
        23: num_primitive_types
      primitive_allocator:
        0: none
        1: world
        2: chunk_preload
        3: effect_preload
        4: effect_cutscene
        5: item_preload
        6: decal_preload
        7: cloth_sim_preload
        8: tod
        9: mp_effect_preload
        10: mp_item_preload
        11: player
        12: human
        13: large_weapon
        14: small_weapon
        15: vehicle
        16: large_layer
        17: small_layer
        18: human_voice_persona
        19: always_loaded_human_voice_persona
        20: audio
        21: interface
        22: fsm
        23: interface_stack
        24: interface_slot
        25: interface_mp_preload
        26: interface_mp_slot
        27: material_effect
        28: permanent
        29: dlc_effect_preload
        30: dlc_item_preload
        31: num_allocator_types
