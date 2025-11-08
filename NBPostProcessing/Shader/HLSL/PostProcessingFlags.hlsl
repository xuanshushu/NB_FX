#ifndef POST_PROCESSING_FLAGS
#define POST_PROCESSING_FLAGS


#if defined(CUSTOM_POSTPROCESS)
    uint _NBPostProcessFlags;
    #define FLAG_BIT_NB_POSTPROCESS_ON (1 << 0)
    #define FLAG_BIT_DISTORT_SPEED (1 << 1)
    #define FLAG_BIT_OVERLAYTEXTURE (1 << 2)
    #define FLAG_BIT_FLASH (1 << 3)
    #define FLAG_BIT_CHORATICABERRAT (1 << 4)
    #define FLAG_BIT_RADIALBLUR (1 << 5)
    #define FLAG_BIT_VIGNETTE (1 << 6)
    #define FLAG_BIT_OVERLAYTEXTURE_POLLARCOORD (1 << 7)
    #define FLAG_BIT_OVERLAYTEXTURE_MASKMAP (1 << 8)
    #define FLAG_BIT_POST_DISTORT_SCREEN_UV (1 << 9)
    #define FLAG_BIT_RADIALBLUR_BY_DISTORT (1 << 10)
    #define FLAG_BIT_CHORATICABERRAT_BY_DISTORT (1 << 11)
    #define FLAG_BIT_FLASHTEXTURE_POLLARCOORD (1 << 12)



    bool CheckLocalFlags(uint bits)
    {
        return (_NBPostProcessFlags&bits) != 0;
    }
#endif

#endif