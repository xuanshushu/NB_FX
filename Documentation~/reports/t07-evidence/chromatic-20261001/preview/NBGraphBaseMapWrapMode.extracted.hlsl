// The original CameraOpaque pass forces BASEMAP wrap=Clamp even when
// the material's saved wrap says Repeat. This is pass identity, not mode.
uint NBGraphBaseMapWrapMode(uint wrapFlags)
{
#if defined(NB_GRAPH_CAMERA_OPAQUE_PASS)
    return 1u;
#else
    return NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BASEMAP);
#endif
}

