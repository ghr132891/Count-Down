using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AutoSaveTool : EditorWindow
{
    private bool isAutoSaveEnabled = true;
    private float saveIntervalMinutes = 5f; // 默认 5 分钟保存一次
    private double lastSaveTime;

    // 在顶部菜单栏创建一个入口
    [MenuItem("Tools/Auto Save Settings (自动保存设置)")]
    public static void ShowWindow()
    {
        GetWindow<AutoSaveTool>("自动保存设置");
    }

    private void OnEnable()
    {
        // 读取本地偏好设置，如果没设置过则使用默认值
        isAutoSaveEnabled = EditorPrefs.GetBool("AutoSave_Enabled", true);
        saveIntervalMinutes = EditorPrefs.GetFloat("AutoSave_Interval", 5f);
        lastSaveTime = EditorApplication.timeSinceStartup;

        // 注册编辑器的 Update 回调
        EditorApplication.update += OnUpdate;
    }

    private void OnDisable()
    {
        // 窗口关闭或脚本重载时，保存当前设置
        EditorPrefs.SetBool("AutoSave_Enabled", isAutoSaveEnabled);
        EditorPrefs.SetFloat("AutoSave_Interval", saveIntervalMinutes);

        // 移除回调，防止内存泄漏
        EditorApplication.update -= OnUpdate;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("自动保存参数配置", EditorStyles.boldLabel);
        GUILayout.Space(5);

        isAutoSaveEnabled = EditorGUILayout.Toggle("启用自动保存", isAutoSaveEnabled);

        // 如果启用，则显示时间设置
        GUI.enabled = isAutoSaveEnabled;
        saveIntervalMinutes = EditorGUILayout.FloatField("保存间隔 (分钟)", saveIntervalMinutes);
        // 限制最小间隔，防止设置太小导致编辑器卡顿
        if (saveIntervalMinutes < 0.5f) saveIntervalMinutes = 0.5f;
        GUI.enabled = true;

        GUILayout.Space(15);
        if (GUILayout.Button("手动执行一次保存 (Save Now)", GUILayout.Height(30)))
        {
            PerformSave();
        }
    }

    private void OnUpdate()
    {
        // 如果未启用，或者正在运行游戏，或者正在编译代码，则不执行自动保存
        if (!isAutoSaveEnabled || EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            // 重置计时器，防止退出运行模式后瞬间触发保存
            lastSaveTime = EditorApplication.timeSinceStartup;
            return;
        }

        // 计算距离上次保存是否达到了设定的时间
        if (EditorApplication.timeSinceStartup - lastSaveTime > saveIntervalMinutes * 60f)
        {
            PerformSave();
        }
    }

    private void PerformSave()
    {
        lastSaveTime = EditorApplication.timeSinceStartup;

        // 保存当前所有打开的场景
        EditorSceneManager.SaveOpenScenes();

        // 保存 Project 面板里的资产（如 Prefab、材质、ScriptableObject 等）
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>[AutoSave Tool]</color> 场景和资源已自动保存于: {System.DateTime.Now:HH:mm:ss}");
    }
}