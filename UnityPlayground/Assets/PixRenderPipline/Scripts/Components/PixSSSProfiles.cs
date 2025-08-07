using System.Collections.Generic;
using UnityEngine;

namespace PixRenderPipline
{
    [ExecuteInEditMode]
    public class PixSSSProfiles : MonoBehaviour
    {
        #region static 

        public static string[] profileNames = new string[0];
        
        public const int MAX_PROFILE_COUNT = 32;
        public static List<PixSSSProfiles> profiles = new();

        static PixSSSProfiles profileMan;
        static Matrix4x4[] props = new Matrix4x4[MAX_PROFILE_COUNT];
        static int _SSS_ProfileCount = Shader.PropertyToID("_SSS_ProfileCount");
        static int _SSS_Profiles = Shader.PropertyToID("_SSS_Profiles");
        static bool listIsDirty = true;
        #endregion

        #region properties
        public string profileName = "new profile";

        public Color scatteringColor = Color.red;

        [Range(0,1)]
        public float scatteringRadius = 1.0f;

        [Range(0,1)]
        public float scatteringIntensity = 1.0f;

        public Color transmissionColor = Color.red;
        public float transmissionRadius = 1.0f;

        [Range(0,1)]
        public float transmissionIntensity = 1.0f;
        #endregion

        void OnEnable()
        {
            if (profileMan == null)
                profileMan = this;

            profiles.Add(this);

            listIsDirty = true;
        }

        void OnDisable()
        {
            if (profileMan == this)
                profileMan = null;

            profiles.Remove(this);

            listIsDirty = true;

            if (profiles.Count > 0)
                profileMan = profiles[0];
        }

        string prevName = "";
        void Update()
        {
            if (prevName != profileName)
            {
                prevName = profileName;
                listIsDirty = true;
            }
        }

        void LateUpdate()
        {
            if (profileMan != this)
                return;

            if (profiles.Count < 1) return;

            for (int i = 0; i < profiles.Count; i++)
            {
                if (i == MAX_PROFILE_COUNT) return;

                var profile = profiles[i];

                var scatteringColor = profile.scatteringColor;
                var transmissionColor = profile.transmissionColor;
                float scatteringRadius = profile.scatteringRadius*0.5f;
                float transmissionRadius = profile.transmissionRadius;
                float scatteringIntensity = profile.scatteringIntensity;
                float transmissonIntensity = profile.transmissionIntensity;

                var prop = props[i];
                prop.SetRow(0, new Vector4(scatteringRadius, transmissionRadius, scatteringIntensity, transmissonIntensity));
                prop.SetRow(1, new Vector4(scatteringColor.r, scatteringColor.g, scatteringColor.b, scatteringColor.a));
                prop.SetRow(2, new Vector4(transmissionColor.r, transmissionColor.g, transmissionColor.b, transmissionColor.a));
                props[i] = prop;
            }

            Shader.SetGlobalInt(_SSS_ProfileCount, profiles.Count);
            Shader.SetGlobalMatrixArray(_SSS_Profiles, props);

            if (!listIsDirty) return;
            listIsDirty = false;

            profileNames = new string[profiles.Count+1];
            profileNames[0] = "None";
            for (int i = 0; i < profiles.Count; i++)
                profileNames[i+1] = profiles[i].profileName;

        }
    }
}
