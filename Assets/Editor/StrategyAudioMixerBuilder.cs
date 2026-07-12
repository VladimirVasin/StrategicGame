using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectUnknown.Strategy.EditorTools
{
    [InitializeOnLoad]
    internal static class StrategyAudioMixerBuilder
    {
        private const string MixerPath = "Assets/Resources/Audio/StrategyAudioMixer.mixer";
        private static readonly BindingFlags Flags = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        static StrategyAudioMixerBuilder()
        {
            EditorApplication.delayCall += EnsureMixerExists;
        }

        [MenuItem("ProjectUnknown/Audio/Rebuild Strategy Mixer")]
        private static void RebuildMixer()
        {
            AssetDatabase.DeleteAsset(MixerPath);
            EnsureMixerExists();
        }

        private static void EnsureMixerExists()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(MixerPath) ?? "Assets/Resources/Audio");
            Type controllerType = FindEditorType("UnityEditor.Audio.AudioMixerController");
            MethodInfo createMixer = controllerType?.GetMethod("CreateMixerControllerAtPath", Flags);
            if (controllerType == null || createMixer == null)
            {
                Debug.LogWarning("Strategy audio mixer builder could not find Unity's mixer controller API.");
                return;
            }

            try
            {
                object controller = createMixer.Invoke(null, new object[] { MixerPath });
                MethodInfo createGroup = FindCreateGroupMethod(controllerType);
                if (controller == null || createGroup == null)
                {
                    throw new InvalidOperationException("Mixer controller or group factory is unavailable.");
                }

                string[] groupNames = Enum.GetNames(typeof(StrategyAudioBus));
                for (int i = 0; i < groupNames.Length; i++)
                {
                    if (groupNames[i] != nameof(StrategyAudioBus.Master))
                    {
                        createGroup.Invoke(controller, BuildCreateGroupArguments(createGroup, groupNames[i]));
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("Strategy audio mixer created with " + groupNames.Length + " routed buses.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Strategy audio mixer generation failed: " + exception.GetBaseException().Message);
            }
        }

        private static Type FindEditorType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static MethodInfo FindCreateGroupMethod(Type controllerType)
        {
            MethodInfo[] methods = controllerType.GetMethods(Flags);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "CreateNewGroup")
                {
                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(string))
                    {
                        return methods[i];
                    }
                }
            }

            return null;
        }

        private static object[] BuildCreateGroupArguments(MethodInfo method, string groupName)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == 0)
                {
                    arguments[i] = groupName;
                }
                else if (parameters[i].ParameterType == typeof(bool))
                {
                    arguments[i] = true;
                }
                else
                {
                    arguments[i] = parameters[i].HasDefaultValue
                        ? parameters[i].DefaultValue
                        : null;
                }
            }

            return arguments;
        }
    }
}
