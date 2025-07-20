/*
 * CreateAssemblyDefinitionForUdonSharp
 * https://gist.github.com/anatawa12/5987f6b5357c3c91603fa07f215dfeab
 *
 * Adds menu to create assembly definition with U# assembly definition
 *
 * MIT License
 * 
 * Copyright (c) 2023 anatawa12
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_def5f8a29179ecbcb45502fa3b4590ce)
using System.IO;
using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

//to avoid conflict, I use UUID for namespace 
namespace anatawa12.gists
{
    public static class CreateAssemblyDefinitionForUdonSharp
    {
        // TODO: Find other references like TMPro
        private const string TemplateAsmDef = @"{
    ""name"": ""<NAME>"",
    ""references"": [
        ""GUID:99835874ee819da44948776e0df4ff1d""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": false,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}";

        [MenuItem("Assets/Create/Assembly Definition pair for U#", false, 98)]
        private static void CreateUSharpScript()
        {
            string folderPath = "Assets/";
            if (Selection.activeObject != null)
            {
                folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (Selection.activeObject.GetType() != typeof(DefaultAsset))
                {
                    folderPath = Path.GetDirectoryName(folderPath);
                }
            }
            else if (Selection.assetGUIDs.Length > 0)
            {
                folderPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            }

            folderPath = folderPath.Replace('\\', '/');
            
            var filePath = EditorUtility.SaveFilePanelInProject("AssemblyDefinition", "", 
                "asmdef", 
                "Save Assembly Definition file", folderPath);

            if (filePath.Length <= 0) return;
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // ReSharper disable once AssignNullToNotNullAttribute
            string asmDefPath = Path.Combine(Path.GetDirectoryName(filePath), $"{fileName}.asmdef");
            // ReSharper disable once AssignNullToNotNullAttribute
            string udonAsmDefPath = Path.Combine(Path.GetDirectoryName(filePath), $"{fileName}.asset");

            if (!CheckForOverwrite(asmDefPath)) return;
            if (!CheckForOverwrite(udonAsmDefPath)) return;

            File.WriteAllText(asmDefPath, TemplateAsmDef.Replace("<NAME>", fileName), System.Text.Encoding.UTF8);

            AssetDatabase.ImportAsset(asmDefPath, ImportAssetOptions.ForceSynchronousImport);
            var asmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmDefPath);

            UdonSharpAssemblyDefinition newProgramAsset = ScriptableObject.CreateInstance<UdonSharpAssemblyDefinition>();
            newProgramAsset.sourceAssembly = asmdef;
            AssetDatabase.CreateAsset(newProgramAsset, udonAsmDefPath);
            AssetDatabase.Refresh();
        }

        private static bool CheckForOverwrite(string file)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(file) != null)
            {
                if (!EditorUtility.DisplayDialog("File already exists", 
                        $"Corresponding asset file '{file}' already found. Overwrite?", "Ok", "Cancel"))
                    return false;
            }

            return true;
        }
    }
}
#endif
