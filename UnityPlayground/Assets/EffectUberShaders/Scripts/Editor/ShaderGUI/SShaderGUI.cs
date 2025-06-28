using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Linq;


/// <summary>
/// 自定义Shader界面
/// 用于为多分支类型的Shader提供简洁方便的界面显示效果
/// 
/// 
/// 介绍：
///     Field:  显示域。可以为属性分配一个或者多个显示域，当Field激活条件满足时，这条属性才会在面板中显示。Field通常是KEYWORD,也有一些特例，后面会有说明
///             定义方式：在displayName的结尾用 [FIELD] [FIELD_A,FIELD_B] [FIELD_A|FIELD_B]  [FIELD_A,FIELD_B|FIELD_C]等方式来定义一个或多个显示域
///             ","号用来表示左右两边都激活才通过   "|"号表示左右两边有一个激活则通过   ","优先级高于"|"
///             Field前面加!可用于取非
/// 
/// 
///     PType:  OSGShaderGUI提供了一些GUI显示类型，一般是使用DisplayName开头的特殊写法来定义
///     
///             类型                    定义方式                                                                用途
///             Group                   属性名 Group#FIELD (唯一使用属性名，而不是显示名定义的类型)             将包含该Group所属Field的属性成组显示，#后面的FIELD可以是一个关联KEYWORD，也可以是任意名字的一个显示域
///             FeatureGroup			"G(KEYWORD_A,KEYWORD_B)/displayLabel"									以下拉列表方式切换一组KEYWORDS就否在拆分后的shader中被define(为uber拆分为多Shader提供支持,一个材质目前只能有一个FeatureGroup)
///             FeatureChecker          "C(featureIndex)/displayLabel"                                          以开关方式显示float,依据多个打开的FeatureChecker中的featureIndex组合成的信息来切换shader(为uber拆分为多Shader提供支持)
///             FeatureStrip            "S(KEYWORD)/KEYWORD_A" 或 "S(KEYWORD)/KEYWORD_A,KEYWORD_B"				PackedShader组合规则：当括号中的KEYWORD处于开启时， / 后面的KEYWORD肯定不会开启(为uber拆分为多Shader提供支持)
///             ToggleWithKeyword       "T(KEYWORD)/displayLabel"                                               以开关方式显示float，并且同时可以开关KEYWORD。作用与[Toggle(KEYWORD)]相同
///             TogglePass              "P(PassLightMode)/displayLabel"                                         开关材质的特定LightMode的Pass
///             Toggle                  "T/displayLabel"                                                        以开关方式显示float，属性名可以作为显示域使用作用与[Toggle]相同
///             EnumWithKeywords        "E(KEYWORD_A,KEYWORD_B)/displayLabel:labelA,labelB;valueA,valueB"       以下拉列表方式切换一组KEYWORDS,并且可以自定义对应枚举的值
///                                     "E(KEYWORD_A,KEYWORD_B)/displayLabel:labelA,labelB"                     以下拉列表方式切换一组KEYWORDS，以默认序号作为作为对应枚举的值
///             Enum                    "E/displayLabel:labelA,labelB;valueA,valueB"                            以下拉列表方式切换一组float值,并且可以自定义对应枚举的值      "属性名_枚举值" 可以作为显示域使用
///                                     "E/displayLabel:labelA,labelB"                                          以下拉列表方式切换一组float值，以默认序号作为作为对应枚举的值 "属性名_枚举值" 可以作为显示域使用
///                                     

/// 
///             Vector                  "V/displayLabel:label_x(from,to),label_y(from,to),label_z(from,to),label_w(from,to)"        将一个Vector分成4个(也可以少于4个)Slider显示
///                                     "V/displayLabel:label_x,label_y,label_z,label_w"                                            将一个Vector分成4个(也可以少于4个)FloatField显示
///                                     "V/displayLabel:label_x(from,to),label_y(from,to),label_z,label_w"                          Vector中的分量可以自由决定是Slider还是FloatField
///                                     
///             VectorPlus              "V(xyz,w)/displayLabel:label_xyz,label_w(from,to)"                                          可拆分Vector中的分量以VectorField或者Slider或FloatField显示
///                                     "V(xy,zw)/displayLabel:label_xy(from,to),label_zw"                                          括号内的拆分写法只有以下固定三种(不想搞什么打乱顺序也行了):(xy,zw) (xyz,w) (xy,z,w)
///                                                                                                                                     
///             
///             Label                   "L/line_1\nline_2"                                                      单纯的提示文字，使用 \n 分行，这些提示文字也可以作为其它参数切换显示名时使用（见下方 其它）
///             Space                   " /"                                                                    提行用，属性的值用于定义提多少行
///             
///             BlendMode               "B/displayLabel:SrcBlendPropName,DistBlendPropName"                     用于切换BlendMode
///             
/// 
///             KeywordLink             "K&(KEYWORD)/KEYWORD_A" 或 "K&(KEYWORD)/KEYWORD_A,KEYWORD_B"            Keyword组合规则：当括号中的KEYWORD处于开启时， / 后面的KEYWORD肯定同时也会开启
///             KeywordStrip            "K!(KEYWORD)/KEYWORD_A" 或 "K!(KEYWORD)/KEYWORD_A,KEYWORD_B"            Keyword组合规则：当括号中的KEYWORD处于开启时， / 后面的KEYWORD肯定不会开启
///             KeywordWithAnyEnumActive"K|(KEYWORD)/_EnumPropName1,_EnumPropName2,_EnumPropName3"              Keyword规则: / 后面是Enum类型的多个属性名，当这些属性中有任何一个的值是这个KeywordAnyEnumActive对应参数值时，括号中的Keyword会打开，否则关闭
///                                                                                                             (感觉我没说清楚，可以看下OSG/E/UnLit Uber Advance，里面有这一条的应用。用来当任何一个特性的贴图坐标系打开了屏幕坐标时，Enable OSG_E_SCREENSPACE;如果没有一个特性使用了屏幕坐标的话，则关掉这个Keyword)    
/// 
///     GUITextureImporterChecker: 贴图的importer设置也可以通过GUI检查，检查的内容写在displayName的 '{}'括号内，多则检查用 ',' 号分割
///             sRGB                    sRGB:on/off                                                             如果有sRGB的检查标签，则贴图类型为Default，sRGB开关强制与约定值一致
///             Size                    Size:512                                                                约定贴图的最大尺寸
///             type                    type:texTypeName                                                        约定贴图类型
/// 其它:
///     如果displayName以"__"开头的话，则该属性不会显示在面板，作用与[HideInInspector]相同
///     Label类型中的labels会缓存在Editor中，材质属性里如果显示名这样写的话  _LabelPropName@_SelectorPorpName 则可以用SelectorPropertive当前的值来切换属性的显示名
///
///                                -------------@wilsonluo
/// </summary>

