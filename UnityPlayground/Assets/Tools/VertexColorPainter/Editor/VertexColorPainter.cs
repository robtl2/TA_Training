using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// 一个绘制Mesh顶点色的工具
/// 可方便的针对指定的颜色通道进行赋值、增加、减少、平滑等操作
/// 可以一次性针对多个Mesh进行绘制操作
/// </summary>
public class VertexColorPainter : EditorWindow
{
	static VertexColorPainter thisWindow;


	[MenuItem("ArtTools/顶点色工具")]
	static void Init()
	{
		thisWindow = GetWindow<VertexColorPainter>();
		thisWindow.titleContent = new GUIContent("顶点色工具");
		thisWindow.Show();
	}

	#region operation
	int operation = 0;
	string[] operationLabels = new string[] {
		"赋值",
		"增加",
		"减少",
		"平滑",
	};

	const int OPERATION_REPLACE = 0;
	const int OPERATION_ADD = 1;
	const int OPERATION_SUBSTRACT = 2;
	const int OPERATION_SMOOTH = 3;
	#endregion

	#region channel
	int channel = 3;
	string[] channelLabels = new string[] {
		"R",
		"G",
		"B",
		"A",
		"RGB",
		"RGBA"
	};

	const int CHANNEL_R = 0;
	const int CHANNEL_G = 1;
	const int CHANNEL_B = 2;
	const int CHANNEL_A = 3;
	const int CHANNEL_RGB = 4;
	const int CHANNEL_RGBA = 5;
	#endregion

	#region states
	bool isStartPainting = false;
	bool isPainting = false;
	bool isAltDown = false;
	bool isChangingBrushRadius = false;
	bool isChangingBrushStrength = false;
	bool showOtherRenderers = true;
	bool useDebugShader = false;
	Vector2 mouseFrom = Vector2.zero;
	Vector2 mouseTo = Vector2.zero;
	#endregion

	#region parameters
	[SerializeField] float radiusMin = .01f;
	[SerializeField] float radiusMax = 1f;
	[SerializeField] float strengthMin = .0001f;
	[SerializeField] float strengthMax = 1.0f;

	[SerializeField] float brushOutterRadius = 0.5f;
	[SerializeField] float brushInternalRadius = 0.2f;
	[SerializeField] float brushStrength = 0.05f;
	[SerializeField] float debugSize = 0.1f;

	bool showDebugParams = false;
	#endregion

	#region locals
	int currentIndex = 0;
	List<GameObject> gameObjects = new List<GameObject>();
	List<Mesh> meshes = new List<Mesh>();
	List<Material> mats = new List<Material>();
	List<Mesh> colliderMeshes = new List<Mesh>();
	Dictionary<GameObject, Collider> colliders = new Dictionary<GameObject, Collider>();
	Dictionary<Collider, bool> colliderEnable = new Dictionary<Collider, bool>();
	List<Collider> tmpColliders;
	Dictionary<Material, Shader> originalShaders = new Dictionary<Material, Shader>();
	Dictionary<Material, int> originalRenderQueue = new Dictionary<Material, int>();
	List<Renderer> otherRenderers = new List<Renderer>();
	
	

	List<List<int>> paintedVertices = new List<List<int>>();
	List<Color[]> lastColors = new List<Color[]>();
	List<Color[]> resetColors = new List<Color[]>();
	List<Color[]> colors = new List<Color[]>();

	Color color = Color.white;
	Color brushColor = Color.yellow;
	float iternalSizeOfOutter = 0.5f;
	#endregion

