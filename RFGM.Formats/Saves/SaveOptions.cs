namespace RFGM.Formats.Saves;

public struct SaveOptions
{
    public bool TooltipsEnabled;
    public bool SubtitlesEnabled;
    public bool Vibration;
    public bool CrouchHold;
    public bool AudioEnabled;
    public bool PushToTalkEnabled;
    public bool InvertX;
    public bool InvertY;
    public bool AimAssist;
    public byte VolumeAmbience;
    public byte VolumeMusic;
    public byte VolumeVoiceRadio;
    public byte VolumeSFXInterface;
    public byte VolumeOverall;
    public RLScreenInfo ScreenInfo;
    public RLLevelOfDetail LevelOfDetail;
    public short TextLanguage;
    public short AudioLanguage;
    public float PitchSensitivity;
    public float HeadingSensitivity;
    public float MouseSensitivity;
    public byte OnFootControls;
    public byte VehicleControls;
    public byte StickControls;
    public byte TankStickControls;
    public bool GyroEnabled;
    public float GyroSensitivity;
    public bool DisableMapRotation;
    public byte CameraFOV;
    public bool PerformanceMode;
    public byte VolumeVoiceChat;
    public bool CameraShake;
    public bool GyroEnabledThirdPerson;
    public bool GyroEnabledVehicle;
    public bool GyroEnabledTurret;
    public bool GyroEnabledSatellite;
    public float GyroSensitivityThirdPersonX;
    public float GyroSensitivityThirdPersonY;
    public float GyroSensitivityVehicleX;
    public float GyroSensitivityVehicleY;
    public float GyroSensitivityTurretX;
    public float GyroSensitivityTurretY;
    public float GyroSensitivitySatelliteX;
    public float GyroSensitivitySatelliteY;
    public GyroAxisConfig GyroOrientationConfigX;
    public GyroAxisConfig GyroOrientationConfigY;
    public GyroAxisConfig GyroAngularVelocityConfigX;
    public GyroAxisConfig GyroAngularVelocityConfigY;
    public bool HideXInput;
    public bool HideDInput;
    public string MultiplayerUserName; //0x40
    public bool VehicleMouseCam;
    public bool ZoomHold;
}

public struct RLScreenInfo
{
    public int Width;
    public int Height;
    public int RRNum;
    public int RRDen;
    public float AspectRatio;
    public uint Monitor;
    public WindowMode WindowMode;
    public bool VSyncEnabled;
    public byte NewInfo;
}

public struct RLLevelOfDetail
{
    public ShadowLOD ShadowLOD;
    public AntialiasLOD AntialiasLOD;
    public ParticleLOD ParticleLOD;
    public bool SoftShadows;
    public bool Bloom;
    public bool MotionBlur;
    public bool DepthOfField;
    public bool Distortion;
    public bool SSAO;
    public bool SunShafts;
    public bool TextureReduction;
    public AnisotropicFiltering AnisotropicFiltering;
}

public struct GyroAxisConfig
{
    public SensorID SensorID;
    public float Direction;
}

public enum ShadowLOD : uint
{
    RL_SHADOW_LOD_OFF = 0x0,
    RL_SHADOW_LOD_LOW = 0x1,
    RL_SHADOW_LOD_MED = 0x2,
    RL_SHADOW_LOD_HIGH = 0x3
};

public enum AntialiasLOD : uint
{
    RL_AA_LOD_OFF = 0x0,
    RL_AA_LOD_2X = 0x1,
    RL_AA_LOD_4X = 0x2,
    RL_AA_LOD_8X = 0x3,
    RL_AA_LOD_16X = 0x4,
    RL_AA_LOD_8XQ = 0x5,
    RL_AA_LOD_16XQ = 0x6
};

public enum ParticleLOD : uint
{
    RL_PARTICLE_LOD_LOW = 0x0,
    RL_PARTICLE_LOD_MED = 0x1,
    RL_PARTICLE_LOD_HIGH = 0x2,
    RL_PARTICLE_LOD_VERY_HIGH = 0x3
};

public enum AnisotropicFiltering : uint
{
    RL_AF_LOD_OFF = 0x0,
    RL_AF_LOD_LOW = 0x1,
    RL_AF_LOD_MED = 0x2,
    RL_AF_LOD_HIGH = 0x3
};

public enum SensorID : uint
{
    GYRO_ANGULAR_VELOCITY_X = 0x0,
    GYRO_ANGULAR_VELOCITY_Y = 0x1,
    GYRO_ANGULAR_VELOCITY_Z = 0x2,
    GYRO_ORIENTATION_X = 0x3,
    GYRO_ORIENTATION_Y = 0x4,
    GYRO_ORIENTATION_Z = 0x5,
    GYRO_ORIENTATION_W = 0x6
};

public enum WindowMode : uint
{
    WindowMode_Windowed = 0x0,
    WindowMode_Borderless = 0x1,
    WindowMode_Fullscreen = 0x2
};