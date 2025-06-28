using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

public class SShaderGUIUtils
{

    #region blendMode
    enum MFBlendMode
    {
        半透明,
        线性减淡,
        正片叠底,
        不透明,
    }

    static BlendMode[] srcModes = new BlendMode[] {
        BlendMode.SrcAlpha,
        BlendMode.SrcAlpha ,
        BlendMode.DstColor ,
        BlendMode.One,
    };

    static BlendMode[] disModes = new BlendMode[] {
        BlendMode.OneMinusSrcAlpha,
        BlendMode.One,
        BlendMode.Zero,
        BlendMode.Zero,
    };

    public static void BlendModeGUI(MaterialEditor materialEditor, string displayLabel, string curBlendMode, string blendSrc, string blendDist)
    {
        Material mat = materialEditor.target as Material;
        if (!mat.HasProperty(curBlendMode) || !mat.HasProperty(blendSrc) || !mat.HasProperty(blendDist))
            return;

        int last = (int)mat.GetFloat(curBlendMode);
        EditorGUI.indentLevel++;
        int cur = EditorGUILayout.Popup(displayLabel, last, System.Enum.GetNames(typeof(MFBlendMode)));
        EditorGUI.indentLevel--;
        if (cur != last)
        {
            mat.SetFloat(curBlendMode, (float)cur);
            mat.SetInt(blendSrc, (int)srcModes[cur]);
            mat.SetInt(blendDist, (int)disModes[cur]);

            if (cur < 3)
            {
                if (mat.renderQueue < 2501) mat.renderQueue = 3000;
            }
            else if (cur == 3) {
                if (mat.renderQueue > 2450) mat.renderQueue = 2000;
            }


            materialEditor.Repaint();
        }
    }
    #endregion


    public static bool SwitchShaderGUI(Material mat, List<string> displayNames, List<string> shaderNames, int column, string title = "材质切换:")
    {
        Shader lastShader = mat.shader;

        int shaderNum = displayNames.Count;

        int drawedShaderNum = 0;
        EditorGUILayout.LabelField(title);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(16));

        GUIStyle style = new GUIStyle(GUI.skin.button);
        Color defultBackground = GUI.backgroundColor;

        while (drawedShaderNum < shaderNum)
        {
            EditorGUILayout.BeginHorizontal();

            for (int i = drawedShaderNum; i < drawedShaderNum + column; ++i)
            {
                if (i >= shaderNum) break;

                string displayName = displayNames[i];
                string shaderName = shaderNames[i];

                if (shaderName == mat.shader.name)
                {
                    GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                    style.normal.textColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = defultBackground;
                    style.normal.textColor = Color.black;
                }


                if (GUILayout.Button(displayName, style))
                {
                    mat.shader = Shader.Find(shaderName);
                }
            }

            EditorGUILayout.EndHorizontal();

            drawedShaderNum += column;
        }

        GUI.backgroundColor = defultBackground;

        EditorGUILayout.EndHorizontal();


