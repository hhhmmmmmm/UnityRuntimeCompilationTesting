using System.IO;
using System.Reflection;
using UnityEngine;
using System.Linq;

public class ScriptTest : MonoBehaviour
{

    void Start()
    {
        var wrapper = new CompilerWrapper();

        // load text files and run them
        foreach (var file in Directory.GetFiles(Application.streamingAssetsPath, "*.txt"))
        {
            wrapper.Execute(file);
            Debug.Log($"Read file {Path.GetFileName(file)}, errors: {wrapper.ErrorsCount}, result: {wrapper.GetReport()}");
        }

        // see what we got! this includes built-ins as well as loaded ones
        var characters = wrapper.CreateInstancesOf<ICharacter>();
        foreach (var character in characters)
        {
            Debug.Log($"Character of type {character.GetType()}, speaks {character.Language}, height = {character.Height} cm");
        }
    }

    private void ReflectedTypeMethodCallTest(object targetObject, string methodName)
    {
        var method = targetObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name == methodName)
            .FirstOrDefault();
        float startTime = Time.realtimeSinceStartup;
        var o = method.Invoke(targetObject, null);

        Debug.LogError($"{(Time.realtimeSinceStartup - startTime)*1000}ms: {o}");
    }
}