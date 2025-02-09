# Rfg.Formats
[![NuGet Version](https://img.shields.io/nuget/v/RFGM.Formats)](https://nuget.org/packages/RFGM.Formats)

Shared repo with code for reading and writing file formats used by the Red Faction Guerrilla. The packfile and PEG code is from [SyncFaction](https://github.com/rfg-modding/SyncFaction).


## Formats
The table below lists each format used by RFG and how much this library supports them. Extensions prepended with an asterisk `*` are stored by the game as two files known as the cpu file and gpu file. For example, for static meshes `.csmesh_pc` is the cpu file extension and `.gsmesh_pc` is the gpu file extension. 

✔️= Fully supported.

❔ = Partially supported.

❌ = Not supported.

| Format                     | Extension(s)           | Read  | Write |
|----------------------------|------------------------|-------|-------|
| Packfile                   | .vpp_pc, .str2_pc      | ✔️    | ✔️     |
| Asset assembler            | .asm_pc                | ✔️    | ✔️     |
| Texture                    | *.cpeg_pc, *.cvbm_pc   | ✔️    | ✔️     |
| Static mesh                | *.csmesh_pc            | ❔️    | ❌     |
| Character mesh             | *.ccmesh_pc            | ❔️    | ❌     |
| Map zone                   | .rfgzone_pc, .layer_pc | ✔️    | ❌     |
| Vehicle mesh               | *.ccar_pc              | ❌     | ❌     |
| Animation                  | .anim_pc               | ❔     | ❌     |
| Rig                        | .rig_pc                | ❔     | ❌     |
| Chunk                      | *.cchk_pc              | ❔     | ❌     |
| Visual effect              | *.cefct_pc             | ❔     | ❌     |
| Foliage mesh               | .cfmesh_pc             | ❌     | ❌     |
| Terrain clutter mesh       | *.cstch                | ❔     | ❌     |
| Terrain zone               | *.cterrain_pc          | ❔     | ❌     |
| Terrain subzone            | *.ctmesh_pc            | ❔     | ❌     |
| Fullscreen map data        | .fsmib                 | ❌     | ❌     |
| Shader                     | .fxo_kg                | ❌     | ❌     |
| Steam localization strings | .le_strings            | ❌     | ❌     |
| Localization strings       | .rfglocatext           | ✔️️   | ✔️    |
| Render material            | .mat_pc                | ❔     | ❌     |
| Animation Morph            | .morph_pc              | ❌     | ❌     |
| UI                         | .vint_doc              | ❌     | ❌     |
| Cloth sim                  | .sim_pc                | ❌     | ❌     |
| Sound config               | .xgs_pc                | ❌     | ❌     |
| Soundbank                  | .xsb_pc                | ❌     | ❌     |
| Wavebank                   | .xwb_pc                | ❌     | ❌     |
| Audio categories           | .aud_pc                | ❌     | ❌     |
| Font                       | .vf3_pc                | ❌     | ❌     |
| ?                          | .vfdvp_pc              | ❌     | ❌     |
| ?                          | .rfgvp_pc              | ❌     | ❌     |