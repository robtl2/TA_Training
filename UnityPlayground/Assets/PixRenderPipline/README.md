# PixRenderPipline

象素风格的延迟渲染管线

---

## 管线流程

### EarlyZPass
绘制场景中不透明物体的深度
  
### GBufferPass
将不透明物体Shading所需的参数写入到GBuffer中
- _PixGBuffer_0
    - Color 16bit (HSV_655)
    - Normal 16bit
    
- _PixGBuffer_1
    - ShadingModel 0~15 4bit
    - MaterialParams 28bit
    

### TiledPass
将ShadingModel和光照范围的掩码写入到_PixTiledID贴图中
尝试将没有对应shadingModel掩码的区块通过设置`positionCS=nan`来跳过栅格化
- _PixTiledID
    - LightMask
    - ShadingModelMask

    
### DeferredPass
基于8x8象素Tile的延迟光照
作为RenderTarget的ColorBuff可用于TransparentPassr的读取

- _PixOpaqueTex
    - RGB 颜色
    - A   亮度增益 
  
### SkyPass
天空盒
全屏面片在fragment阶段由相机空间坐标转为世界坐标计算视线方向
- _PixOpaqueTex

### TransparentPass
Forward流程的半透明绘制
因为有GBuffer,所以可以方便的做Decal与面片光源之类的效果
- _PixColorTex

### PostProcessPass
后处理效果
延用DeferredPass中的ColorBuff作为写入目标
- _PixOpaqueTex

### FinalPass
象素化滤镜(CRT风格之类)
象素滤镜的写入目标为BackBuffer，因为低分辨率的RT已经无法制作扫描线这一类的滤镜效果了
ToneMapping也应该在这里做

---




## 额外功能

### Debug
在PixRendererPiplineAsset的inspector面板中可以开启GBuffer的Debug功能
可以查看由GBuffer构建的各种通道信息