public class PackedShaderInfo
{
	public static string currentGroupName = "";

	public static List<string> PackedFeatureNames = new List<string>();
	public static List<string> PackedFeatureGroups = new List<string>();

	public static Dictionary<string, Dictionary<string, Shader>> PackedShaders = new Dictionary<string, Dictionary<string, Shader>>();

	public string baseShader;
	public string shaderHeader;
	public string keywordGroup;
	public string[] keywords;
	public int maxFeatureCount;

	public PackedShaderInfo(string info, int maxFeatureCount)
	{
		this.maxFeatureCount = maxFeatureCount;

		string[] infos = info.Split('|');

		baseShader = infos[0];
		shaderHeader = infos[1];
		keywordGroup = infos[2];
		keywords = infos[3].Split(',');
	}



	public static Dictionary<string, Dictionary<string, string>> CollectPackedShaders(List<PackedShaderInfo> packedShaderInfos, List<StripPackedRule> stripPackedRules)
	{
		Dictionary<string, Dictionary<string, string>> packedShaderNames = new Dictionary<string, Dictionary<string, string>>();

		if (packedShaderInfos.Count == 0) return packedShaderNames;

		PackedFeatureNames.Clear();

		foreach (var packedShaderInfo in packedShaderInfos)
		{
			string PackedShaderHeader = packedShaderInfo.shaderHeader;

			foreach (var name in packedShaderInfo.keywords)
				if (!PackedFeatureNames.Contains(name))
					PackedFeatureNames.Add(name);
		}

		foreach (var packedShaderInfo in packedShaderInfos)
		{
			List<int> ids = new List<int>();
			foreach (var name in packedShaderInfo.keywords) {
				int id = PackedFeatureNames.IndexOf(name);
				ids.Add(id);
			}

			string groupName = packedShaderInfo.keywordGroup;
			if (groupName == "__") groupName = "";

			if(!PackedShaders.ContainsKey(groupName))
				PackedShaders[groupName] = new Dictionary<string, Shader>();

			PackedShaders[groupName][""] = Shader.Find(packedShaderInfo.baseShader);

			packedShaderNames[groupName] = new Dictionary<string, string>();
			if (groupName != "") {
				packedShaderNames[groupName][""] = packedShaderInfo.baseShader;
			}

			PackedFeatureGroups.Add(packedShaderInfo.keywordGroup);

			List<List<int>> featureIDs = new List<List<int>>();
			CollectPackedFeatureIDs(packedShaderInfo.maxFeatureCount, new List<int>(), 0, ids, featureIDs);

			foreach (var fIDs in featureIDs)
			{
				bool pass = true;
				foreach (var rule in stripPackedRules) {
					int A = PackedFeatureNames.IndexOf(rule.keyword);

					if (fIDs.Contains(A)) {
						foreach (var k in rule.keywords) {
							int B = PackedFeatureNames.IndexOf(k);
							if (fIDs.Contains(B)) {
								pass = false;
								break;
							}
						}
					}

					if (!pass) break;
				}

				if (!pass) continue;

				string key = "";
				string shaderName = packedShaderInfo.shaderHeader;
				for (int i = 0; i < fIDs.Count; ++i)
				{
					key += fIDs[i].ToString();
					shaderName += PackedFeatureNames[fIDs[i]];
					if (i < fIDs.Count - 1)
					{
						key += ",";
						shaderName += "_";
					}
				}

				PackedShaders[groupName][key] = Shader.Find(shaderName);
				packedShaderNames[groupName][key] = shaderName;
			}
		}

		return packedShaderNames;
	}

	static void CollectPackedFeatureIDs(int maxFeatureCount, List<int> headIDs, int pid, List<int> cids, List<List<int>> featureIds)
	{
		List<int> curHeadIDs = new List<int>();
		foreach (int i in headIDs) curHeadIDs.Add(i);

		foreach (int i in cids)
		{
			List<int> curIDs = new List<int>();
			foreach (int id in curHeadIDs) curIDs.Add(id);
			curIDs.Add(i);
			featureIds.Add(curIDs);

			List<int> _cids = new List<int>();
			foreach (int id in cids)
			{
				if (id > i) _cids.Add(id);
			}

			if (curIDs.Count < maxFeatureCount)
				CollectPackedFeatureIDs(maxFeatureCount, curIDs, i, _cids, featureIds);
		}
	}
}

public class StripPackedRule
{
	public string keyword;
	public List<string> keywords;

	public StripPackedRule(string ruleInfo)
	{
		keyword = "";
		keywords = new List<string>();

		Regex regex = new Regex(@"\s*\([^\)]*\)\s*");//所有()内的内容
		Match match = regex.Match(ruleInfo);

		if (match.Success)
		{
			keyword = match.Value.Substring(1, match.Value.Length - 2);
		}

		regex = new Regex(@"/.*");         //  右斜杠右边的字符串
		match = regex.Match(ruleInfo);

		if (match.Success)
		{
			string keywordStr = match.Value.Substring(1);
			var keys = keywordStr.Split(',');
			foreach (var k in keys) keywords.Add(k);
		}
	}
}

public class ShaderPropertie
{
	#region type
	public enum PType
	{
		Group,
		ToggleWithKeyword,
		Toggle,
		TogglePass,
		EnumWithKeywords,
		Enum,
		Label,
		Vector,
		VectorPlus,
		Space,
		KeywordLink,
		KeywordStrip,
		KeywordWithAnyEnumActive,
		BlendMode,

		FeatureChecker,
		FeatureGroup,
		
		None,
	}
	#endregion

	public static string PackedFeaturesKey = "";


	#region parse helper
	public const string PACKED_SHADER_NAME = "PACKED_SHADER";	//用于定义packedShader信息的属性
	const string GROUP_NAME = "Group#";     //特殊情况:Group使用材质属性的name，而不是displayName来判断
	const string IGNORE_DISPLAY = "__";     //需要先排除用于跳过显示的标识，然后再用PTypeDic中的Key作类型判断

	static Regex BracketsRegex = new Regex(@"\s*\([^\)]*\)\s*");//所有()内的内容,包括()
	static Regex FieldsRegex = new Regex(@"\[(.*)\]");          //[]内的字符串,包括[]
	static Regex ImporterRuleRegex = new Regex(@"\{(.*)\}");    //{}内的字符串,包括{}
	static Regex RightOfSharpRegex = new Regex(@"#.*");         //"#"右边的字符串,包括"#"
	static Regex RightOfColonRegex = new Regex(@":.*");         //":"右边的字符串,包括":"
	static Regex LeftOfColonRegex = new Regex(@"[^:]*");        //":"左边的字符串
	static Regex LeftOfSemicolonRegex = new Regex(@"[^;]*");    //";"左边的字符串
	static Regex RightOfSemicolonRegex = new Regex(@";.*");     //";"右边的字符串