        return lastShader != mat.shader;

    }


    [MenuItem("TATools/MakePackedUberShaders")]
    public static void MakePackedUberShaders()
    {
        MakePackedShaders("SEffect/UnLit Uber Base");
        MakePackedShaders("SEffect/UnLit Uber Advance Base");

        AssetDatabase.Refresh();
    }

    static void MakePackedShaders(string baseShaderName)
    {
        Shader shader = Shader.Find(baseShaderName);
        string baseShaderHeader = string.Format("Shader \"{0}\"", baseShaderName);

        string shaderPath = AssetDatabase.GetAssetPath(shader);
        string shaderFolder = Path.GetDirectoryName(shaderPath) + "/";

        string absShaderPath = Application.dataPath + shaderPath.Substring(6);
        string absShaderFolder = Application.dataPath + shaderFolder.Substring(6);

        if (!File.Exists(absShaderPath)) return;

        #region parse base shaderText
        List<string> paragraphs = new List<string>();                            //划分原Shader为三个不同的段落，方便后面的文本组合
        List<PackedShaderInfo> packedShaderInfos = new List<PackedShaderInfo>(); //关于packedShader的信息
        List<StripPackedRule> stripRules = new List<StripPackedRule>();

        Regex regex;
        Match match;
        string matchValue = "";
        string tmpContent = "";

        using (StreamReader sr = new StreamReader(absShaderPath))
        {
            regex = new Regex("\\\"(.*)\\\"");//匹配引号中的内容，包括引号
            StringBuilder sb = new StringBuilder();
            string line = "";

            while (true)
            {
                line = sr.ReadLine();

                if (line == null) break;

                match = regex.Match(line);
                if (match.Success)
                {
                    matchValue = match.Value;
                    matchValue = matchValue.Substring(1, matchValue.Length - 2);
                    if (line.TrimStart().StartsWith(ShaderPropertie.PACKED_SHADER_NAME))
                    {
                        string[] packedInfos = line.Split('=');
                        string maxFeatureCountStr = packedInfos[1];
                        int maxFeatureCount;
                        int.TryParse(maxFeatureCountStr.Trim(), out maxFeatureCount);

                        packedShaderInfos.Add(new PackedShaderInfo(matchValue, maxFeatureCount));
                    }

                    if (matchValue.TrimStart().StartsWith("S(")) {
                        stripRules.Add(new StripPackedRule(matchValue));
                    }
                }

                if (line != null)
                {
                    sb.AppendLine(line);

                    tmpContent = line.TrimStart();
                    if (tmpContent.StartsWith("//packed properties below") || tmpContent.StartsWith("//packed features below"))
                    {
                        paragraphs.Add(sb.ToString());

                        sb.Remove(0, sb.Length);
                    }
                }
            }

            paragraphs.Add(sb.ToString());
        }



        string paragraph_mid = paragraphs[1];//中间段落包含被注释的各功能模块的材质参数，这里需要进一步处理

        regex = new Regex(@"/\*(?s).*?\*/");                 //找到内容中被注释的参数块
        Regex FieldsRegex = new Regex(@"\[(.*)\]");          //[]内的字符串,包括[]

        MatchCollection matchCollection = regex.Matches(paragraph_mid);

        //收集用于组合packedShader参数的代码行
        List<PropertieContent> propertiesContents = new List<PropertieContent>();
        for (int i = 0; i < matchCollection.Count; ++i)
        {
            string[] lines = matchCollection[i].Value.Split('\n');

            StringBuilder contentSb = new StringBuilder();
            for (int j = 1; j < lines.Length - 2; ++j)
            {
                string line = lines[j].TrimEnd();

                //避免属性开头比如[HDR]之类的tag被误判为Field
                string findFieds = line.TrimStart();
                if (findFieds.StartsWith("["))
                {
                    findFieds = findFieds.Substring(findFieds.IndexOf(']') + 1);
                }

                var fieldsMatch = FieldsRegex.Match(findFieds);

                if (fieldsMatch.Success)
                {
                    string fieldsStr = fieldsMatch.Value.Substring(1, fieldsMatch.Value.Length - 2);

                    string[] fields = fieldsStr.Split(',');

                    List<string> keywords = new List<string>();
                    foreach (var field in fields)
                    {
                        if (field == field.ToUpper() && !field.StartsWith("!"))
                            keywords.Add(field);
                    }

                    PropertieContent content = new PropertieContent(keywords, line);
                    propertiesContents.Add(content);
                }
            }
        }

        //清除注释
        paragraph_mid = regex.Replace(paragraph_mid, "");
        paragraphs[1] = paragraph_mid;
        #endregion

        #region prepair packed shaderNames
        Dictionary<string, Dictionary<string, string>> packedShaderNames = PackedShaderInfo.CollectPackedShaders(packedShaderInfos, stripRules);
        List<string> packedFeatureNames = PackedShaderInfo.PackedFeatureNames;
        #endregion

        foreach (string group in packedShaderNames.Keys)
        {
            foreach (string key in packedShaderNames[group].Keys)
            {
                string shaderName = packedShaderNames[group][key];
                string shaderFileName = shaderName.Replace('/', '_').Replace(' ', '_').Replace("Hidden_", "");
                string shaderFilePath = absShaderFolder + shaderFileName + ".shader";

                StringBuilder shaderContent = new StringBuilder();
                shaderContent.Append(paragraphs[0].Replace(baseShaderHeader, string.Format("Shader \"{0}\"", shaderName)));

                #region get featureNames
                List<string> featureNames = new List<string>();
                if (key.Trim() != string.Empty)
                {
                    string[] ks = key.Split(',');
                    for (int i = 0; i < ks.Length; ++i)
                    {
                        int id = -1;
                        int.TryParse(ks[i], out id);

                        if (id != -1) featureNames.Add(packedFeatureNames[id]);
                    }
                }
                if (group != "") featureNames.Add(group);
                #endregion

                #region combine propertieContent
                StringBuilder propertieContent = new StringBuilder();
                foreach (var line in propertiesContents)
                {
                    bool pass = true;

                    foreach (var k in line.keywords)
                    {
                        if (!featureNames.Contains(k))
                        {
                            pass = false;
                            break;
                        }
                    }

                    if (pass)
                        propertieContent.AppendLine(line.content);
                }

                shaderContent.Append(propertieContent.ToString());
                #endregion

                shaderContent.Append(paragraphs[1]);

                #region comine defined feature keywords
                foreach (string feature in featureNames)
                    shaderContent.AppendLine(string.Format("            #define {0}", feature));
                #endregion

                shaderContent.Append(paragraphs[2]);

                #region saveOut
                if (File.Exists(shaderFilePath)) File.Delete(shaderFilePath);
                StreamWriter sw = new StreamWriter(shaderFilePath, false);
                sw.Write(shaderContent.ToString());
                sw.Close();
                #endregion
            }
        }
    }

    public class PropertieContent {
        public List<string> keywords;
        public string content;

        public PropertieContent(List<string> Keywords, string content) {
            this.content = content;
            this.keywords = Keywords;
        }
    }

    
}