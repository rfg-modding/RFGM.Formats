namespace RFGM.Archiver.Models;

public enum FileFormat
{
    /// <summary>
    /// vpp_pc archives
    /// </summary>
    Vpp,

    /// <summary>
    /// str2_pc files (like vpp + asm metadata)
    /// </summary>
    Str2,

    /// <summary>
    /// cpeg_pc+gpeg_pc and cvbm_pc+gvbm_pc
    /// </summary>
    Peg,

    /// <summary>
    /// Any other file that should not be dissected
    /// </summary>
    Unsupported

}