	//通过displayName字符串的前两个字符判断类型
	static readonly Dictionary<string, PType> PTypeDic = new Dictionary<string, PType> {
		{ "T(", PType.ToggleWithKeyword },
		{ "T/", PType.Toggle },
		{ "P(", PType.TogglePass },
		{ "E(", PType.EnumWithKeywords },
		{ "E/", PType.Enum},
		{ "L/", PType.Label},
		{ "V/", PType.Vector},
		{ "V(", PType.VectorPlus},
		{ " /", PType.Space },
		{ "K&", PType.KeywordLink},
		{ "K!", PType.KeywordStrip},
		{ "K|", PType.KeywordWithAnyEnumActive},
		{ "B/", PType.BlendMode},

		{ "C(", PType.FeatureChecker},//跟据不同FeatureChecker的开关组合来切换不同的Shader(uber类Shader的editor替换方案)
		{ "G(", PType.FeatureGroup},//跟据FeatureGroup的选项组合来切换不同的Shader(uber类Shader的editor替换方案)
	};
	#endregion


	#region infos 提取各个信息字符段
	public string fieldsInfo = "";              //[]内的fields信息段
	public string keywordsInfo = "";            //()内的keywords信息段
	public string importerRulesInfo = "";       //{}内的improterRules信息段
	public string commonInfo = "";              //排除掉所有特殊字段，留下的通用信息段
	#endregion


	#region propertives 从上面的infos中根据不同type最终解析出的结果
	public static Dictionary<string, string[]> LabelCaches = new Dictionary<string, string[]>();
	

	public bool ignoreDisplay = false;
	public PType type = PType.None;
	public List<GUITextureImporterChecker> textureImporterCheckers = new List<GUITextureImporterChecker>();
	public string[] displayFields = new string[0];
	public string[] keywords = new string[0];
	public string[] labels = new string[0];
	public List<int> enumVals = new List<int>();
	public List<Vector2> ranges = new List<Vector2>();
	public string keyword = "";
	public string displayLabel = "";
	public int lines = 0;
	#endregion


	public MaterialProperty materialProperty;

	public ShaderPropertie(MaterialProperty mp, Material mat)
	{
		materialProperty = mp;
		Parse(mp, mat);
	}
	

	#region Parse
	void Parse(MaterialProperty mp, Material mat)
	{
		ParseType(mp);
		ParseInfos(mp);
		ParseProperties(mp, mat);
		ParseCachedLabels(mat);
	}

	/// <summary>
	/// 解析属性类型
	/// </summary>
	void ParseType(MaterialProperty mp)
	{
		ignoreDisplay = false;

		if (mp.name.StartsWith(PACKED_SHADER_NAME)) {
			ignoreDisplay = true;
			type = PType.None;
			return;
		}

		if (mp.displayName.StartsWith("S(")) {
			ignoreDisplay = true;
			type = PType.None;
			return;
		}

		if (mp.name.StartsWith(GROUP_NAME))
		{
			type = PType.Group;
			return;
		}
		else
		{
			string header = mp.displayName.Substring(0, 2);
			if (header == IGNORE_DISPLAY)
			{
				ignoreDisplay = true;
				header = mp.displayName.Substring(2, 2);
			}

			if (PTypeDic.ContainsKey(header))
			{
				type = PTypeDic[header];

				if (type == PType.KeywordLink || type == PType.KeywordStrip || type == PType.KeywordWithAnyEnumActive)
					ignoreDisplay = true;

				return;
			}
		}

		type = PType.None;
	}

	/// <summary>
	/// 预解析出不同功能的字段
	/// </summary>
	void ParseInfos(MaterialProperty mp)
	{
		commonInfo = mp.displayName;
		if (commonInfo.StartsWith(IGNORE_DISPLAY))
			commonInfo = commonInfo.Substring(2);

		Match rexMatch;
		string matchValue;
		if (type != PType.None)
		{
			if (type == PType.Group)
			{
				matchValue = RightOfSharpRegex.Match(mp.name).Value;
				keywordsInfo = matchValue.Substring(1);
			}
			else
			{
				rexMatch = BracketsRegex.Match(mp.displayName);

				if (rexMatch.Success)
				{
					matchValue = rexMatch.Value;
					commonInfo = commonInfo.Replace(matchValue, "");
					keywordsInfo = matchValue.Substring(1, matchValue.Length - 2);
				}

				commonInfo = commonInfo.Substring(2);
			}
		}

		rexMatch = FieldsRegex.Match(mp.displayName);
		if (rexMatch.Success)
		{
			matchValue = rexMatch.Value;
			commonInfo = commonInfo.Replace(matchValue, "");
			fieldsInfo = matchValue.Substring(1, matchValue.Length - 2);
		}

		rexMatch = ImporterRuleRegex.Match(mp.displayName);
		if (rexMatch.Success)
		{
			matchValue = rexMatch.Value;
			commonInfo = commonInfo.Replace(matchValue, "");
			importerRulesInfo = matchValue.Substring(1, matchValue.Length - 2);
		}

		if (commonInfo.StartsWith("/")) commonInfo = commonInfo.Substring(1);
	}

