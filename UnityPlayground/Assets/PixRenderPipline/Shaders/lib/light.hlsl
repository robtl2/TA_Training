#ifndef LIGHT_INCLUDED
#define LIGHT_INCLUDED

half3 _PixMainLightPosition;
half3 _PixMainLightDirection;
half3 _PixMainLightColor;

half3 _PixAmbientLightColor;

struct MainLight
{
    half3 position;
    half3 direction;
    half3 color;
};


MainLight GetMainLight() 
{
    MainLight mainLight;
    mainLight.position = _PixMainLightPosition;
    mainLight.direction = _PixMainLightDirection;
    mainLight.color = _PixMainLightColor;
    return mainLight;
}


#endif