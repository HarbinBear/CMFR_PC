using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ReplaceShader : Editor
{
    [MenuItem("Toooool/Replace Shader")]
    static void ReplaceShadersInMaterials()
    {
        // 获取要更改shader的路径
        string strPath = "Assets/Content";
        string strShaderName= "ToyRP/gbuffer";

        Shader shader = Shader.Find(strShaderName);
        if (shader == null)
        {
            Debug.LogError("Shader " + strShaderName + " not found.");
            return;
        }
        // 在指定路径下找到所有的材质
        string[] strGuids = AssetDatabase.FindAssets("t:material", new string[] { strPath });
        foreach (string strGuid in strGuids)
        {
            // 获取到具体的材质资源
            string strAssetPath = AssetDatabase.GUIDToAssetPath(strGuid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(strAssetPath);
            
            // 替换Shader
            material.shader = shader;

            Debug.Log("Replaced shader in material " + material.name);
        }
    }
}