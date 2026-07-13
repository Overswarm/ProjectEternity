// Minimal UnityEngine API stub — compile-check only, never shipped.
// Covers exactly the surface used by Assets/Scripts.
using System;

namespace UnityEngine
{
    public static class Mathf
    {
        public static float Abs(float v) => Math.Abs(v);
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 1e-6f;
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Log10(float v) => (float)Math.Log10(v);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Max(params float[] values) { float m = values[0]; foreach (var v in values) m = Math.Max(m, v); return m; }
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float MoveTowards(float cur, float target, float maxDelta)
            => Math.Abs(target - cur) <= maxDelta ? target : cur + Math.Sign(target - cur) * maxDelta;
        // real value-noise so headless worldgen resembles Unity's PerlinNoise
        public static float PerlinNoise(float x, float y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf), v = yf * yf * (3f - 2f * yf);
            float a = NoiseVal(xi, yi), b = NoiseVal(xi + 1, yi);
            float c = NoiseVal(xi, yi + 1), d = NoiseVal(xi + 1, yi + 1);
            return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
        }
        static float NoiseVal(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static float Distance(Vector2 a, Vector2 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        public static Vector2 MoveTowards(Vector2 cur, Vector2 target, float maxDelta)
        {
            float d = Distance(cur, target);
            if (d <= maxDelta || d == 0f) return target;
            return new Vector2(cur.x + (target.x - cur.x) / d * maxDelta, cur.y + (target.y - cur.y) / d * maxDelta);
        }
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
    }

    public struct Quaternion
    {
        public static Quaternion Euler(float x, float y, float z) => new Quaternion();
        public static Quaternion LookRotation(Vector3 fwd) => new Quaternion();
        public static Vector3 operator *(Quaternion q, Vector3 v) => v;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color Lerp(Color x, Color y, float t) => x;
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public bool Contains(Vector2 p) => p.x >= x && p.x < x + width && p.y >= y && p.y < y + height;
    }

    public struct Ray { }
    public struct RaycastHit { public Collider collider; }

    public class Object
    {
        public string name;
        public static void Destroy(Object obj) { }
        public static T[] FindObjectsOfType<T>() where T : Object => new T[0];
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T : Object => new T[0];
    }

    public class Texture : Object { }
    public class Texture2D : Texture { public static Texture2D whiteTexture => new Texture2D(); }

    public class Shader : Object
    {
        public static Shader Find(string name) => new Shader();
        public static int PropertyToID(string name) => 0;
    }

    public class Material : Object
    {
        public Material(Shader s) { }
        public bool enableInstancing;
        public void SetFloat(string name, float v) { }
    }

    public class MaterialPropertyBlock
    {
        public void SetColor(int id, Color c) { }
    }

    public class Font : Object { public Material material; }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object => default(T);
    }

    public enum FindObjectsSortMode { None, InstanceID }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum LightType { Spot, Directional, Point, Area }
    public enum CameraClearFlags { Skybox = 1, SolidColor = 2 }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextAlignment { Left, Center, Right }
    public enum EventType { Repaint = 7, Layout = 8 }
    public enum KeyCode
    {
        Space, Escape, Alpha1, Alpha2, Alpha3, Alpha4,
        A, D, E, Q, S, W,
        UpArrow, DownArrow, RightArrow, LeftArrow,
    }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }

    public class Component : Object
    {
        public Transform transform => new Transform();
        public GameObject gameObject => new GameObject("stub");
        public T GetComponent<T>() => default(T);
    }

    public class Transform : Component
    {
        public Vector3 position;
        public Vector3 localPosition;
        public Vector3 localScale;
        public Quaternion rotation;
        public Transform parent;
        public void SetParent(Transform p) { }
    }

    public class GameObject : Object
    {
        public GameObject(string name) { }
        public GameObject() { }
        public string tag;
        public Transform transform => new Transform();
        public static GameObject CreatePrimitive(PrimitiveType t) => new GameObject();
        public T AddComponent<T>() where T : Component, new() => new T();
        public T GetComponent<T>() => default(T);
        public void SetActive(bool active) { }
    }

    public class Behaviour : Component { public bool enabled; }

    public class MonoBehaviour : Behaviour { }

    public class Collider : Component { }
    public class Renderer : Component
    {
        public Material sharedMaterial;
        public void SetPropertyBlock(MaterialPropertyBlock b) { }
    }
    public class MeshRenderer : Renderer { }

    public class Light : Behaviour
    {
        public LightType type;
        public float intensity;
        public Color color;
    }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public float farClipPlane;
        public Ray ScreenPointToRay(Vector3 pos) => new Ray();
    }

    public class TextMesh : Component
    {
        public Font font;
        public float characterSize;
        public int fontSize;
        public TextAnchor anchor;
        public TextAlignment alignment;
        public string text;
        public Color color;
    }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDist)
        { hit = new RaycastHit(); return false; }
    }

    public static class Input
    {
        public static bool GetKey(KeyCode k) => false;
        public static bool GetKeyDown(KeyCode k) => false;
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static float GetAxis(string axis) => 0f;
        public static Vector3 mousePosition => new Vector3();
    }

    public static class Time
    {
        public static float unscaledDeltaTime => 0.016f;
        public static float deltaTime => 0.016f;
    }

    public static class Screen
    {
        public static int width => 1920;
        public static int height => 1080;
    }

    public static class RenderSettings
    {
        public static Color ambientLight { get; set; }
    }

    // ---------------- IMGUI ----------------
    public class GUIContent { public GUIContent(string text) { } }

    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public int fontSize;
        public FontStyle fontStyle;
        public bool wordWrap;
        public Vector2 CalcSize(GUIContent content) => new Vector2(100, 20);
    }

    public class GUISkin : Object
    {
        public GUIStyle label = new GUIStyle();
        public GUIStyle button = new GUIStyle();
        public GUIStyle box = new GUIStyle();
    }

    public class Event
    {
        public static Event current => new Event();
        public Vector2 mousePosition;
        public EventType type;
    }

    public static class GUI
    {
        public static Color color { get; set; }
        public static bool enabled { get; set; }
        public static GUISkin skin => new GUISkin();
        public static void Box(Rect r, string text) { }
        public static void Label(Rect r, string text) { }
        public static void Label(Rect r, string text, GUIStyle style) { }
        public static void DrawTexture(Rect r, Texture t) { }
    }

    public class GUILayoutOption { }

    public static class GUILayout
    {
        public static void BeginArea(Rect r) { }
        public static void EndArea() { }
        public static void BeginHorizontal(params GUILayoutOption[] opts) { }
        public static void EndHorizontal() { }
        public static Vector2 BeginScrollView(Vector2 pos, params GUILayoutOption[] opts) => pos;
        public static void EndScrollView() { }
        public static bool Button(string text, params GUILayoutOption[] opts) => false;
        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] opts) => false;
        public static void Label(string text, params GUILayoutOption[] opts) { }
        public static void Label(string text, GUIStyle style, params GUILayoutOption[] opts) { }
        public static float HorizontalSlider(float value, float min, float max, params GUILayoutOption[] opts) => value;
        public static bool Toggle(bool value, string text, params GUILayoutOption[] opts) => value;
        public static void Space(float pixels) { }
        public static void FlexibleSpace() { }
        public static GUILayoutOption Width(float w) => new GUILayoutOption();
        public static GUILayoutOption ExpandWidth(bool expand) => new GUILayoutOption();
    }

    public static class GUILayoutUtility
    {
        public static Rect GetRect(float w, float h, params GUILayoutOption[] opts) => new Rect(0, 0, w, h);
    }

    public static class GUIUtility { public static int hotControl; }
}
