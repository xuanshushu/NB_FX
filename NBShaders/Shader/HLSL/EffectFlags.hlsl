#ifndef EFFECT_FLAGS
#define EFFECT_FLAGS

    #define FLAG_BIT_SATURABILITY_ON (1 << 0)
    #define FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE (1 << 1)
    #define FLAG_BIT_PARTICLE_FRESNEL_FADE_ON (1 << 2)
    #define FLAG_BIT_PARTICLE_FRESNEL_COLOR_ON (1 << 3)
    #define FLAG_BIT_PARTICLE_USETEXCOORD2 (1 << 4)
    #define FLAG_BIT_PARTICLE_DISTANCEFADE_ON (1 << 5)
    #define FLAG_BIT_PARTICLE_CHORATICABERRAT (1 << 6)
    #define FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON (1 << 7)
    #define FLAG_BIT_PARTICLE_POLARCOORDINATES_ON (1 << 8)
    #define FLAG_BIT_PARTICLE_UTWIRL_ON (1 << 9)
    #define FLAG_BIT_PARTICLE_LINEARTOGAMMA_ON (1 << 10)
    #define FLAG_BIT_PARTICLE_FRESNEL_ON (1 << 11)
    #define FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON (1 << 12)
    #define FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA (1 << 13)
    #define FLAG_BIT_PARTICLE_UIEFFECT_ON (1 << 14)
    #define FLAG_BIT_PARTICLE_UNSCALETIME_ON (1 << 15)
    #define FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON (1 << 16)
    #define FLAG_BIT_PARTICLE_CUSTOMDATA1_ON (1 << 17)
    #define FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON (1 << 18)
    #define FLAG_BIT_HUESHIFT_ON (1 << 19)
    #define FLAG_BIT_PARTICLE_CUSTOMDATA2_ON (1 << 20)
    #define FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE (1 << 21)
    #define FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX (1 << 22)
    #define FLAG_BIT_PARTICLE_RAMP_COLOR_MAP_MODE_ON (1 << 23)
    #define FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD (1 << 24)
    #define FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE (1 << 25)
    #define FLAG_BIT_PARTICLE_DISSOLVE_RAMP_MAP (1 << 26)
    #define FLAG_BIT_PARTICLE_DISSOLVE_MASK (1 << 27)
    #define FLAG_BIT_PARTICLE_BACKCOLOR (1 << 28)
    #define FLAG_BIT_PARTICLE_EMISSION_FOLLOW_MAINTEX_UV (1 << 29)
    #define FLAG_BIT_PARTICLE_VERTEX_OFFSET_ON (1 << 30)
    #define FLAG_BIT_PARTICLE_VERTEX_OFFSET_NORMAL_DIR (1 << 31)
    // uint _W9ParticleShaderFlags;

    #define FLAG_BIT_PARTICLE_1_DEPTH_OUTLINE (1 << 0)
    #define FLAG_BIT_PARTICLE_1_PARALLAX_MAPPING (1 << 1)
    #define FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT (1 << 2)
    #define FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT (1 << 3)
    #define FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT (1 << 4)
    #define FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK (1 << 5)
    #define FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY (1 << 6)
    #define FLAG_BIT_PARTICLE_1_MASK_REFINE (1 << 7)
    #define FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE (1 << 8)
    #define FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR (1 << 9)
    #define FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_SIMPLE (1 << 10)
    #define FLAG_BIT_PARTICLE_1_DISSOVLE_USE_RAMP (1 << 11)
    #define FLAG_BIT_PARTICLE_1_MASK_MAP2 (1 << 12)
    #define FLAG_BIT_PARTICLE_1_MASK_MAP3 (1 << 13)
    #define FLAG_BIT_PARTICLE_1_NOISE_MASKMAP (1 << 14)
    #define FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER (1 << 15)
    #define FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_VORONOI (1 << 16)
    #define FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE (1 << 17)
    #define FLAG_BIT_PARTICLE_1_USE_TEXCOORD1 (1 << 18)
    #define FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 (1 << 19)
    #define FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE (1 << 20)
    #define FLAG_BIT_PARTICLE_1_UV_FROM_MESH (1 << 21)
    #define FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE (1 << 22)
    #define FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM (1 << 23)
    #define FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST (1 << 24)
    #define FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO (1 << 25)
    #define FLAG_BIT_PARTICLE_1_VERTEXOFFSET_MASKMAP (1 << 26)
    #define FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE (1 << 27)
    #define FLAG_BIT_PARTICLE_1_BUMP_TEX_UV_FOLLOW_MAINTEX (1 << 28)
    #define FLAG_BIT_PARTICLE_1_SIXWAY_RAMPMAP (1 << 29)
    #define FLAG_BIT_PARTICLE_1_MATCAP_MULTY_MODE (1 << 30)
    
 
    //WrapMode不能够超过16位（因为会占用x和x+16两个bit位）
    #define FLAG_BIT_WRAPMODE_BASEMAP (1 << 0)
    #define FLAG_BIT_WRAPMODE_MASKMAP (1 << 1)
    #define FLAG_BIT_WRAPMODE_MASKMAP2 (1 << 2)
    #define FLAG_BIT_WRAPMODE_NOISEMAP (1 << 3)
    #define FLAG_BIT_WRAPMODE_EMISSIONMAP (1 << 4)
    #define FLAG_BIT_WRAPMODE_DISSOLVE_MAP (1 << 5)
    #define FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP (1 << 6)
    #define FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP (1 << 7)
    #define FLAG_BIT_WRAPMODE_COLORBLENDMAP (1 << 8)
    #define FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP (1 << 9)
    #define FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP (1 << 10)
    #define FLAG_BIT_WRAPMODE_MASKMAP3 (1 << 11)
    #define FLAG_BIT_WRAPMODE_NOISE_MASKMAP (1 << 12)
    #define FLAG_BIT_WRAPMODE_VERTEXOFFSET_MASKMAP (1 << 13)
    #define FLAG_BIT_WRAPMODE_BUMPTEX (1 << 14)
    #define FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP (1 << 15)

    #define FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_X (0*4)
    #define FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_Y (1*4)
    #define FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY (2*4)
    #define FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT (3*4)
    #define FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X (4*4)
    #define FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y (5*4)
    #define FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET (6*4)
    #define FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY (7*4)

    #define FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X (0*4)
    #define FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y (1*4)
    #define FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY (2*4)
    #define FLAGBIT_POS_1_CUSTOMDATA_SATURATE (3*4)
    #define FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_X (4*4)
    #define FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_Y (5*4)
    #define FLAGBIT_POS_1_CUSTOMDATA_VERTEXOFFSET_INTENSITY (6*4)
    #define FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY (7*4)

    #define FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X (0*4)
    #define FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y (1*4)
    #define FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X (2*4)
    #define FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y (3*4)
    #define FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X (4*4)
    #define FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y (5*4)
    #define FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST (6*4)
    //---->这里还有一个坑可以用哦

    #define FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_X (0*4)
    #define FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_Y (1*4)
    #define FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_X (2*4)
    #define FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_Y (3*4)

    #define isCustomDataBit (1 << 3)
    #define Data12Bit (1 << 2)
    #define DataXYorZWBit (1 << 1)
    #define DataXZorYWBit (1 << 0)

    #define FLAG_BIT_UVMODE_POS_0_MAINTEX (0*2)
    #define FLAG_BIT_UVMODE_POS_0_MASKMAP (1*2)
    #define FLAG_BIT_UVMODE_POS_0_MASKMAP_2 (2*2)
    #define FLAG_BIT_UVMODE_POS_0_MASKMAP_3 (3*2)
    #define FLAG_BIT_UVMODE_POS_0_NOISE_MAP (4*2)
    #define FLAG_BIT_UVMODE_POS_0_NOISE_MASK_MAP (5*2)
    #define FLAG_BIT_UVMODE_POS_0_EMISSION_MAP (6*2)
    #define FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP (7*2)
    #define FLAG_BIT_UVMODE_POS_0_DISSOLVE_MASK_MAP (8*2)
    #define FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP (9*2)
    #define FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP (10*2)
    #define FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP (11*2)
    #define FLAG_BIT_UVMODE_POS_0_BUMPTEX (12*2)
    #define FLAG_BIT_UVMODE_POS_0_RAMP_COLOR_MAP (13*2)
    #define FLAG_BIT_UVMODE_POS_0_PROGRAM_NOISE (14*2)
    #define FLAG_BIT_UVMODE_POS_0_SHAREDUV (15*2)

    #define FLAG_BIT_COLOR_CHANNEL_POS_0_MAINTEX_ALPHA (0*2)
    #define FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1 (1*2)
    #define FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2 (2*2)
    #define FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3 (3*2)
    #define FLAG_BIT_COLOR_CHANNEL_POS_0_NOISE_MASK (4*2)
    #define FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MAP (5*2)
    #define FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MASK_MAP (6*2)
    #define FLAG_BIT_COLOR_CHANNEL_POS_0_RAMP_COLOR_MAP (7*2)

    float GetCustomData(uint flagProperty,int flagPos,float orignValue,half4 cutstomData1,half4 customData2)
    {
        uint bit =  flagProperty >> flagPos;
    
        // bit &= 15;// binary 1111 这一步可能是没必要的。
        UNITY_BRANCH
        if((bit & isCustomDataBit) == 0)
        {
            return orignValue;
        }
        else
        {
            half4 customData = 0;
            if((bit & Data12Bit))
            {
                customData = cutstomData1;
               
            }
            else
            {
                customData = customData2;
            }

            if(bit & DataXYorZWBit)
            {
                if(bit & DataXZorYWBit)
                {
                    return customData.x;
                }
                else
                {
                    return customData.y;
                }
            }
            else
            {
                if(bit & DataXZorYWBit)
                {
                    return customData.z;
                }
                else
                {
                    return customData.w;
                }
            }
        }
        return 999;//提示错误
    }


    struct BaseUVs
    {
        float2 defaultUVChannel;
        float2 specialUVChannel;
        float2 uvAfterTwirlPolar;
        float2 cylinderUV;
        float2 mainTexUV;
        float2 screenUV;
        float2 worldPosUV;
        float2 objectPosUV;
        float2 sharedUV;
    };

    float2 GetUVByUVMode(uint flagProperty,uint flagTypeProperty,int flagPos,BaseUVs baseUVs)
    {
        flagProperty = flagProperty >> flagPos;
        flagProperty &= 3;
        flagTypeProperty = flagTypeProperty >> flagPos;
        flagTypeProperty &= 3;
        switch (flagTypeProperty)
        {
            case 0:
                switch(flagProperty)
                {
                    case 0:
                    return baseUVs.defaultUVChannel;
                    case 1:
                    return baseUVs.specialUVChannel;
                    case 2:
                    return baseUVs.uvAfterTwirlPolar;
                    case 3:
                    return baseUVs.cylinderUV;
                    default:
                    return baseUVs.defaultUVChannel;
                }
            case 1:
                switch(flagProperty)
                {
                    case 0:
                    return baseUVs.mainTexUV;
                    case 1:
                    return baseUVs.screenUV;
                    case 2:
                    return baseUVs.worldPosUV;
                    case 3:
                    return baseUVs.objectPosUV;
                    default:
                    return baseUVs.defaultUVChannel;
                }
            case 2:
                switch(flagProperty)
                {
                    case 0:
                    return baseUVs.sharedUV;
                    default:
                    return baseUVs.defaultUVChannel;
                }
            default:
            return baseUVs.defaultUVChannel;
        }
    }

    half Blend_HardLight(half Base, half Blend)
    {
        half result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
        half result2 = 2.0 * Base * Blend;
        half zeroOrOne = step(Blend, 0.5);
        half Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
        return Out;
    }

    #define FLAG_BIT_PNOISE_BLEND_POS_0_BASE_BLEND (0*3)
    #define FLAG_BIT_PNOISE_BLEND_POS_0_MASK (1*3)
    #define FLAG_BIT_PNOISE_BLEND_POS_0_DISSOLVE (2*3)
    #define FLAG_BIT_PNOISE_BLEND_POS_0_DISTORT (3*3)
    #define FLAG_BIT_PNOISE_BLEND_POS_0_MAINTEX (4*3)

    half BlendPNoise(uint flagProperty,int flagPos,half source,half pNoise,half opacity)
    {
        flagProperty = flagProperty >> flagPos;
        flagProperty &= 7;
        switch(flagProperty)
        {
            case 0:
                return source;
            case 1:
                return lerp(source,pNoise*source,opacity);
            case 2:
                return lerp(source,min(pNoise,source),opacity);
            case 3:
                return lerp(source,Blend_HardLight(source,pNoise),opacity);
            default:
                return source;
        }
    }

    //ForDistort
    half2 BlendPNoise(uint flagProperty,int flagPos,half2 source,half2 pNoise,half opacity)
    {
        flagProperty = flagProperty >> flagPos;
        flagProperty &= 7;
        switch(flagProperty)
        {
        case 0:
            return source;
        case 1:
            return lerp(source,pNoise*source,opacity);
        case 2:
            return lerp(source,min(pNoise,source),opacity);
        case 3:
            return lerp(source,Blend_HardLight(source,pNoise),opacity);
        default:
            return source;
        }
    }


    
#endif