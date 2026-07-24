using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Field cho phép kéo-thả trực tiếp 1 file Scene (.unity) vào Inspector, giống hệt cách kéo
/// 1 GameObject/Prefab vào 1 field object thông thường. Dùng thay cho việc gõ tay tên scene
/// dạng string (dễ gõ sai/khi đổi tên scene quên sửa lại field).
///
/// Cách dùng: khai báo public SceneField mySceneField; trong script, kéo scene vào Inspector,
/// rồi lấy tên bằng mySceneField.SceneName (hoặc dùng thẳng như string nhờ implicit operator).
///
/// LƯU Ý: Scene kéo vào đây vẫn phải được add vào File > Build Settings > Scenes In Build
/// thì mới load được lúc chạy game thật (kéo vào field này không tự động thêm vào Build Settings).
/// </summary>
[System.Serializable]
public class SceneField
{
    [SerializeField] private Object sceneAsset;
    [SerializeField] private string sceneName = "";

    /// <summary>Tên scene, dùng để truyền vào SceneManager.LoadScene / SceneLoader.LoadScene.</summary>
    public string SceneName => sceneName;

    // Cho phép dùng trực tiếp 1 biến SceneField ở chỗ nào đang cần string (ví dụ LoadScene(mySceneField))
    public static implicit operator string(SceneField sceneField)
    {
        return sceneField != null ? sceneField.sceneName : null;
    }
}

#if UNITY_EDITOR
/// <summary>Custom Inspector drawer: hiện field SceneField dưới dạng ô kéo-thả Scene asset.</summary>
[CustomPropertyDrawer(typeof(SceneField))]
public class SceneFieldPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, GUIContent.none, property);

        SerializedProperty sceneAssetProp = property.FindPropertyRelative("sceneAsset");
        SerializedProperty sceneNameProp = property.FindPropertyRelative("sceneName");

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        EditorGUI.BeginChangeCheck();
        Object newSceneAsset = EditorGUI.ObjectField(position, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            sceneAssetProp.objectReferenceValue = newSceneAsset;
            sceneNameProp.stringValue = newSceneAsset != null ? newSceneAsset.name : "";
        }

        EditorGUI.EndProperty();
    }
}
#endif