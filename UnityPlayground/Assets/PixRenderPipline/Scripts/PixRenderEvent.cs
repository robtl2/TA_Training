using System.Collections.Generic;
using System;

/// <summary>
/// 渲染事件,方便为渲染管线不同阶段扩展流程
/// 相当于极简版的RenderFeature
/// 就是起个问外面的人还要不要加菜的作用
/// </summary>
namespace PixRenderPipline
{

    public enum PixRenderEventName
    {
        BeforeEarlyZ,
        AfterEarlyZ,

        BeforeGBuffer,
        AfterGBuffer,

        BeforeTiled,
        AfterTiled,

        BeforeDeferred,
        AfterDeferred,

        BeforeTransparent,
        AfterTransparent,

        BeforePostProcess,
        AfterPostProcess,

        BeforeFinal,
    }

    public class PixRenderEvent
    {
        static Dictionary<PixRenderEventName, Action<PixRenderer>> events = new();

        public static void AddEvent(PixRenderEventName name, Action<PixRenderer> action)
        {
            if (events.TryGetValue(name, out var existingAction))
                events[name] = existingAction + action;
            else
                events[name] = action;
        }

        public static void RemoveEvent(PixRenderEventName name, Action<PixRenderer> action)
        {
            if (events.TryGetValue(name, out var existingAction))
                events[name] = existingAction - action;
        }

        public static void TriggerEvent(PixRenderEventName name, PixRenderer renderer)
        {
            if (events.TryGetValue(name, out var action))
                action?.Invoke(renderer);
        }

        public static void Dispose()
        {
            events.Clear();
        }
    }

}