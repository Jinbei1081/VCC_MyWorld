/*
 * CompilationLogWindow
 * https://gist.github.com/anatawa12/5987f6b5357c3c91603fa07f215dfeab
 *
 * The window to see compilation progress
 *
 * Copy this cs file to anywhere in your asset folder is the only step to install this tool.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_5987f6b5357c3c91603fa07f215dfeab)

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace anatawa12.gists
{
    internal class CompilationLogWindow : EditorWindow
    {
        [SerializeField] private Vector2 scroll;
        [SerializeField] private List<CompilingAssemblyInfo> compilingAssemblyInfos = new List<CompilingAssemblyInfo>();
        [SerializeField] private List<CompiledAssemblyInfo> compiledAssemblies = new List<CompiledAssemblyInfo>();

        private void OnEnable()
        {
            CompilationPipeline.compilationStarted += CompilationStarted;
            CompilationPipeline.compilationFinished += CompilationFinished;
            CompilationPipeline.assemblyCompilationStarted += AssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinished;
        }

        private void OnDisable()
        {
            CompilationPipeline.compilationStarted -= CompilationStarted;
            CompilationPipeline.compilationFinished -= CompilationFinished;
            CompilationPipeline.assemblyCompilationStarted -= AssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= AssemblyCompilationFinished;
        }

        [MenuItem("Tools/anatawa12 gists/CompilationLogWindow")]
        static void Create() => GetWindow<CompilationLogWindow>("Compilation Log Window");

        private void OnGUI()
        {
            var logStyle = new GUIStyle(EditorStyles.label) { wordWrap = false };

            if (compilingAssemblyInfos.Count == 0)
                GUILayout.Label("No compilation in progress", logStyle);

            // show compiling assemblies
            foreach (var compilingAssemblyInfo in compilingAssemblyInfos)
                GUILayout.Label($"since {compilingAssemblyInfo.StartTime:hh:mm:ss}: compiling {compilingAssemblyInfo.ShortName}", logStyle);

            HorizontalLine(5f);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // show compiled
            for (var i = compiledAssemblies.Count - 1; i >= 0; i--)
                GUILayout.Label($"since {compiledAssemblies[i].StartTime:hh:mm:ss} for {PrettyPrint(compiledAssemblies[i].Duration)} : " +
                                $"{compiledAssemblies[i].ShortName}", logStyle);

            EditorGUILayout.EndScrollView();
        }

        private void HorizontalLine(float height, float width = 1f)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);

            rect.y += (rect.height - width) / 2;
            rect.height = width;

            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private string PrettyPrint(TimeSpan span)
        {
            if (span < TimeSpan.FromSeconds(1))
            {
                return $"{span.TotalMilliseconds} ms";
            }
            else if (span < TimeSpan.FromSeconds(100))
            {
                return $"{span.TotalSeconds:.00} sec";
            }
            else if (span < TimeSpan.FromMinutes(100))
            {
                return $"{span.TotalMinutes:.00} min";
            }
            else if (span < TimeSpan.FromDays(100))
            {
                return $"{span.TotalHours:.00} days";
            }
            else
            {
                return $"{span.TotalDays:.00} days";
            }
        }

        private void CompilationFinished(object obj)
        {
            
        }

        private void CompilationStarted(object obj)
        {
            compiledAssemblies.Clear();
            compilingAssemblyInfos.Clear();
            Repaint();
        }

        private void AssemblyCompilationFinished(string asmName, CompilerMessage[] arg2)
        {
            var index = compilingAssemblyInfos.FindIndex(x => x.name == asmName);
            if (index != -1)
            {
                compiledAssemblies.Add(compilingAssemblyInfos[index].Finish());
                compilingAssemblyInfos.RemoveAt(index);
            }
            Repaint();
        }

        private void AssemblyCompilationStarted(string asmName)
        {
            compilingAssemblyInfos.Add(new CompilingAssemblyInfo(asmName));
            Repaint();
        }

        [Serializable]
        class CompiledAssemblyInfo
        {
            [SerializeField] private long startTime;
            [SerializeField] private long duration;
            public string name;
            public string ShortName => name.Substring(name.LastIndexOf('/') + 1);
            public DateTime StartTime => new DateTime(startTime);
            public TimeSpan Duration => new TimeSpan(duration);

            public CompiledAssemblyInfo(string name, DateTime startTime, TimeSpan duration)
            {
                this.name = name;
                this.startTime = startTime.Ticks;
                this.duration = duration.Ticks;
            }
        }

        [Serializable]
        class CompilingAssemblyInfo
        {
            [SerializeField] private long startTime;
            public string name;
            public DateTime StartTime => new DateTime(startTime);
            public string ShortName => name.Substring(name.LastIndexOf('/') + 1);

            public CompilingAssemblyInfo([NotNull] string name, DateTime startTime)
            {
                this.name = name ?? throw new ArgumentNullException(nameof(name));
                this.startTime = startTime.Ticks;
            }

            public CompilingAssemblyInfo(string name) : this(name, DateTime.Now)
            {
            }

            public CompiledAssemblyInfo Finish(DateTime? at = null) =>
                new CompiledAssemblyInfo(name, StartTime, (at ?? DateTime.Now) - StartTime);
        }
    }
}

#endif