	void OnGUI()
	{
		#region painting button 
		GUILayout.BeginVertical("box");

		GUI.backgroundColor = isStartPainting ? Color.white : Color.green;
		string btnPaintingText = isStartPainting ? "结束绘制(ESC)" : "开始刷顶点色";

		if (!isStartPainting) {
			GUILayout.Label("请先选中要绘制的一个或多个Mesh物体");
		}

        if (GUILayout.Button(btnPaintingText, GUILayout.Height(25)))
        {
            isStartPainting = !isStartPainting;

            if (isStartPainting)
                isStartPainting = StartPainting();
            else
                StopPainting();
		}

		GUILayout.EndVertical();
		#endregion


		if (!isStartPainting)
		{
			return;
		}

		#region brush
		GUILayout.Space(5);

		GUI.backgroundColor = Color.white;

		GUILayout.Label("笔刷:");
		GUILayout.BeginVertical("box");
		EditorGUI.BeginChangeCheck();
		brushOutterRadius = EditorGUILayout.Slider("笔刷外半径", brushOutterRadius, radiusMin, radiusMax);
		if (EditorGUI.EndChangeCheck())
			brushInternalRadius = Mathf.Max(radiusMin, brushOutterRadius * iternalSizeOfOutter);

		GUILayout.Space(5);
		EditorGUI.BeginChangeCheck();
		brushInternalRadius = EditorGUILayout.Slider("笔刷内半径", brushInternalRadius, radiusMin, brushOutterRadius);
		if (EditorGUI.EndChangeCheck())
			iternalSizeOfOutter = Mathf.Max(0.1f, brushInternalRadius / brushOutterRadius);
		GUILayout.Space(5);
		brushStrength = EditorGUILayout.Slider("笔刷权重", brushStrength, strengthMin, strengthMax);
		GUILayout.EndVertical();
		#endregion


		#region Operation
		GUILayout.Space(5);
		GUILayout.Label("操作:");
		GUILayout.BeginVertical("box");
		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
		for (int i = 0; i < operationLabels.Length; ++i)
		{

			if (i == operation)
				GUI.backgroundColor = Color.gray;
			else
				GUI.backgroundColor = Color.white;

			if (GUILayout.Button(operationLabels[i]))
			{
				operation = i;
			}
		}
		GUILayout.EndHorizontal();

		if (operation == OPERATION_SMOOTH)
		{
			GUILayout.Label("提示:笔刷外半径为平滑时的采样范围");
		}

		GUILayout.Space(5);
		GUILayout.EndVertical();
		GUI.backgroundColor = Color.white;
		#endregion


		#region Color
		GUILayout.Space(5);
		GUILayout.Label("颜色:");
		GUILayout.BeginVertical("box");

		color = EditorGUILayout.ColorField("颜色", color);
		EditorGUI.BeginChangeCheck();
		channel = EditorGUILayout.Popup("Channel", channel, channelLabels);
		if (EditorGUI.EndChangeCheck())
			UpdateChannel();

		GUILayout.EndVertical();
		#endregion


		#region tools
		GUILayout.Space(5);
		GUILayout.Label("工具:");
		GUILayout.BeginVertical("box");

		if (GUILayout.Button("填充全部顶点", GUILayout.Height(25)))
		{
			FillObject();
		}
		GUILayout.Space(5);


		if (GUILayout.Button("重置所有顶点色", GUILayout.Height(25)))
		{
			ResetVertexColor();
		}

		GUILayout.EndVertical();
		#endregion


		#region debug
		GUILayout.Space(5);

		showDebugParams = EditorGUILayout.Foldout(showDebugParams, "Debug:");

		if (showDebugParams)
		{
			GUILayout.BeginVertical("box");

			EditorGUI.BeginChangeCheck();
			showOtherRenderers = EditorGUILayout.Toggle("显示其它的物体", showOtherRenderers);
			if (EditorGUI.EndChangeCheck())
			{
				foreach (var ren in otherRenderers)
					ren.enabled = showOtherRenderers;
			}

			EditorGUI.BeginChangeCheck();
			useDebugShader = EditorGUILayout.Toggle("使用DebugShader", useDebugShader);
			if (EditorGUI.EndChangeCheck())
			{
				foreach (Material mat in mats)
				{
					mat.shader = useDebugShader ? Shader.Find("OSG/Debug/VertexColor") : originalShaders[mat];
					mat.renderQueue = useDebugShader ? 2000 : originalRenderQueue[mat];
				}
			}

			debugSize = EditorGUILayout.Slider("顶点大小", debugSize, 0.1f, 0.5f);

			radiusMin = EditorGUILayout.FloatField("笔刷半径最小值:", radiusMin);
			radiusMax = EditorGUILayout.FloatField("笔刷半径最大值:", radiusMax);

			GUILayout.EndVertical();
		}

		#endregion
	}

	void OnSceneGUI(SceneView sceneView)
	{
		if (!isStartPainting) return;
		//disable object selection when painting
		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

		Event e = Event.current;

		#region dont paint in viewport operating
		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftAlt) isAltDown = true;
		if (e.type == EventType.KeyUp && e.keyCode == KeyCode.LeftAlt) isAltDown = false;
		if (isAltDown) return;
		#endregion


