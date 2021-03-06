﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace DefineSymbolEditor
{
    /// <summary>
    /// ローカルに保存されるデータ
    /// 形式はjson
    /// </summary>
	[Serializable]
	class DefineSymbolData : IDefineSymbolData
	{
		const string kFilePath = "ProjectSettings/DefineSymbolEditor.txt";

		public List<BuildTargetGroup> targets;
		public DefineSymbolContext context;
		public string commonSymbols;
		public List<DefineSymbolPreset> presets;


        //------------------------------------------------------
        // lifecycle
        //------------------------------------------------------

        public DefineSymbolData()
		{
			targets = new List<BuildTargetGroup>();
			context = new DefineSymbolContext();
			commonSymbols = string.Empty;
			presets = new List<DefineSymbolPreset>();
		}


		//------------------------------------------------------
		// io
		//------------------------------------------------------

		public static DefineSymbolData Load()
		{
			try
			{
				if (!File.Exists(kFilePath))
				{
					return new DefineSymbolData();
				}

				var json = File.ReadAllText(kFilePath);
				return JsonUtility.FromJson<DefineSymbolData>(json);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				return new DefineSymbolData();
			}
		}

		public void Save()
		{
			try
			{
				var json = EditorJsonUtility.ToJson(this);
				json = Regex.Replace(json, @"[,\[\{]", i => i.Value + "\n");
				json = Regex.Replace(json, @"[\]\}]", i => "\n" + i.Value);
				File.WriteAllText(kFilePath, json, System.Text.Encoding.UTF8);
			}
			catch (Exception)
			{
			}
		}


		//------------------------------------------------------
		// IDefineSymbolData
		//------------------------------------------------------

		public string GetCommonSymbols()
		{
			return commonSymbols;
		}

		public string GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup)
		{
			return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
		}


        //------------------------------------------------------
        // preset
        //------------------------------------------------------

        public void AddPreset(DefineSymbolPreset preset)
        {
            var index = presets.FindIndex(i => i.name == preset.name);
            if (index >= 0)
            {
                presets[index] = preset;
            }
            else
            {
                presets.Add(preset);
                presets.Sort((x, y) => x.name.CompareTo(y.name));
            }
        }

		public string GetPresetSymbols(string presetName, BuildTargetGroup targetGroup)
		{
			var preset = presets.Find(i => i.name == presetName);
			if (preset == null) 
				return string.Empty;
			
			var index = Array.FindIndex(preset.symbols, i => i.target == targetGroup);
			if (index < 0)
				return string.Empty;
			
			// プリセット保存時には有効だったプラットフォームが今は無効化されている事もある
			if (!targets.Contains(targetGroup))
				return string.Empty;

			return preset.symbols[index].symbol;
		}
	}
}