#ifndef SAMPLER_INCLUDED
#define SAMPLER_INCLUDED

#define SAMPLER_QUALITY_LOW 1
#define SAMPLER_QUALITY_MEDIUM 2
#define SAMPLER_QUALITY_HIGH 3

#if defined SSAO_QUALITY_POOR
    #define SSAO_SAMPLER_COUNT 1
    #define SSAO_SAMPLER_COUNT_2 1

#elif defined SSAO_QUALITY_LOW
    #define SSAO_SAMPLER_COUNT 4
    #define SSAO_SAMPLER_COUNT_2 2

    static half3 dirSamplers[4] = {
        half3(0.5, 0, 0.8660254),
        half3(-0.4330127, 0.25, 0.8660254),
        half3(0, -0.5, 0.8660254),
        half3(-0.25, -0.4330127, 0.8660254)
    };
#elif defined SSAO_QUALITY_MEDIUM
    #define SSAO_SAMPLER_COUNT 8
    #define SSAO_SAMPLER_COUNT_2 4

    static half3 dirSamplers[8] = {
        half3(0.3826834, 0, 0.9238795),
        half3(-0.3535534, 0.3535534, 0.8660254),
        half3(0, -0.5, 0.8660254),
        half3(0.5, 0.5, 0.7071068),
        half3(-0.5, 0.5, 0.7071068),
        half3(-0.5, -0.5, 0.7071068),
        half3(0.5, -0.5, 0.7071068),
        half3(0.1913417, -0.1913417, 0.9622502)
    };
#elif defined SSAO_QUALITY_HIGH
    #define SSAO_SAMPLER_COUNT 16
    #define SSAO_SAMPLER_COUNT_2 8

    static half3 dirSamplers[16] = {
        half3(0.1950903, 0, 0.9807853),
        half3(-0.1913417, 0.1913417, 0.9622502),
        half3(0, -0.3826834, 0.9238795),
        half3(0.3826834, 0, 0.9238795),
        half3(0, 0.3826834, 0.9238795),
        half3(-0.3826834, 0, 0.9238795),
        half3(0.1913417, -0.1913417, 0.9622502),
        half3(0.3535534, 0.3535534, 0.8660254),
        half3(-0.3535534, 0.3535534, 0.8660254),
        half3(-0.3535534, -0.3535534, 0.8660254),
        half3(0.3535534, -0.3535534, 0.8660254),
        half3(0.5, 0.5, 0.7071068),
        half3(-0.5, 0.5, 0.7071068),
        half3(-0.5, -0.5, 0.7071068),
        half3(0.5, -0.5, 0.7071068),
        half3(0.25, 0.4330127, 0.8660254)
    };
#else
    #define SSAO_SAMPLER_COUNT 0
#endif







#endif