		#region fast change brush radius
		
		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.B)
		{
			isChangingBrushRadius = true;
			mouseFrom = e.mousePosition;
		}
		if (e.type == EventType.KeyUp && e.keyCode == KeyCode.B) isChangingBrushRadius = false;

		if (isChangingBrushRadius)
		{
			mouseTo = e.mousePosition;
			brushOutterRadius += 0.01f * (radiusMax - radiusMin) * (mouseFrom.y - mouseTo.y);
			brushOutterRadius = Mathf.Clamp(brushOutterRadius, radiusMin, radiusMax);
			brushInternalRadius = brushOutterRadius * iternalSizeOfOutter;
			mouseFrom = mouseTo;

			Repaint();
		}
		
		#endregion

		#region  fast change brush strength
		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.S)
		{
			isChangingBrushStrength = true;
			mouseFrom = e.mousePosition;
		}
		if (e.type == EventType.KeyUp && e.keyCode == KeyCode.S) isChangingBrushStrength = false;

		if (isChangingBrushStrength)
		{
			mouseTo = e.mousePosition;
			brushStrength += 0.001f * (mouseFrom.y - mouseTo.y);
			brushStrength = Mathf.Clamp(brushStrength, strengthMin, strengthMax);
			mouseFrom = mouseTo;
			Repaint();
		}
		#endregion

		if (isChangingBrushRadius || isChangingBrushStrength) return;

		#region paintting
		Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
		RaycastHit[] hits = Physics.RaycastAll(ray);

		List<RaycastHit> currentHits = new List<RaycastHit>();
		foreach (var hit in hits){
			if (tmpColliders.Contains(hit.collider)) {
				currentHits.Add(hit);

				currentIndex = tmpColliders.IndexOf(hit.collider);
				paintedVertices[currentIndex] = new List<int>();
				colors[currentIndex] = new Color[] { };
			}
		}


		foreach (var hit in currentHits)
		{
			currentIndex = tmpColliders.IndexOf(hit.collider);

			#region draw brush gizmo
			Handles.color = brushColor;
			Handles.DrawWireDisc(hit.point, hit.normal, brushInternalRadius);
			Handles.color = brushColor * Color.gray;
			Handles.DrawWireDisc(hit.point, hit.normal, brushOutterRadius);
			#endregion

			List<PaintingVertex> vertsInBrush = getVerticesInBrush(hit.collider, hit.point);

			#region draw vertices gizmo
			float vertGizmoSize = debugSize * 0.02f;
			Vector3 sceneCameraPos = GetSceneCameraPos();
			foreach (PaintingVertex vert in vertsInBrush)
			{
				Handles.color = vert.gizmoColor;
				Vector3 viewDir = sceneCameraPos - vert.posWorld;
				float r = Vector3.Magnitude(viewDir) * vertGizmoSize;
				Handles.DrawSolidDisc(vert.posWorld, Vector3.Normalize(viewDir), r);
			}
			#endregion

			#region updae paintting
			if (e.type == EventType.MouseDown && e.button == 0 && e.isMouse)
			{
				isPainting = true;

				paintedVertices[currentIndex].Clear();
				lastColors[currentIndex] = meshes[currentIndex].colors;

				Undo.RegisterCompleteObjectUndo(meshes[currentIndex], "Paint");
				Undo.FlushUndoRecordObjects();
			}

			if (isPainting && e.type == EventType.MouseUp && e.button == 0 && e.isMouse)
			{
				isPainting = false;
				EditorUtility.SetDirty(meshes[currentIndex]);
			}

			if (isPainting)
			{
				colors[currentIndex] = meshes[currentIndex].colors;

				foreach (PaintingVertex vert in vertsInBrush)
				{
					if (!paintedVertices[currentIndex].Contains(vert.index) || operation == OPERATION_SMOOTH)
					{
						Color[] _colors = colors[currentIndex];
						OperateVertexColor(ref _colors, lastColors[currentIndex], vert.meshIndex, vert.index, vert.weight);
						colors[currentIndex] = _colors;
						paintedVertices[currentIndex].Add(vert.index);
					}
				}

				AssignColor(meshes[currentIndex],colors[currentIndex]);
			}

			#endregion
		}

		#endregion
    }

    void OnDisable()
	{
		StopPainting();
	}

	bool StartPainting()
	{
		bool inited = InitObject();

		isPainting = false;

		iternalSizeOfOutter = brushInternalRadius / brushOutterRadius;
		
		resetColors.Clear();
		for (int i = 0; i < meshes.Count; ++i) 
			resetColors.Add(meshes[i].colors);

		if (inited)
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			SceneView.duringSceneGui += OnSceneGUI;

			Undo.undoRedoPerformed -= UnDoCallback;
			Undo.undoRedoPerformed += UnDoCallback;
		}

		Selection.activeGameObject = null;

		return inited;
    }

    void StopPainting()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
		Undo.undoRedoPerformed -= UnDoCallback;

		foreach(var tmpCollider in tmpColliders)
			DestroyImmediate(tmpCollider, false);

		foreach (var colliderMesh in colliderMeshes)
			DestroyImmediate(colliderMesh, false);

		foreach (Material mat in mats)
		{
			if(originalShaders.ContainsKey(mat))
				mat.shader = originalShaders[mat];

			if(originalRenderQueue.ContainsKey(mat))
				mat.renderQueue = originalRenderQueue[mat];
		}


		foreach (var ren in otherRenderers)
			ren.enabled = true;

		foreach (var collider in colliderEnable.Keys) 
			collider.enabled = colliderEnable[collider];
		
			

		isStartPainting = false;
		showOtherRenderers = true;
		useDebugShader = false;

		AssetDatabase.SaveAssets();

		Selection.objects = gameObjects.ToArray();
	}

	bool InitObject()
	{
		#region check mesh
		GameObject[] selectedGameObjects = Selection.gameObjects;
		List<Renderer> renderers = new List<Renderer>();

		colliderMeshes.Clear();
		gameObjects.Clear();
		colliders.Clear();
		meshes.Clear();
		mats.Clear();
		
		if (selectedGameObjects.Length<1) return false;

		foreach (var gameObject in selectedGameObjects) {
			MeshRenderer mrenderer = gameObject.GetComponent<MeshRenderer>();
            SkinnedMeshRenderer skrenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
			if (!mrenderer && !skrenderer) continue;

            Renderer renderer = skrenderer ? skrenderer : mrenderer;
            Mesh mesh;
            if (!skrenderer)
            {
                MeshFilter mf = gameObject.GetComponent<MeshFilter>();
                if (!mf) continue;

                mesh = mf.sharedMesh;
                if (!mesh) continue;
            }
            else
            {
                mesh = skrenderer.sharedMesh;
                if (!mesh) continue;
            }

			gameObjects.Add(gameObject);
			meshes.Add(mesh);
			renderers.Add(renderer);
		}

		if (gameObjects.Count < 1) return false;



		#endregion

		#region init mesh
		tmpColliders = new List<Collider>();

		for (int i = 0; i < gameObjects.Count; ++i) {
            bool isSkinned = renderers[i] is SkinnedMeshRenderer;

            Mesh mesh = meshes[i];
			GameObject gameObject = gameObjects[i];
			Renderer renderer = renderers[i];

			string assetPath = AssetDatabase.GetAssetPath(mesh);
            if (!assetPath.EndsWith(".asset"))
            {
                string newAssetPath = System.IO.Path.GetDirectoryName(assetPath) + "/" + mesh.name + ".asset";

                if (newAssetPath.StartsWith("Library/"))
                    newAssetPath = newAssetPath.Replace("Library/", "Assets/");

                Mesh newMesh = new Mesh();
                newMesh.vertices = mesh.vertices;
                newMesh.triangles = mesh.triangles;
                newMesh.normals = mesh.normals;
                newMesh.tangents = mesh.tangents;
                newMesh.colors = mesh.colors;
                newMesh.uv = mesh.uv;
                newMesh.uv2 = mesh.uv2;
                newMesh.uv3 = mesh.uv3;
                newMesh.uv4 = mesh.uv4;
                newMesh.uv5 = mesh.uv5;
                newMesh.uv6 = mesh.uv6;
                newMesh.uv7 = mesh.uv7;
                newMesh.uv8 = mesh.uv8;
                newMesh.boneWeights = mesh.boneWeights;
                newMesh.bindposes = mesh.bindposes;
                newMesh.subMeshCount = mesh.subMeshCount;
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                    newMesh.SetTriangles(mesh.GetTriangles(subMeshIndex), subMeshIndex);
                newMesh.bounds = mesh.bounds;
                newMesh.UploadMeshData(false);

                AssetDatabase.CreateAsset(newMesh, newAssetPath);
                if (isSkinned)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                    skinnedMeshRenderer.sharedMesh = newMesh;
                }
                else
                {
                    MeshFilter mf = gameObject.GetComponent<MeshFilter>();
                    mf.sharedMesh = newMesh;
                }

                meshes[i] = newMesh;
                mesh = newMesh;
			}

			Mesh colliderMesh = new Mesh();
			colliderMesh.vertices = mesh.vertices;
			colliderMesh.triangles = mesh.triangles;
			colliderMesh.UploadMeshData(false);
			colliderMeshes.Add(colliderMesh);

			mats.AddRange(renderer.sharedMaterials);
			foreach (Material mat in renderer.sharedMaterials)
			{
				originalShaders[mat] = mat.shader;
				originalRenderQueue[mat] = mat.renderQueue;
			}

			Collider collider = gameObject.GetComponent<Collider>();

			if (collider)
			{
				colliderEnable[collider] = collider.enabled;
				collider.enabled = false;
			}

			MeshCollider tmpCollider = gameObject.AddComponent<MeshCollider>();
			tmpColliders.Add(tmpCollider);
			tmpCollider.sharedMesh = colliderMesh;

			if (mesh.colors == null || mesh.colors.Length != mesh.vertexCount)
			{
				Color[] colors = new Color[mesh.vertexCount];
				for (int v = 0; v < mesh.vertexCount; ++v)
					colors[v] = Color.white;

				mesh.colors = colors;
			}
		}
		#endregion

		#region collect otherRenderers
		Scene scene = SceneManager.GetActiveScene();
		GameObject[] roots = scene.GetRootGameObjects();
		otherRenderers.Clear();
		foreach (GameObject root in roots)
		{
			Renderer[] rens = root.GetComponentsInChildren<Renderer>(false);

			foreach (Renderer ren in rens)
			{
				if (!renderers.Contains(ren))
					otherRenderers.Add(ren);
			}
		}
        #endregion

        #region initBuffs
        lastColors.Clear();
		paintedVertices.Clear();
		colors.Clear();
		for (int i = 0; i < gameObjects.Count; ++i)
		{
			lastColors.Add(new Color[] { });
			colors.Add(new Color[] { });
			paintedVertices.Add(new List<int>());
		}
        #endregion

        UpdateChannel();

		return true;
	}

	void UpdateChannel()
	{
		foreach (Material mat in mats)
		{
			if (mat.shader.name == "OSG/Debug/VertexColor")
				mat.SetFloat("_Channel", channel > 4 ? 4 : channel);
		}
	}

	void FillObject()
	{
		for (int i=0;i< meshes.Count;++i) {
			Mesh mesh = meshes[i];
			
			Undo.RegisterCompleteObjectUndo(mesh, "Paint");
			Undo.FlushUndoRecordObjects();

			Color[] colors = mesh.colors;
			int len = colors.Length;

			for (int v = 0; v < len; ++v)
				OperateVertexColor(ref colors, colors, i, v, 1);

			AssignColor(mesh, colors);

			EditorUtility.SetDirty(mesh);
		}
		
	}

	void OperateVertexColor(ref Color[] colors, Color[] lastColors,int meshIndex, int index, float weight)
	{
		Color _color = colors[index];

		switch (operation)
		{
			case OPERATION_ADD:
				_color += color * brushStrength * weight;
				break;
			case OPERATION_SUBSTRACT:
				_color -= color * brushStrength * weight;
				break;
			case OPERATION_SMOOTH:
				SmoothVertexColor(ref _color, lastColors, meshIndex, index, weight);
				break;
			case OPERATION_REPLACE:
				_color = Color.Lerp(_color, color, brushStrength * weight);
				break;
		}

		colors[index] = _color;
	}

	void SmoothVertexColor(ref Color color, Color[] lastColors,int meshIndex, int index, float weight)
	{
		Mesh mesh = meshes[meshIndex];
		Vector3[] vertices = mesh.vertices;
		int len = vertices.Length;
		for (int i = 0; i < len; ++i)
			vertices[i] = gameObjects[meshIndex].transform.TransformPoint(vertices[i]);

		Vector3 curVert = vertices[index];

		Color _color = new Color(0, 0, 0, 0);
		float td = 0;
		float d;
		for (int i = 0; i < len; ++i)
		{
			d = Vector3.Distance(vertices[i], curVert) / brushOutterRadius;
			if (d < 1)
			{
				_color += lastColors[i] * d;
				td += d;
			}
		}

		if (td > 0)
		{
			_color /= td;
			color = Color.Lerp(color, _color, brushStrength * weight);
		}
	}

	//-------------------------TODO 多DepthReferObject参考
	void ResetVertexColor()
	{
		for (int i = 0; i < gameObjects.Count; ++i) {
			Mesh mesh = meshes[i];

			Undo.RegisterCompleteObjectUndo(mesh, "Paint");
			Undo.FlushUndoRecordObjects();

			AssignColor(mesh, resetColors[i]);

			EditorUtility.SetDirty(mesh);
		}
		
	}

	void AssignColor(Mesh mesh, Color[] colors)
	{
		int len = colors.Length;

		Color[] _colors = new Color[len];
		Color color;
		for (int i = 0; i < len; ++i)
		{
			_colors[i] = mesh.colors[i];
			color = colors[i];
			switch (channel)
			{
				case CHANNEL_R:
					_colors[i].r = Mathf.Clamp01(color.r);
					break;
				case CHANNEL_G:
					_colors[i].g = Mathf.Clamp01(color.g);
					break;
				case CHANNEL_B:
					_colors[i].b = Mathf.Clamp01(color.b);
					break;
				case CHANNEL_A:
					_colors[i].a = Mathf.Clamp01(color.a);
					break;
				case CHANNEL_RGB:
					_colors[i].r = Mathf.Clamp01(color.r);
					_colors[i].g = Mathf.Clamp01(color.g);
					_colors[i].b = Mathf.Clamp01(color.b);
					break;
				case CHANNEL_RGBA:
					_colors[i].r = Mathf.Clamp01(color.r);
					_colors[i].g = Mathf.Clamp01(color.g);
					_colors[i].b = Mathf.Clamp01(color.b);
					_colors[i].a = Mathf.Clamp01(color.a);
					break;
			}
		}

		mesh.colors = _colors;
	}

	//undo后得刷新一下才能正常显示
	void UnDoCallback()
	{
		for (int i = 0; i < meshes.Count; ++i)
			meshes[i].colors = meshes[i].colors;
	}

	List<PaintingVertex> getVerticesInBrush(Collider collider, Vector3 brushPos)
	{
		List<PaintingVertex> paintingVerts = new List<PaintingVertex>();
		GameObject gameObject = collider.gameObject;
		Matrix4x4 tm = collider.transform.localToWorldMatrix;
		int meshIndex = gameObjects.IndexOf(gameObject);

		Vector3[] verts = meshes[meshIndex].vertices;

		float normaliInter = brushInternalRadius / brushOutterRadius;

		for (int i = 0; i < verts.Length; ++i)
		{
			Vector3 vert = verts[i];
			Vector3 posWorld = tm.MultiplyPoint(vert);

			float d = Vector3.Distance(posWorld, brushPos);
			if (d < brushOutterRadius)
			{
				d /= brushOutterRadius;
				d = 1 - d;

				float weight = Mathf.SmoothStep(normaliInter, 1, d);

				PaintingVertex paintingVert;
				paintingVert.meshIndex = meshIndex;
				paintingVert.index = i;
				paintingVert.posWorld = posWorld;
				paintingVert.weight = weight;
				paintingVert.gizmoColor = color * weight;

				paintingVerts.Add(paintingVert);
			}
		}

		return paintingVerts;
	}

	Vector3 GetSceneCameraPos()
	{
		Camera sceneCamera = SceneView.lastActiveSceneView.camera;
		Matrix4x4 sceneViewMatrix = sceneCamera.cameraToWorldMatrix;
		return sceneViewMatrix.MultiplyPoint(Vector3.zero);
	}

	struct PaintingVertex
	{
		public int meshIndex;
		public int index;
		public Vector3 posWorld;
		public float weight;
		public Color gizmoColor;
	}
}
