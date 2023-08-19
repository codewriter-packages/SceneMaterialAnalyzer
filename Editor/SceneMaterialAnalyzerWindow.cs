using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;

public class SceneMaterialAnalyzerWindow : EditorWindow
{
    [MenuItem("Window/Analysis/Scene Material Analyzer")]
    public static void ShowSceneViewer()
    {
        var window = GetWindow<SceneMaterialAnalyzerWindow>();
        window.titleContent = new GUIContent("Mat Analyzer");
        window.Show();
    }

    private List<MaterialInfo> _materials;
    private MaterialInfo _currentInfo;
    private Vector2 _materialsScroll;
    private Vector3 _rendersScroll;

    private void OnEnable()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += EditorSceneManager_sceneClosing;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
    }

    private void OnDisable()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= EditorSceneManager_sceneClosing;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
    }

    private void EditorSceneManager_sceneClosing(Scene scene, bool removingScene)
    {
        _materials = null;
        _currentInfo = null;
    }

    private void EditorSceneManager_sceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        RefreshMaterials();
    }

    private void OnGUI()
    {
        using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshMaterials();
            }

            GUILayout.FlexibleSpace();
        }

        if (_materials == null)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Analyze", GUILayout.Width(160), GUILayout.Height(30)))
                    {
                        RefreshMaterials();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }

            return;
        }

        using (new GUILayout.HorizontalScope())
        {
            using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.MaxWidth(position.width / 2f - 10)))
            {
                DrawMaterials();
            }

            using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.MaxWidth(position.width / 2f - 10)))
            {
                DrawSelectedMaterialInfo();
            }
        }
    }

    private void DrawMaterials()
    {
        GUILayout.Label("Materials in scene", EditorStyles.boldLabel);

        var shader = default(Shader);

        _materialsScroll = GUILayout.BeginScrollView(_materialsScroll);

        foreach (var entry in _materials)
        {
            var prevColor = GUI.color;
            GUI.color = entry == _currentInfo ? Color.cyan : prevColor;

            if (shader != entry.Material.shader)
            {
                GUILayout.Space(5);
                GUILayout.Label(entry.Material.shader.name, EditorStyles.miniLabel);
                shader = entry.Material.shader;
            }

            if (GUILayout.Button(entry.Material.name, EditorStyles.objectField))
            {
                _currentInfo = entry;
            }

            GUI.color = prevColor;
        }

        GUILayout.EndScrollView();
    }

    private void DrawSelectedMaterialInfo()
    {
        GUILayout.Label("GameObjects with selected Material", EditorStyles.boldLabel);

        if (_currentInfo == null)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow &&
                _materials.Count > 0)
            {
                _currentInfo = _materials[0];
                Repaint();
                Event.current.Use();
                return;
            }

            EditorGUILayout.HelpBox("No material selected", MessageType.Info);
            return;
        }

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.DownArrow)
            {
                var currentInfoIndex = _materials.IndexOf(_currentInfo);
                _currentInfo = _materials[(currentInfoIndex + 1) % _materials.Count];
                Repaint();
                Event.current.Use();
                return;
            }
            else
            {
                var currentInfoIndex = _materials.IndexOf(_currentInfo);
                currentInfoIndex = currentInfoIndex > 0 ? (currentInfoIndex - 1) : _materials.Count - 1;
                _currentInfo = _materials[currentInfoIndex];
                Repaint();
                Event.current.Use();
                return;
            }
        }

        GUILayout.Label("Shader: " + _currentInfo.Material.shader.name, EditorStyles.largeLabel);

        _rendersScroll = GUILayout.BeginScrollView(_rendersScroll);

        foreach (var rend in _currentInfo.Renders)
        {
            var renderObject = rend.gameObject;

            if (GUILayout.Button(renderObject.name, EditorStyles.objectField))
            {
                Selection.activeGameObject = renderObject;
                EditorGUIUtility.PingObject(renderObject);
            }
        }

        GUILayout.EndScrollView();
    }

    private void RefreshMaterials()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPaused = true;
        }

        _materials = FindObjectsOfType<Renderer>()
            .SelectMany(o =>
            {
                return o.sharedMaterials.Select(m => new
                {
                    renderer = o,
                    material = m,
                });
            })
            .Where(o => o.material != null)
            .GroupBy(o => o.material)
            .Select(o =>
            {
                var renders = o.Select(p => p.renderer).ToList();
                return new MaterialInfo(o.Key, renders);
            })
            .OrderBy(o => o.Material.shader.name)
            .ToList();
    }

    private class MaterialInfo
    {
        public MaterialInfo(Material material, List<Renderer> renders)
        {
            Material = material;
            Renders = renders;
        }

        public Material Material { get; }
        public List<Renderer> Renders { get; }
    }
}