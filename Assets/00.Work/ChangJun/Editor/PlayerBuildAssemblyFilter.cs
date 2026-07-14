#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace ChangJun.Editor
{
    /// <summary>
    /// Editor-only Roslyn/MCP assemblies must not ship in player builds.
    /// </summary>
    internal sealed class PlayerBuildAssemblyFilter : IFilterBuildAssemblies
    {
        public int callbackOrder => 0;

        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            return assemblies.Where(assembly => !IsEditorOnlyAssembly(assembly)).ToArray();
        }

        static bool IsEditorOnlyAssembly(string assemblyPath)
        {
            var name = Path.GetFileName(assemblyPath);
            return name.StartsWith("Microsoft.CodeAnalysis")
                || name.StartsWith("System.Collections.Immutable")
                || name.StartsWith("MCPForUnity");
        }
    }
}
#endif