	void ParseProperties(MaterialProperty mp, Material mat)
	{
		#region prepair
		if (!string.IsNullOrEmpty(fieldsInfo))
			displayFields = fieldsInfo.Split(',');

		keyword = keywordsInfo;
		if (!string.IsNullOrEmpty(keywordsInfo))
			keywords = keywordsInfo.Split(',');

		if (type == PType.KeywordLink || type == PType.KeywordStrip)
			keywords = commonInfo.Split(',');

		for (int i = 0; i < keywords.Length; ++i)
			if (keywords[i] == "_DUMMY" || keywords[i] == "__") keywords[i] = "";
		#endregion


		#region parse displayLabel
		displayLabel = commonInfo;
		if (type == PType.FeatureGroup || type == PType.Enum || type == PType.EnumWithKeywords || type == PType.Vector || type == PType.VectorPlus || type == PType.BlendMode)
		{
			displayLabel = LeftOfColonRegex.Match(commonInfo).Value;
		}
		#endregion


		#region parse labels
		labels = new string[0];
		switch (type)
		{
			case PType.KeywordWithAnyEnumActive:
				labels = commonInfo.Split(',');
				break;
			case PType.Label:
				labels = commonInfo.Split('\n');
				LabelCaches[mp.name] = labels;
				break;
			case PType.BlendMode:
				labels = RightOfColonRegex.Match(commonInfo).Value.Substring(1).Split(',');
				break;
			default:
				break;
		}

		commonInfo = commonInfo.Replace(displayLabel + ":", "");//后面的解析commonInfo不需要displayLabel部分了

		string labelInfo = commonInfo;
		Match rexMatch;
		if (type == PType.Enum || type == PType.EnumWithKeywords || type == PType.FeatureGroup)
		{
			rexMatch = LeftOfSemicolonRegex.Match(labelInfo);
			if (rexMatch.Success)
				labelInfo = rexMatch.Value;

			labels = labelInfo.Split(',');
		}

		if (type == PType.Vector || type == PType.VectorPlus)
		{
			labelInfo = BracketsRegex.Replace(labelInfo, "");

			labels = labelInfo.Split(',');
		}
		#endregion


		#region parse enumVals
		if (type == PType.Enum || type == PType.EnumWithKeywords || type == PType.FeatureGroup)
		{
			enumVals.Clear();
			for (int i = 0; i < labels.Length; ++i) enumVals.Add(i);

			rexMatch = RightOfSemicolonRegex.Match(commonInfo);
			if (rexMatch.Success)
			{
				string enumValsInfo = rexMatch.Value.Substring(1);
				string[] vals = enumValsInfo.Split(',');

				for (int i = 0; i < vals.Length; ++i)
				{
					if (i < enumVals.Count)
					{
						int v;
						int.TryParse(vals[i], out v);
						enumVals[i] = v;
					}
				}
			}
		}
		#endregion


		#region parse ranges
		if (type == PType.Vector || type == PType.VectorPlus)
		{
			ranges.Clear();

			string[] rangesInfo = commonInfo.Split(',');

			foreach (string range in rangesInfo)
			{
				rexMatch = BracketsRegex.Match(range);
				if (rexMatch.Success)
				{
					string rangeStr = range.Substring(1, rexMatch.Value.Length - 2);
					string[] vs = rangeStr.Split(',');
					float x = 0;
					float y = 1;
					float.TryParse(vs[0], out x);
					float.TryParse(vs[1], out y);
					ranges.Add(new Vector2(x, y));
				}
				else
				{
					ranges.Add(Vector2.zero);
				}
			}
		}
		#endregion


		#region parse lines
		lines = labels.Length;
		switch (type)
		{
			case PType.Space:
				lines = (int)mp.floatValue;
				break;
			case PType.EnumWithKeywords:
				lines = enumVals.Count;
				break;
			case PType.Enum:
				lines = enumVals.Count;
				break;
			case PType.FeatureGroup:
				lines = enumVals.Count;
				break;
			case PType.VectorPlus:
				lines = 1;
				if (keyword == "xyz,w" || keyword == "xy,zw")
					lines = 2;
				else if (keyword == "xy,z,w")
					lines = 3;
				break;
		}

		#endregion


		#region parse textureImporterCheckers
		if (mp.type == MaterialProperty.PropType.Texture)
		{
			textureImporterCheckers.Clear();

			if (!string.IsNullOrEmpty(importerRulesInfo))
			{
				string[] checkerInfo = importerRulesInfo.Split(',');

				if (checkerInfo.Length > 0)
				{
					foreach (string info in checkerInfo)
						textureImporterCheckers.Add(new GUITextureImporterChecker(mp.textureValue, info));
				}
			}
		}
		#endregion
	}

	void ParseCachedLabels(Material mat)
	{
		if (displayLabel.Contains("@"))
		{
			string[] cachedLabelInfo = displayLabel.Split('@');
			string k = cachedLabelInfo[0];
			string v = cachedLabelInfo[1];

			if (!LabelCaches.ContainsKey(k) || !mat.HasProperty(v)) return;

			int value = (int)mat.GetFloat(v);

			if (LabelCaches[k].Length > value)
			{
				displayLabel = LabelCaches[k][value];
			}
		}
	}

	#endregion


	#region Draw GUI

	public void Draw(MaterialEditor materialEditor, Dictionary<string, bool> marcos, List<ShaderPropertie> featureCheckers)
	{

		switch (materialProperty.type)
		{
			case MaterialProperty.PropType.Float:
				DrawFloat(materialEditor, marcos, featureCheckers);
				break;
			case MaterialProperty.PropType.Color:
				DrawColor(materialEditor, marcos);
				break;
			case MaterialProperty.PropType.Range:
				DrawRange(materialEditor, marcos);
				break;
			case MaterialProperty.PropType.Vector:
				DrawVector(materialEditor, marcos);
				break;
			case MaterialProperty.PropType.Texture:
				DrawTexture(materialEditor, marcos);
				break;
			default:
				materialEditor.DefaultShaderProperty(materialProperty, displayLabel);
				break;
		}
	}

	static GUIStyle _bigFont;
	static GUIStyle bigFont
	{
		get
		{
			if (_bigFont == null)
			{
				_bigFont = new GUIStyle(GUI.skin.label);
				_bigFont.fontSize = 10;
				_bigFont.richText = true;
			}

			return _bigFont;
		}
	}

	Rect DrawNameLine(string displaystr, string descstr, int indent = 0, int heightOffset = 0)
	{
		var rect = EditorGUILayout.GetControlRect();
		rect = new Rect(rect.xMin + indent, rect.yMin, rect.width - indent, rect.height);
		var rectBg = new Rect(rect.xMin, rect.yMin, rect.width, rect.height * 2 + heightOffset + 2);
		EditorGUI.DrawRect(rectBg, new Color(0.0f, 0.0f, 0.0f, 0.4f * (30 - indent) / 30.0f));

		if (descstr.Length > 0) displaystr += "   " + "<color=#404040ff>" + descstr + "</color>";

		var rectfont = new Rect(rect.xMin, rect.yMin + 2, rect.width, rect.height);
		// EditorGUI.LabelField(rectfont, displaystr, bigFont);
        EditorGUI.LabelField(rectfont, displaystr);

		return rectBg;
	}

	static Rect GetIndentRect(int indent = 20, int padding = 5)
	{
		Rect rect = EditorGUILayout.GetControlRect();
		rect = new Rect(rect.xMin + indent + padding, rect.yMin, rect.width - indent - padding * 2, rect.height);
		return rect;
	}

	Rect GetIndentRectTex(int indent = 20, int padding = 5)
	{
		Rect rect = EditorGUILayout.GetControlRect();
		rect = new Rect(rect.xMin + indent + padding, rect.yMin, rect.width - indent - padding * 2, 80);
		return rect;
	}

