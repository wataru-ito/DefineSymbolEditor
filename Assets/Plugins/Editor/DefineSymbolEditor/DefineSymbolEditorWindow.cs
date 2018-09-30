using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DefineSymbolEditor
{
	class DefineSymbolEditorWindow : EditorWindow
	{
		readonly BuildTargetGroup[] kTargets = 
		{
			BuildTargetGroup.Standalone,
			BuildTargetGroup.iOS,
			BuildTargetGroup.Android,
			BuildTargetGroup.WebGL,
			BuildTargetGroup.WSA,
			BuildTargetGroup.Tizen,
			BuildTargetGroup.XboxOne,
			BuildTargetGroup.PSP2,
			BuildTargetGroup.PS4,
			#if !UNITY_2017_3_OR_NEWER
			BuildTargetGroup.SamsungTV,
			#endif
			BuildTargetGroup.N3DS,
			BuildTargetGroup.WiiU,
			BuildTargetGroup.tvOS,
			BuildTargetGroup.Facebook,
			BuildTargetGroup.Switch,
		};

		const float kTargetItemHeight = 36f;
		const float kTargetIconSize = 32f;

		DefineSymbolData m_data;
		DefineSymbolContext m_context;
		DefineSymbolStatus m_statusCommon;

		DefineSymbolPlatformData[] m_platforms;
		DefineSymbolPlatformData m_current;

		string[] m_presetLabels;
		string[] m_presetDeleteLabels;
        string m_presetName;

		Action m_mode;

		GUIStyle m_platformStyle;
		GUIStyle m_entryBackEvenStyle;
		GUIStyle m_entryBackOddStyle;
		Vector2 m_targetScrollPosition;
		Vector2 m_settingScrollPosition;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		static DefineSymbolEditorWindow s_instane;

		[MenuItem("Tools/Build/DefineSymbol Editor")]
		static void Open()
		{
			if (s_instane) return;

			s_instane = CreateInstance<DefineSymbolEditorWindow>();
			s_instane.ShowUtility();
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		void OnEnable()
		{
			s_instane = this;

			titleContent = new GUIContent("DefineSymbol Editor");
			minSize = new Vector2(570f, 380f);

			m_platforms = Array.ConvertAll(kTargets, i => new DefineSymbolPlatformData(i));
			m_current = Array.Find(m_platforms, i => i.target == EditorUserBuildSettings.selectedBuildTargetGroup) ?? m_platforms[0];
			
            m_data = DefineSymbolData.Load();			
			m_context = new DefineSymbolContext(m_data.context);

            InitGUI();
            UpdatePresetLabels();
            SetSymbolMode();
        }

        void OnDestroy()
        {
            if (s_instane == this)
            {
                s_instane = null;
            }
        }

        void OnGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				const float kPaddingSide = 12f;
				GUILayout.Space(kPaddingSide);
				using (new EditorGUILayout.VerticalScope())
				{
					GUILayout.Space(8);
					DrawHeader();
					GUILayout.Space(2);
					using (new EditorGUILayout.HorizontalScope())
					{
						using (new EditorGUILayout.VerticalScope(GUILayout.Width(250f + 8f)))
						{
							DrawPlatformList();
						}

						using (new EditorGUILayout.VerticalScope())
						{
							using (var scroll = new EditorGUILayout.ScrollViewScope(m_settingScrollPosition))
							{
								m_mode();
								m_settingScrollPosition = scroll.scrollPosition;
							}
						}
					}

					GUILayout.Space(8);
					DrawFooter();
					GUILayout.Space(8);
				}
				GUILayout.Space(kPaddingSide);
			}
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void InitGUI()
		{
            var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			m_platformStyle = skin.FindStyle("PlayerSettingsPlatform");
			m_entryBackEvenStyle = skin.FindStyle("OL EntryBackEven");
			m_entryBackOddStyle = skin.FindStyle("OL EntryBackOdd");
		}

		void DrawBuildTargetIcon(Rect itemPosition, DefineSymbolPlatformData platform)
		{
			var icon = itemPosition;
			icon.x += 2;
			icon.y += (icon.height - kTargetIconSize) * 0.5f;
			icon.width = icon.height = kTargetIconSize;
			GUI.DrawTexture(icon, platform.icon);

			itemPosition.x = icon.xMax + 4f;
			itemPosition.y += (itemPosition.height - 16f) * 0.5f;
			itemPosition.height = 16f;
			GUI.Label(itemPosition, platform.target.ToString());
		}

		void DrawHeader()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(" Platform", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();
				
                var index = EditorGUILayout.Popup(0, m_presetLabels, GUILayout.Width(90));
                if (index > 0)
				{
                    OnPresetSelected(index);
				}
				
                index = EditorGUILayout.Popup(0, m_presetDeleteLabels, GUILayout.Width(40));
				if (index > 0)
				{
					OnDeletePresetSelected(index);
				}
			}
		}

		void DrawFooter()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				var kFooterBtnWidth = GUILayout.Width(108);
				GUI.enabled = m_mode == DrawSymbolMode;
				if (GUILayout.Button("シンボル定義編集", kFooterBtnWidth))
				{
					SetContextMode();
				}
				GUI.enabled = true;

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Apply", kFooterBtnWidth))
				{
					OnApply();
				}
				if (GUILayout.Button("Revert", kFooterBtnWidth))
				{
					OnRevert();
				}
			}
		}

		void OnApply()
		{
			if (m_mode == DrawContextMode)
			{
				m_data.context = new DefineSymbolContext(m_context);
				m_data.Save();
				SetSymbolMode();
			}
			else
			{
				m_data.commonSymbols = m_statusCommon.ToSymbols();
				m_data.Save();
				foreach (var platform in m_platforms)
				{
					PlayerSettings.SetScriptingDefineSymbolsForGroup(platform.target,
						m_data.targets.Contains(platform.target) ? platform.status.ToSymbols() : string.Empty);
				}
				Close();
			}
		}

		void OnRevert()
		{
			if (m_mode == DrawContextMode)
			{
				m_context = new DefineSymbolContext(m_data.context);
				SetSymbolMode();
			}
			else
			{
				Close();
			}
		}


		//------------------------------------------------------
		// preset
		//------------------------------------------------------

		void UpdatePresetLabels()
		{
            m_presetLabels = new string[m_data.presets.Count + 4];
            int indexer = 0;
            m_presetLabels[indexer++] = "プリセット選択";
			m_presetLabels[indexer++] = string.Empty;
			for (int i = 0; i < m_data.presets.Count; ++i)
			{
				m_presetLabels[indexer++] = m_data.presets[i].name;
			}
			m_presetLabels[indexer++] = string.Empty;
			m_presetLabels[indexer++] = "保存";


			m_presetDeleteLabels = new string[m_data.presets.Count + 2];
            indexer = 0;
            m_presetDeleteLabels[indexer++] = "削除";
			m_presetDeleteLabels[indexer++] = string.Empty;
			for (int i = 0; i < m_data.presets.Count; ++i)
			{
				m_presetDeleteLabels[indexer++] = m_data.presets[i].name;
			}
		}

		void OnPresetSelected(int index)
		{
			index -= 2;
			if (index < 0) return;

			if (index < m_data.presets.Count)
			{
                var preset = m_data.presets[index];
                m_presetName = preset.name;
                UpdatePlatformStatus(preset);
				return;
			}

            var win = DefineSymbolPresetCreateWindow.Open(m_presetName, name =>
			{
				var preset = DefineSymbolPreset.Create(name, m_statusCommon, Array.ConvertAll(m_platforms, i => i.status));
				m_data.AddPreset(preset);
				m_data.Save();
				UpdatePresetLabels();
                m_presetName = name;
            });

            var pos = win.position;
            pos.position = new Vector2(
                position.x + position.width - pos.width,
                position.y);
            win.position = pos;
        }

		void OnDeletePresetSelected(int index)
		{
			index -= 2;
			if (index < 0) return;

			if (index < m_data.presets.Count)
			{
				m_data.presets.RemoveAt(index);
				m_data.Save();
				UpdatePresetLabels();
			}
		}


		//------------------------------------------------------
		// platform list
		//------------------------------------------------------

		void DrawPlatformList()
		{
			GUILayout.Box(GUIContent.none, GUILayout.Width(250f), GUILayout.ExpandHeight(true));
			var position = GUILayoutUtility.GetLastRect();
			position.x += 1f;
			position.y += 1f;
			position.width -= 1f;
			position.height -= 1f;

			var viewRect = new Rect(0, 0, position.width - 16f, kTargetItemHeight * m_platforms.Length);
			using (var scroll = new GUI.ScrollViewScope(position, m_targetScrollPosition, viewRect))
			{
				for (int i = 0; i < m_platforms.Length; ++i)
				{
					var itemRect = new Rect(0, kTargetItemHeight * i, viewRect.width, kTargetItemHeight);
					PlatformField(itemRect, m_platforms[i], i % 2 == 0 ? m_entryBackEvenStyle : m_entryBackOddStyle);
				}

				m_targetScrollPosition = scroll.scrollPosition;
			}
		}

		void PlatformField(Rect itemPosition, DefineSymbolPlatformData platform, GUIStyle backStyle)
		{
			var togglePosition = new Rect(
				itemPosition.xMax - 32f,
				itemPosition.y + (itemPosition.height - 16f) * 0.5f,
				16f, 16f);

			var ev = Event.current;
			switch (ev.type)
			{
				case EventType.Repaint:
					backStyle.Draw(itemPosition, false, false, platform == m_current, false);
					
					var icon = itemPosition;
					icon.x += 2;
					icon.y += (icon.height - kTargetIconSize) * 0.5f;
					icon.width = icon.height = kTargetIconSize;
					GUI.DrawTexture(icon, platform.icon);
					
					m_platformStyle.Draw(itemPosition,
						new GUIContent(platform.name),
						focusedWindow == this, false, platform == m_current, false);
					break;

				case EventType.MouseDown:
					if (itemPosition.Contains(ev.mousePosition) && ev.button == 0)
					{
						// トグルにかぶってたら処理しない = ev.Use()しちゃうとトグルが反応しなくなる
						if (!togglePosition.Contains(ev.mousePosition))
						{
							m_current = platform;
							ev.Use();
						}
					}
					break;
			}

			var isTarget = m_data.targets.Contains(platform.target);
			if (GUI.Toggle(togglePosition, isTarget, GUIContent.none) != isTarget)
			{
				if (!isTarget)
				{
					m_data.targets.Add(platform.target);
					m_data.targets.Sort((x, y) => Array.FindIndex(m_platforms, i => i.target == x).CompareTo(Array.FindIndex(m_platforms, i => i.target == y)));
				}
				else
				{
					m_data.targets.Remove(platform.target);
				}
			}	
		}


		//------------------------------------------------------
		// symbol mode
		//------------------------------------------------------

		void SetSymbolMode()
		{
			m_mode = DrawSymbolMode;
			UpdatePlatformStatus(m_data);
		}

		void UpdatePlatformStatus(IDefineSymbolData data)
		{
			DefineSymbolContext commonContext, indivisualContext;
			m_context.Split(out commonContext, out indivisualContext);

			m_statusCommon = new DefineSymbolStatus(BuildTargetGroup.Unknown, null, commonContext, data.GetCommonSymbols());

			foreach (var platform in m_platforms)
			{
				platform.status = new DefineSymbolStatus(platform.target, m_statusCommon, indivisualContext,
					data.GetScriptingDefineSymbolsForGroup(platform.target));
			}
		}

		void DrawSymbolMode()
		{
			var targetEnabled = m_data.targets.Contains(m_current.target);
			GUI.enabled = targetEnabled;

			GUILayout.Box(GUIContent.none, GUI.skin.label, GUILayout.Height(32), GUILayout.ExpandWidth((true)));
			DrawBuildTargetIcon(GUILayoutUtility.GetLastRect(), m_current);

			EditorGUILayout.Space();
			DrawEditStatus(m_current.status);

			GUI.enabled = true;
			if (!targetEnabled)
			{
				EditorGUILayout.HelpBox("このプラットフォームを有効にするにはチェックを入れてください", MessageType.Info);
			}
		}

		void DrawEditStatus(DefineSymbolStatus status)
		{
			if (status.common != null)
			{
				EditorGUILayout.LabelField("共通");
				DrawEditStatus(status.common);
				EditorGUILayout.Space();
			}

			status.toggles.ForEach(EditStatusToggle);
			status.dropdowns.ForEach(EditStatusDropdown);
		}

		void EditStatusToggle(ToggleStatus toggle)
		{
			toggle.enabled = EditorGUILayout.Toggle(toggle.context.content, toggle.enabled);
		}

		void EditStatusDropdown(DropdownStatus dropdown)
		{
			dropdown.index = EditorGUILayout.Popup(dropdown.context.content, dropdown.index, dropdown.context.displayedOptions);
		}


		//------------------------------------------------------
		// context mode
		//------------------------------------------------------

		void SetContextMode()
		{
			m_mode = DrawContextMode;
		}

		void DrawContextMode()
		{
			var prev = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 80f;

			DrawEdit("Toggle", m_context.toggles, DrawEditToggle, CreateToggle);
			DrawEdit("Dropdown", m_context.dropdowns, DrawEditDropdown, CreateDropdown);

			EditorGUIUtility.labelWidth = prev;
		}

		void DrawEdit<T>(string label, List<T> list, Func<T,bool> drawer, Func<T> createInstance)
			where T : SymbolContext
		{
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			for (int i = 0; i < list.Count; ++i)
			{
				using (new EditorGUILayout.VerticalScope("box"))
				{
					var deleteFlag = drawer(list[i]);
					if (deleteFlag)
					{
						list.RemoveAt(i--);
					}
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("定義追加"))
				{
					list.Add(createInstance());
				}
			}
		}

		const float kBtnWidth = 38f;

		bool DrawEditSymbol(SymbolContext symbol)
		{
			var deleteFlag = false;
			using (new EditorGUILayout.HorizontalScope())
			{
				symbol.name = EditorGUILayout.TextField("名前", symbol.name);
				deleteFlag = GUILayout.Button("削除", EditorStyles.miniButton, GUILayout.Width(kBtnWidth));
			}
			symbol.description = EditorGUILayout.TextField("説明", symbol.description);
			symbol.individual = EditorGUILayout.Toggle("Platform別", symbol.individual);

			return deleteFlag;
		}

		bool DrawEditToggle(ToggleContext toggle)
		{
			return DrawEditSymbol(toggle);
		}

		bool DrawEditDropdown(DropdownContext dropdown)
		{
			var deleteFlg = DrawEditSymbol(dropdown);
			++EditorGUI.indentLevel;

			for (int j = 0; j < dropdown.items.Count; ++j)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					dropdown.items[j] = EditorGUILayout.TextField(dropdown.items[j]);
					if (GUILayout.Button("削除", EditorStyles.miniButton, GUILayout.Width(kBtnWidth)))
					{
						dropdown.items.RemoveAt(j--);
					}
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("選択追加"))
				{
					dropdown.items.Add("ITEM" + dropdown.items.Count);
				}
			}

			--EditorGUI.indentLevel;

			return deleteFlg;
		}

		ToggleContext CreateToggle()
		{
			var toggle = new ToggleContext();
			toggle.name = "TOGGLE" + m_context.toggles.Count;
			toggle.description = string.Empty;
			return toggle;
		}

		DropdownContext CreateDropdown()
		{
			var dropdown = new DropdownContext();
			dropdown.name = "DROPDOWN" + m_context.dropdowns.Count;
			dropdown.description = string.Empty;
			dropdown.items.Add("ITEM0");
			return dropdown;
		}
	}
}