using UnityEngine;

namespace NBShader
{
    public class NBPostProcessFlags : ShaderFlagsBase
    {
        public const string FlagsName = "_NBPostProcessFlags";
        public static int FlagsId = Shader.PropertyToID(FlagsName);

        public override int GetShaderFlagsId(int index = 0)
        {
            return FlagsId;
        }

        protected override string GetShaderFlagsName(int index = 0)
        {
            return FlagsName;
        }

        public NBPostProcessFlags(Material material = null) : base(material)
        {
        }

        public const int FLAG_BIT_NB_POSTPROCESS_ON = 1 << 0;
        public const int FLAG_BIT_DISTORT_SPEED = 1 << 1;
        public const int FLAG_BIT_OVERLAYTEXTURE = 1 << 2;
        public const int FLAG_BIT_FLASH = 1 << 3;
        public const int FLAG_BIT_CHORATICABERRAT = 1 << 4;
        public const int FLAG_BIT_RADIALBLUR = 1 << 5;
        public const int FLAG_BIT_VIGNETTE = 1 << 6;
        public const int FLAG_BIT_OVERLAYTEXTURE_POLLARCOORD = 1 << 7;
        public const int FLAG_BIT_OVERLAYTEXTURE_MASKMAP = 1 << 8;
        public const int FLAG_BIT_POST_DISTORT_SCREEN_UV = 1 << 9; //默认来自于PolarUV
        public const int FLAG_BIT_RADIALBLUR_BY_DISTORT = 1 << 10; //默认来自于PolarUV
        public const int FLAG_BIT_CHORATICABERRAT_BY_DISTORT = 1 << 11; //默认来自于PolarUV
        public const int FLAG_BIT_FLASHTEXTURE_POLLARCOORD = 1 << 12;


    }
}