	void DrawFloat(MaterialEditor materialEditor, Dictionary<string, bool> marcos, List<ShaderPropertie> featureCheckers)
	{
		Material mat = materialEditor.target as Material;

		bool pre = mat.GetFloat(materialProperty.name) > 0;

		switch (type)
		{
			case PType.Group:
				EditorGUILayout.Separator();
				bool cur = EditorGUILayout.Foldout(pre, displayLabel);
				if (cur != pre)
					mat.SetFloat(materialProperty.name, cur ? 1 : 0);
				break;
			case PType.ToggleWithKeyword:
				Rect rect = EditorGUILayout.GetControlRect();
				cur = EditorGUI.Toggle(rect, "    " + displayLabel, marcos[keyword]);
				if (cur != marcos[keyword])
					marcos[keyword] = cur;
				break;
			case PType.Toggle:
				rect = EditorGUILayout.GetControlRect();
				cur = EditorGUI.Toggle(rect, "    " + displayLabel, pre);

				if (cur != pre)
					mat.SetFloat(materialProperty.name, cur ? 1 : 0);
				break;
			case PType.TogglePass:
				rect = EditorGUILayout.GetControlRect();
				cur = EditorGUI.Toggle(rect, "    " + displayLabel, mat.GetShaderPassEnabled(keyword));
				mat.SetShaderPassEnabled(keyword, cur);
				mat.SetFloat(materialProperty.name, cur ? 1 : 0);
				break;
			case PType.Label:
				EditorGUI.indentLevel++;
				foreach (var tmpLabel in labels)
				{
					rect = EditorGUILayout.GetControlRect();
					EditorGUI.LabelField(rect, tmpLabel, bigFont);
				}
				EditorGUI.indentLevel--;
				break;
			case PType.EnumWithKeywords:
				int EnumNums = keywords.Length;
				string[] Keys = new string[EnumNums];
				for (int i = 0; i < EnumNums; ++i)
				{
					Keys[i] = keywords[i];
					if (!marcos.ContainsKey(Keys[i]))
					{
						marcos[Keys[i]] = false;
					}
				}

				int lastValue = (int)mat.GetFloat(materialProperty.name);
				int lastIndex = enumVals.IndexOf(lastValue);

				EditorGUI.indentLevel++;
				int curIndex = EditorGUILayout.Popup(displayLabel, lastIndex, labels);
				int curValue = enumVals[curIndex];
				EditorGUI.indentLevel--;

				if (curValue != lastValue)
				{
					mat.SetFloat(materialProperty.name, (float)curValue);
					marcos[Keys[lastIndex]] = false;
					marcos[Keys[curIndex]] = true;
				}
				break;
			case PType.Enum:
				List<int> values = enumVals;

				lastValue = (int)mat.GetFloat(materialProperty.name);
				lastValue = values.IndexOf(lastValue);
				EditorGUI.indentLevel++;
				curIndex = EditorGUILayout.Popup(displayLabel, lastValue, labels);
				EditorGUI.indentLevel--;
				curValue = values[curIndex];

				if (curValue != lastValue)
					mat.SetFloat(materialProperty.name, (float)curValue);

				break;
			case PType.Space:
				GUILayout.Space(15 * lines);
				break;
			case PType.BlendMode:
				SShaderGUIUtils.BlendModeGUI(materialEditor, displayLabel, materialProperty.name, labels[0], labels[1]);
				break;

			case PType.FeatureChecker:
				rect = EditorGUILayout.GetControlRect();
				cur = EditorGUI.Toggle(rect, "    " + displayLabel, pre);

				if (cur != pre) {
					int curVal = cur ? 1 : 0;
					mat.SetFloat(materialProperty.name, curVal);
					materialProperty.floatValue = curVal;

					string lastKey = PackedFeaturesKey;
					PackedFeaturesKey = CombineFeaturesKey(featureCheckers);

					if (
						!PackedShaderInfo.PackedShaders.ContainsKey(PackedShaderInfo.currentGroupName) || 
						!PackedShaderInfo.PackedShaders[PackedShaderInfo.currentGroupName].ContainsKey(PackedFeaturesKey)
					)
					{
						EditorUtility.DisplayDialog("提示", "不支持的特性组合", "OK");
						mat.SetFloat(materialProperty.name, pre ? 1 : 0);
						PackedFeaturesKey = lastKey;
					}
				}
				break;
			case PType.FeatureGroup:
				lastValue = (int)materialProperty.floatValue;
				EditorGUI.indentLevel++;
				curValue = EditorGUILayout.Popup(displayLabel, lastValue, labels);
				EditorGUI.indentLevel--;

				if (curValue != lastValue) {
					mat.SetFloat(materialProperty.name, curValue);
					materialProperty.floatValue = curValue;
					
					string lastGroupName = PackedShaderInfo.currentGroupName;
					PackedShaderInfo.currentGroupName = keywords[curValue];

					string lastKey = PackedFeaturesKey;
					PackedFeaturesKey = CombineFeaturesKey(featureCheckers);

					if (
						!PackedShaderInfo.PackedShaders.ContainsKey(PackedShaderInfo.currentGroupName) ||
						!PackedShaderInfo.PackedShaders[PackedShaderInfo.currentGroupName].ContainsKey(PackedFeaturesKey)
					)
					{
						EditorUtility.DisplayDialog("提示", "不支持的特性组合", "OK");

						mat.SetFloat(materialProperty.name, lastValue);
						PackedFeaturesKey = lastKey;
						PackedShaderInfo.currentGroupName = lastGroupName;
					}
					
				}
				break;
			default:

				rect = DrawNameLine(displayLabel + string.Format("({0})", materialProperty.name), "", 20, -14);
				rect.y += 2;
				rect.height -= 4;
				rect.width -= 23;
				materialEditor.FloatProperty(rect, materialProperty, " ");
				GUILayout.Space(4);

				break;
		}
	}

	void DrawColor(MaterialEditor materialEditor, Dictionary<string, bool> marcos)
	{
		DrawNameLine(displayLabel + string.Format("({0})", materialProperty.name), "", 20);
		materialEditor.ColorProperty(GetIndentRect(), materialProperty, "");
	}

	void DrawRange(MaterialEditor materialEditor, Dictionary<string, bool> marcos)
	{
		DrawNameLine(displayLabel + string.Format("({0})", materialProperty.name), "", 20);
		materialEditor.RangeProperty(GetIndentRect(), materialProperty, "");
	}

