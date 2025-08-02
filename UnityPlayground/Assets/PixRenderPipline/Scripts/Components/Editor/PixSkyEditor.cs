using UnityEngine;
using UnityEditor; 

namespace PixRenderPipline
{
    [CustomEditor(typeof(PixSky))]
    public class PixSkyEditor : Editor
    {
        void bakeSH(PixSky self)
        {
            if (self.texture == null)
            {
                Debug.LogError("No cubemap texture found to bake SH.");
                return;
            }

            self.shData.Bake(self.texture);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PixSky self = (PixSky)target;

            if(self.skyType == PixSky.SkyType.Texture)
            {
                if (GUILayout.Button("Bake SH"))
                {
                    bakeSH(self);
                }
            }
        }
    }
}

