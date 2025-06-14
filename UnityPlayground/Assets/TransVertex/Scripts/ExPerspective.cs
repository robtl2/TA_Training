using UnityEngine;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ExPerspective : MonoBehaviour
{
    public float ExFov = 10;

    [Range(0, 1)]
    public float factor = 1;
    public float offset = 0;
    public float smooth = 0.1f;

    public float range = 100;
    public bool debug = false;

    Camera targetCamera;
    List<Material> mats = new List<Material>();
    void OnEnable()
    {
        Init();
        Refresh();
        #if UNITY_EDITOR
        EditorApplication.update += OnEditorUpdate;
        #endif
    }

    void OnDisable()
    {
        #if UNITY_EDITOR
        EditorApplication.update -= OnEditorUpdate;
        #endif
    }

    void Update()
    {
        Refresh();
    }

    #if UNITY_EDITOR
    private void OnEditorUpdate()
    {
        if (!Application.isPlaying)
            Refresh();
    }
    #endif

    void Init()
    {
        InitMaterials();
        InitCamera();
    }

    void Refresh()
    {
        if (targetCamera == null)
            return;

        var vp = targetCamera.worldToCameraMatrix;
        var rawP = Matrix4x4.Perspective(ExFov, targetCamera.aspect, targetCamera.nearClipPlane, targetCamera.farClipPlane);
        var p = GL.GetGPUProjectionMatrix(rawP, true);
        var ex_vp = p * vp;

        foreach (var mat in mats)
        {
            mat.SetMatrix("_EX_VP", ex_vp);
            mat.SetVector("_EX_PerspectiveProps", new Vector4(factor, offset, smooth, range));

            if (debug)
                mat.EnableKeyword("_DEBUG_ON");
            else
                mat.DisableKeyword("_DEBUG_ON");
        }    
    }

    void InitCamera()
    {
#if UNITY_EDITOR
        bool isInGameView = EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().Name == "GameView";
        
        Camera camObj;
        if (!isInGameView && SceneView.lastActiveSceneView != null)
            camObj = SceneView.lastActiveSceneView.camera;
        else
            camObj = Camera.main;

        if (camObj == null)
            return;

        targetCamera = camObj;
#else
        targetCamera = Camera.main;
#endif
    }

    void InitMaterials()
    { 
        mats = GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials)
            .Concat(GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r => r.sharedMaterials))
            .Where(m => m != null)
            .Distinct().ToList();
    }
    
}