	void DrawVector(MaterialEditor materialEditor, Dictionary<string, bool> marcos)
	{
		Material mat = materialEditor.target as Material;

		switch (type)
		{
			case PType.Vector:
				Vector4 values = mat.GetVector(materialProperty.name);
				DrawNameLine(displayLabel, "", 20, 60 - 20 * (4 - lines));
				EditorGUI.indentLevel += 3;

				EditorGUI.BeginChangeCheck();
				for (int i = 0; i < lines; i++)
				{
					if (string.IsNullOrEmpty(labels[i]))
						continue;

					if (ranges[i] != Vector2.zero)
						values[i] = EditorGUILayout.Slider(labels[i], values[i], ranges[i].x, ranges[i].y);
					else
						values[i] = EditorGUILayout.FloatField(labels[i], values[i]);
				}

				if (EditorGUI.EndChangeCheck())
					mat.SetVector(materialProperty.name, values);

				EditorGUI.indentLevel -= 3;
				break;
			case PType.VectorPlus:
				if (lines == 1)
				{
					DrawNameLine(displayLabel, "", 20, 5);
					materialEditor.VectorProperty(GetIndentRect(), materialProperty, "");
				}
				else
				{
					values = mat.GetVector(materialProperty.name);
					DrawNameLine(displayLabel, "", 20, 60 - 20 * (4 - lines));
					EditorGUI.indentLevel += 3;

					switch (keyword)
					{
						case "xyz,w":
							EditorGUI.BeginChangeCheck();
							Vector3 v3 = new Vector3(values.x, values.y, values.z);
							float v = values.w;
							v3 = EditorGUILayout.Vector3Field(labels[0], v3);
							if (ranges[1] != Vector2.zero)
								v = EditorGUILayout.Slider(labels[1], v, ranges[1].x, ranges[1].y);
							else
								v = EditorGUILayout.FloatField(labels[1], v);
							if (EditorGUI.EndChangeCheck())
								values = new Vector4(v3.x, v3.y, v3.z, v);
							break;
						case "xy,zw":
							EditorGUI.BeginChangeCheck();
							Vector2 v2_0 = new Vector2(values.x, values.y);
							Vector2 v2_1 = new Vector2(values.z, values.w);
							v2_0 = EditorGUILayout.Vector2Field(labels[0], v2_0);
							v2_1 = EditorGUILayout.Vector2Field(labels[1], v2_1);
							if (EditorGUI.EndChangeCheck())
								values = new Vector4(v2_0.x, v2_0.y, v2_1.x, v2_1.y);
							break;
						case "xy,z,w":
							EditorGUI.BeginChangeCheck();
							Vector2 v2 = new Vector2(values.x, values.y);
							float v_1 = values.z;
							float v_2 = values.w;
							v2 = EditorGUILayout.Vector2Field(labels[0], v2);
							if (ranges[1] != Vector2.zero)
								v_1 = EditorGUILayout.Slider(labels[1], v_1, ranges[1].x, ranges[1].y);
							else
								v_1 = EditorGUILayout.FloatField(labels[1], v_1);
							if (ranges[2] != Vector2.zero)
								v_2 = EditorGUILayout.Slider(labels[2], v_2, ranges[2].x, ranges[2].y);
							else
								v_2 = EditorGUILayout.FloatField(labels[2], v_2);
							if (EditorGUI.EndChangeCheck())
								values = new Vector4(v2.x, v2.y, v_1, v_2);
							break;
					}

					mat.SetVector(materialProperty.name, values);

					EditorGUI.indentLevel -= 3;
					break;
				}
				break;
			default:
				DrawNameLine(displayLabel, "", 20, 5);
				materialEditor.VectorProperty(GetIndentRect(), materialProperty, "");
				break;
		}

		GUILayout.Space(5);
	}

	void DrawTexture(MaterialEditor materialEditor, Dictionary<string, bool> marcos)
	{
		List<GUITextureImporterChecker> failedCheckers = new List<GUITextureImporterChecker>();
		if (textureImporterCheckers.Count > 0)
		{

			foreach (var checker in textureImporterCheckers)
			{
				if (!checker.Check())
				{
					failedCheckers.Add(checker);
				}
			}
		}

		if (failedCheckers.Count > 0)
		{
			DrawNameLine(displayLabel + string.Format("({0})", materialProperty.name), "", 20, 70);
			DrawNameLine(GUITextureImporterChecker.CHECKER_FAILED_WARRING, "", 20, -14);

			Rect rect = GetIndentRect(240, 5);
			rect.y -= 16;

			if (GUI.Button(rect, "fix"))
			{
				foreach (var checker in failedCheckers)
				{
					checker.Fix();
				}

				failedCheckers[0].importer.SaveAndReimport();
			}

			rect = GetIndentRectTex();
			rect.y -= 16;

			materialEditor.TextureProperty(rect, materialProperty, "");
		}
		else
		{
			DrawNameLine(displayLabel + string.Format("({0})", materialProperty.name), "", 20, 50);
			materialEditor.TextureProperty(GetIndentRectTex(), materialProperty, "");
		}

		GUILayout.Space(50);
	}

	public static string CombineFeaturesKey(List<ShaderPropertie> featureCheckers) {
		string key = "";
		List<int> featureIDs = new List<int>();
		foreach (var prop in featureCheckers)
		{
			if (prop.materialProperty.floatValue < 1) continue;

			string featureName = prop.keywordsInfo;
			int featureID = PackedShaderInfo.PackedFeatureNames.IndexOf(featureName);
			featureIDs.Add(featureID);
		}
		featureIDs.Sort();
		for (int i = 0; i < featureIDs.Count; ++i)
		{
			key += featureIDs[i].ToString();

			if (i < featureIDs.Count - 1) key += ",";
		}

		return key;
	}

	#endregion
}


public class GUITextureImporterChecker
{
	public const string CHECKER_FAILED_WARRING = "<color=#ff4000ff>!ImporterSetting is not all correct!</color>";

	public enum CheckType
	{
		sRGB,
		Size,
		Type,
	}

	/// <summary>
	/// 检查贴图类型时对应的字符串
	/// </summary>
	Dictionary<string, TextureImporterType> texTypeNames = new Dictionary<string, TextureImporterType> {
		{"default",TextureImporterType.Default},
		{"normal",TextureImporterType.NormalMap},
		{"gui",TextureImporterType.GUI},
		{"sprite",TextureImporterType.Sprite},
		{"cursor",TextureImporterType.Cursor},
		{"cookie",TextureImporterType.Cookie},
		{"lightmap",TextureImporterType.Lightmap},
		{"single",TextureImporterType.SingleChannel},
	};


	public CheckType checkType = CheckType.sRGB;

	public TextureImporter importer;

	Texture texture;
	bool toggle = true;
	int value = 0;
	string info = "";

	public GUITextureImporterChecker(Texture texture, string constructionLine)
	{
		this.texture = texture;

		string texturePath = AssetDatabase.GetAssetPath(texture);
		importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);

