// Copyright (c) anatawa12 2022
// Published under CC0 Licesnse or Unlicesnse at your option
// see https://creativecommons.org/publicdomain/zero/1.0/legalcode for CC0
// see https://unlicense.org/ for Unlicense

// The .cs file to log compilation to some file. useful with tail -f
// `tail -f compileLog.txt` to see the compilation progress continuously
// `rm compileLog.txt; touch compileLog.txt; tail -f compileLog.txt` to clear history and see progress

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_257fbbebd9b7dab8bf39c0c710a2bfc7)
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;

namespace anatawa12.gists
{
    [InitializeOnLoad]
    public static class CompileLogger
    {
        private const string Path = "compileLog.txt";
        static CompileLogger()
        {
            CompilationPipeline.compilationStarted += asm =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Compile Start\n");
            CompilationPipeline.assemblyCompilationStarted += asm =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Compile STR: {asm}\n");
            CompilationPipeline.assemblyCompilationFinished += (asm, _) =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Compile FNH: {asm}\n");
            CompilationPipeline.compilationFinished += asm =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Compile Finish\n");
            AssetDatabase.importPackageStarted += pkg =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Import Package STR: {pkg}\n");
            AssetDatabase.importPackageCancelled += pkg =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Import Package CCL: {pkg}\n");
            AssetDatabase.importPackageFailed += (pkg, err) =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Import Package FAL: {pkg}: {err}\n");
            AssetDatabase.importPackageCompleted += pkg =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: Import Package FNH: {pkg}\n");
            AssemblyReloadEvents.beforeAssemblyReload += () =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: beforeAssemblyReload\n");
            AssemblyReloadEvents.afterAssemblyReload += () =>
                File.AppendAllText(Path, $"{DateTime.Now:O}: afterAssemblyReload\n");
        }
    }
}
#endif
