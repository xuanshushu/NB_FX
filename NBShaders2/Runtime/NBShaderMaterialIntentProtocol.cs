namespace NBShader
{
    /// <summary>
    /// Serialized material mode values used by NBShader intent resolution.
    /// These values mirror shader properties and must remain compatible with existing materials.
    /// </summary>
    internal static class NBShaderMaterialIntentProtocol
    {
        public const int MeshSourceParticle = 0;
        public const int MeshSourceUIEffectRawImage = 2;
        public const int MeshSourceUIEffectSprite = 3;
        public const int MeshSourceUIEffectBaseMap = 4;
        public const int MeshSourceUIParticle = 5;

        public const int TransparentOpaque = 0;
        public const int TransparentTransparent = 1;
        public const int TransparentCutOff = 2;

        public const int FxLightUnlit = 0;
        public const int FxLightBlinnPhong = 1;
        public const int FxLightHalfLambert = 2;
        public const int FxLightPbr = 3;
        public const int FxLightSixWay = 4;

        public const int BlendAlpha = 0;
        public const int BlendPremultiply = 1;
        public const int BlendAdditive = 2;
        public const int BlendMultiply = 3;
        public const int BlendOpaque = 4;

        public const int TimeUnscaled = 1;
        public const int TimeScriptable = 2;

        public const int VatTyflow = 1;
    }
}