		ParsePropertives(constructionLine);
	}

	void ParsePropertives(string constructionLine)
	{
		string[] infoStrs = constructionLine.Split(':');

		string typeStr = infoStrs[0].ToLower();
		info = infoStrs[1].ToLower();
		if (typeStr == "srgb")
		{
			checkType = CheckType.sRGB;
			toggle = (info == "true" || info == "on" || info == "1");
		}
		else if (typeStr == "size")
		{
			checkType = CheckType.Size;
			int.TryParse(info, out value);
		}
		else if (typeStr == "type")
		{
			checkType = CheckType.Type;
		}
	}

	public bool Check()
	{
		bool pass = true;

		switch (checkType)
		{
			case CheckType.sRGB:
				pass = CheckSRGB();
				break;
			case CheckType.Size:
				pass = CheckSize();
				break;
			case CheckType.Type:
				pass = CheckTexType();
				break;
		}

		return pass;
	}

	public void Fix()
	{
		switch (checkType)
		{
			case CheckType.sRGB:
				FixSRGB();
				break;
			case CheckType.Size:
				FixSize();
				break;
			case CheckType.Type:
				FixTexType();
				break;
		}
	}

	bool CheckSRGB()
	{
		if (!importer) return true;

		return importer.sRGBTexture == toggle && importer.textureType == TextureImporterType.Default;
	}

	bool CheckSize()
	{
		if (!importer) return true;

		return importer.maxTextureSize <= value;
	}

	bool CheckTexType()
	{
		if (!importer) return true;
		if (!texTypeNames.ContainsKey(info)) return true;

		return importer.textureType == texTypeNames[info];
	}

	void FixSRGB()
	{
		if (!importer) return;

		importer.textureType = TextureImporterType.Default;
		importer.sRGBTexture = toggle;
	}

	void FixSize()
	{
		if (!importer) return;

		importer.maxTextureSize = value;
	}

	void FixTexType()
	{
		if (!importer) return;
		importer.textureType = texTypeNames[info];
	}
}


public class SShaderGUI : ShaderGUI
{
	protected Dictionary<string, bool> marcos = new Dictionary<string, bool>();
	string[] keyWords = new string[] { };
	bool hasPackedShaders = false;
	bool skipDrawGUI = false;

	bool checkDisplayToggle(Material targetMat, string displayField, ShaderPropertie mp, List<string> toggleFields, List<string> enumFields, bool isFirst, bool isNegative)
	{
		if (toggleFields.Contains(displayField))
		{
			if (targetMat.HasProperty(displayField))
			{
				bool toggle = targetMat.GetFloat(displayField) > 0.5f;

				if (isNegative) toggle = !toggle;

				if (!toggle) return false;
			}
		}

		if (enumFields.Contains(displayField))
		{
			int mark = displayField.LastIndexOf('_');
			string propertyName = displayField.Substring(0, mark);

			if (targetMat.HasProperty(propertyName))
			{
				string fieldValueStr = displayField.Substring(mark + 1, displayField.Length - mark - 1);
				int fieldValue;
				int.TryParse(fieldValueStr, out fieldValue);

				bool toggle = (int)targetMat.GetFloat(propertyName) == fieldValue;
				if (isNegative) toggle = !toggle;
				if (!toggle) return false;
			}
		}

		if (mp.type != ShaderPropertie.PType.Group)
		{
			string groupName = "Group#" + displayField;
			if (targetMat.HasProperty(groupName))
			{
				bool groupOpen = targetMat.GetFloat(groupName) > 0;
				if (isNegative) groupOpen = !groupOpen;

				if (!groupOpen && isFirst) return false;
			}
		}

		if (isNegative)
		{
			if (marcos.ContainsKey(displayField) && keyWords.Contains(displayField))
			{
				return false;
			}
		}
		else
		{
			if (marcos.ContainsKey(displayField) && !keyWords.Contains(displayField))
			{
				return false;
			}
		}


		return true;
	}


	override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		if (skipDrawGUI)
		{
			skipDrawGUI = false;
			materialEditor.Repaint();
			return;
		}

		PackedShaderInfo.currentGroupName = "";

		ShaderPropertie featureGroup = null;
		Material mat = materialEditor.target as Material;
		List<ShaderPropertie> shaderProperties = new List<ShaderPropertie>();
		List<ShaderPropertie> allShaderProperties = new List<ShaderPropertie>();
		List<ShaderPropertie> featureCheckers = new List<ShaderPropertie>();
		List<PackedShaderInfo> packedShaderInfos = new List<PackedShaderInfo>();
		List<StripPackedRule> stripPackedRules = new List<StripPackedRule>();

		ShaderPropertie.LabelCaches = new Dictionary<string, string[]>();

		#region prepair shaderProperties
		PackedShaderInfo.PackedShaders.Clear();
		hasPackedShaders = false;
		foreach (var mp in properties)
		{
			if (mp.name.StartsWith(ShaderPropertie.PACKED_SHADER_NAME)) {
				hasPackedShaders = true;
				packedShaderInfos.Add(new PackedShaderInfo(mp.displayName,(int)mp.floatValue));
				continue;
			}

			if (mp.displayName.StartsWith("S(")) {
				stripPackedRules.Add(new StripPackedRule(mp.displayName));
				continue;
			}

			ShaderPropertie sp = new ShaderPropertie(mp, mat);

			if (sp.type == ShaderPropertie.PType.FeatureChecker)
				featureCheckers.Add(sp);

			if (sp.type == ShaderPropertie.PType.FeatureGroup)
				featureGroup = sp;

			if (!sp.ignoreDisplay)
				shaderProperties.Add(sp);

			allShaderProperties.Add(sp);
		}
		#endregion


		#region switch packedShaders
		if (hasPackedShaders)
		{
			PackedShaderInfo.CollectPackedShaders(packedShaderInfos, stripPackedRules);

			ShaderPropertie.PackedFeaturesKey = ShaderPropertie.CombineFeaturesKey(featureCheckers);

			PackedShaderInfo.currentGroupName = "";
			if (featureGroup != null) {
				int curGroup = (int)featureGroup.materialProperty.floatValue;
				PackedShaderInfo.currentGroupName = featureGroup.keywords[curGroup];
			}

			if (PackedShaderInfo.PackedShaders.ContainsKey(PackedShaderInfo.currentGroupName)) {

				if (PackedShaderInfo.PackedShaders[PackedShaderInfo.currentGroupName].ContainsKey(ShaderPropertie.PackedFeaturesKey) &&
					PackedShaderInfo.PackedShaders[PackedShaderInfo.currentGroupName][ShaderPropertie.PackedFeaturesKey] &&
					mat.shader != PackedShaderInfo.PackedShaders[PackedShaderInfo.currentGroupName][ShaderPropertie.PackedFeaturesKey])
				{
					mat.shader = PackedShaderInfo.PackedShaders[PackedShaderInfo.currentGroupName][ShaderPropertie.PackedFeaturesKey];
					skipDrawGUI = true;
					return;
				}
			}
		}
		#endregion


		#region Prepair Keywords rules
		List<string> toggleFields = new List<string>();
		List<string> enumFields = new List<string>();
		List<ShaderPropertie> keywordLinks = new List<ShaderPropertie>();
		List<ShaderPropertie> keywordStrips = new List<ShaderPropertie>();
		List<ShaderPropertie> keywordWithAnyEnumActives = new List<ShaderPropertie>();
		Dictionary<ShaderPropertie, bool> keywordWithAnyEnumActiveDic = new Dictionary<ShaderPropertie, bool>();

		foreach (var sp in allShaderProperties)
		{
			switch (sp.type)
			{
				case ShaderPropertie.PType.Toggle:
					toggleFields.Add(sp.materialProperty.name);
					break;
				case ShaderPropertie.PType.TogglePass:
					toggleFields.Add(sp.materialProperty.name);
					break;
				case ShaderPropertie.PType.Enum:
					foreach (int v in sp.enumVals)
						enumFields.Add(string.Format("{0}_{1}", sp.materialProperty.name, v));
					break;
				case ShaderPropertie.PType.KeywordLink:
					keywordLinks.Add(sp);
					break;
				case ShaderPropertie.PType.KeywordStrip:
					keywordStrips.Add(sp);
					break;
				case ShaderPropertie.PType.KeywordWithAnyEnumActive:
					keywordWithAnyEnumActiveDic[sp] = false;
					keywordWithAnyEnumActives.Add(sp);
					break;
			}
		}
		#endregion


		#region refresh marcos
		keyWords = mat.shaderKeywords;
		Dictionary<string, bool> tempKey = new Dictionary<string, bool>();

		foreach (var sp in shaderProperties)
		{
			if (sp.type == ShaderPropertie.PType.ToggleWithKeyword)
			{
				string keyword = sp.keyword;
				if (!tempKey.ContainsKey(keyword))
					tempKey.Add(sp.keyword, false);
			}

			if (sp.type == ShaderPropertie.PType.EnumWithKeywords)
			{
				var keywordArray = sp.keywords;

				foreach (var word in keywordArray)
				{
					if (!tempKey.ContainsKey(word))
						tempKey.Add(word, false);
				}
			}

		}

		foreach (var sp in keywordWithAnyEnumActiveDic.Keys)
		{
			if (sp.type == ShaderPropertie.PType.KeywordWithAnyEnumActive)
			{
				if (!string.IsNullOrEmpty(sp.keyword))
				{
					if (!tempKey.ContainsKey(sp.keyword))
					{
						tempKey.Add(sp.keyword, false);
					}
				}
			}
		}

		foreach (var sp in keywordLinks)
		{
			if (!tempKey.ContainsKey(sp.keyword))
			{
				tempKey.Add(sp.keyword, false);
			}
		}


		foreach (var sp in keywordStrips)
		{
			if (!tempKey.ContainsKey(sp.keyword))
			{
				tempKey.Add(sp.keyword, false);
			}
		}





		marcos.Clear();
		marcos = tempKey;

		foreach (var sp in shaderProperties)
		{
			if (sp.type == ShaderPropertie.PType.ToggleWithKeyword)
			{
				string keyword = sp.keyword;
				if (keyWords.Contains(keyword))
					marcos[keyword] = true;
				else
					marcos[keyword] = false;
			}
			if (sp.type == ShaderPropertie.PType.EnumWithKeywords)
			{
				var keywordArray = sp.keywords;

				bool allfalse = true;
				for (int i = 0; i < keywordArray.Length; i++)
				{
					var word = keywordArray[i];
					if (keyWords.Contains(word))
					{
						allfalse = false;
						marcos[word] = true;
					}
					else
					{
						marcos[word] = false;
					}

				}
				if (allfalse)
				{
					marcos[keywordArray[0]] = true;
				}
			}
		}
		#endregion


		foreach (var prop in shaderProperties)
		{
			#region toggleDisplay
			bool displayToggle = true;
			for (int i = 0; i < prop.displayFields.Length; ++i)
			{
				string fieldStr = prop.displayFields[i];

				string[] fields = fieldStr.Split('|');
				displayToggle = false;
				foreach (string f in fields)
				{
					bool isNegative = false;
					string field = f;

					if (field.StartsWith("!"))
					{
						field = field.Substring(1);
						isNegative = true;
					}

					if (checkDisplayToggle(mat, field, prop, toggleFields, enumFields, i == 0, isNegative))
					{
						displayToggle = true;
						break;
					}
				}

				if (!displayToggle) break;
			}
			if (!displayToggle) continue;
			#endregion

			#region init EmumKeywords
			if (prop.type == ShaderPropertie.PType.EnumWithKeywords)
			{
				int lastValue = (int)mat.GetFloat(prop.materialProperty.name);
				int lastIndex = prop.enumVals.IndexOf(lastValue);
				string keyword = prop.keywords[lastIndex];
				if (!string.IsNullOrEmpty(keyword) && !mat.shaderKeywords.Contains<string>(keyword))
				{
					mat.EnableKeyword(keyword);
					materialEditor.Repaint();
				}
			}
			#endregion

			prop.Draw(materialEditor, marcos, featureCheckers);

			#region prepaire keywordWithAnyEnumActives
			foreach (ShaderPropertie sp in keywordWithAnyEnumActives)
			{
				if (sp.labels.Contains<string>(prop.materialProperty.name))
				{

					int curVal = (int)prop.materialProperty.floatValue;
					int curEnum = prop.enumVals[curVal];
					int curRule = (int)sp.materialProperty.floatValue;

					if (curEnum == curRule)
						keywordWithAnyEnumActiveDic[sp] = true;
				}
			}
			#endregion
		}


		#region Rule Keywords
		foreach (var sp in keywordWithAnyEnumActiveDic.Keys)
		{
			if (keywordWithAnyEnumActiveDic[sp])
				marcos[sp.keyword] = true;
			else
				marcos[sp.keyword] = false;
		}

		foreach (var sp in keywordLinks)
		{
			if (marcos.ContainsKey(sp.keyword) && marcos[sp.keyword])
			{
				foreach (string keyword in sp.keywords)
					marcos[keyword] = true;
			}
		}

		foreach (var sp in keywordStrips)
		{
			if (marcos.ContainsKey(sp.keyword) && marcos[sp.keyword])
			{
				foreach (string keyword in sp.keywords)
					marcos[keyword] = false;
			}
		}
		#endregion


		#region update keywords
		var newkeywords = new List<string> { };
		foreach (var item in marcos)
		{
			if (item.Value)
				newkeywords.Add(item.Key);
		}

		mat.shaderKeywords = newkeywords.ToArray();
		EditorUtility.SetDirty(mat);
		#endregion


		EditorGUILayout.Separator();
		materialEditor.RenderQueueField();
		materialEditor.EnableInstancingField();
	}





}
