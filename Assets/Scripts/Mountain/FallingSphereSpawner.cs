using System.Collections;
using UnityEngine;

public class FallingSphereSpawner : MonoBehaviour
{
    [Header("Spawn Area (local)")]
    [SerializeField] Vector3 boxCenter = new Vector3(0f, 20f, 0f);
    [SerializeField] Vector3 boxSize = new Vector3(20f, 0f, 20f);

    [Header("Spawn Timing")]
    [SerializeField] Vector2 intervalRange = new Vector2(0.6f, 1.2f);

    [Header("Sphere / Prefab")]
    [SerializeField] GameObject spherePrefab; // 互換用（単体指定）。未設定ならPrimitiveを生成
    [SerializeField] GameObject[] spawnPrefabs; // 複数指定時はこちらからランダム選択
    [SerializeField] float sphereRadius = 0.5f;
    [SerializeField] Material overrideMaterial; // 任意

    [Header("Size")]
    [SerializeField] bool useRandomRadius = true;
    [SerializeField] Vector2 radiusRange = new Vector2(0.4f, 1.2f); // 最小〜最大半径
    [SerializeField] bool applyScaleToPrefab = true; // Prefab使用時も半径ベースでスケール適用

    [Header("Physics & Cleanup")]
    [SerializeField] float mass = 1f;
    [SerializeField] float killY = -20f;
    [SerializeField] float maxLifetime = 30f;
    [SerializeField] Transform parentForSpawned; // 任意でヒエラルキー整理

    [Header("Gizmos")]
    [SerializeField] bool showGizmosAlways = false;
    [SerializeField] bool showGizmosWhenSelected = true;
    [SerializeField] Color gizmoFillColor = new Color(0.2f, 0.6f, 1f, 0.2f);
    [SerializeField] Color gizmoWireColor = new Color(0.2f, 0.6f, 1f, 0.9f);
    [SerializeField] bool drawSolid = true;
    [SerializeField] bool drawWire = true;

    Coroutine spawnLoop;

    void OnEnable()
    {
        if (spawnLoop == null)
            spawnLoop = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            spawnLoop = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOne();
            float wait = Mathf.Max(0.01f, Random.Range(intervalRange.x, intervalRange.y));
            yield return new WaitForSeconds(wait);
        }
    }

    void SpawnOne()
    {
        Vector3 worldPos = GetRandomPointInBox();
        float chosenRadius = GetChosenRadius();
        GameObject chosenPrefab = PickPrefab();
        GameObject go = CreateSphereObject(worldPos, chosenRadius, chosenPrefab);

        if (parentForSpawned != null)
            go.transform.SetParent(parentForSpawned, true);

        // 一定時間後に必ず破棄（安全ネット）
        Destroy(go, Mathf.Max(1f, maxLifetime));

        // Yが一定より下に落ちたら即破棄
        StartCoroutine(DespawnWhenBelowY(go, killY));
    }

    GameObject CreateSphereObject(Vector3 position, float chosenRadius, GameObject chosenPrefab)
    {
        GameObject go;
        if (chosenPrefab != null)
        {
            go = Instantiate(chosenPrefab, position, Quaternion.identity);
            if (applyScaleToPrefab)
            {
                go.transform.localScale = Vector3.one * (chosenRadius * 2f);
            }
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            // 見た目サイズ（直径）を半径に合わせる
            go.transform.localScale = Vector3.one * (chosenRadius * 2f);

            if (overrideMaterial != null)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = overrideMaterial;
            }
        }

        // Rigidbody を確実に付与
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.mass = Mathf.Max(0.01f, mass);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        return go;
    }

    GameObject PickPrefab()
    {
        // 複数指定があればそちらを優先
        if (spawnPrefabs != null && spawnPrefabs.Length > 0)
        {
            // null を除外
            int nonNullCount = 0;
            for (int i = 0; i < spawnPrefabs.Length; i++)
            {
                if (spawnPrefabs[i] != null) nonNullCount++;
            }
            if (nonNullCount > 0)
            {
                int pickIndex = Random.Range(0, nonNullCount);
                for (int i = 0, seen = 0; i < spawnPrefabs.Length; i++)
                {
                    if (spawnPrefabs[i] == null) continue;
                    if (seen == pickIndex) return spawnPrefabs[i];
                    seen++;
                }
            }
        }

        // 互換: 単体指定があればそれを使う
        if (spherePrefab != null) return spherePrefab;

        // 何もなければ null → Primitive の生成にフォールバック
        return null;
    }

    float GetChosenRadius()
    {
        if (!useRandomRadius)
        {
            return Mathf.Max(0.0001f, sphereRadius);
        }

        float minR = Mathf.Max(0.0001f, Mathf.Min(radiusRange.x, radiusRange.y));
        float maxR = Mathf.Max(0.0001f, Mathf.Max(radiusRange.x, radiusRange.y));
        if (Mathf.Approximately(minR, maxR)) return minR;
        return Random.Range(minR, maxR);
    }

    Vector3 GetRandomPointInBox()
    {
        Vector3 half = boxSize * 0.5f;
        float rx = Random.Range(-half.x, half.x);
        float ry = boxSize.y == 0f ? 0f : Random.Range(-half.y, half.y);
        float rz = Random.Range(-half.z, half.z);
        // ローカル→ワールド
        return transform.TransformPoint(boxCenter + new Vector3(rx, ry, rz));
    }

    IEnumerator DespawnWhenBelowY(GameObject target, float thresholdY)
    {
        // 1フレーム毎に見る必要はないので少し間隔を空ける
        WaitForSeconds wait = new WaitForSeconds(0.25f);
        while (target != null)
        {
            if (target.transform.position.y < thresholdY)
            {
                Destroy(target);
                yield break;
            }
            yield return wait;
        }
    }

    void OnDrawGizmos()
    {
        if (showGizmosAlways) DrawSpawnBoxGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (showGizmosWhenSelected) DrawSpawnBoxGizmos();
    }

    void DrawSpawnBoxGizmos()
    {
        // TransformPoint と同じ変換で描くために localToWorldMatrix を直接使用
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 size = new Vector3(
            Mathf.Max(0.0001f, boxSize.x),
            Mathf.Max(0.0001f, boxSize.y),
            Mathf.Max(0.0001f, boxSize.z)
        );

        Vector3 center = boxCenter;

        if (drawSolid)
        {
            Gizmos.color = gizmoFillColor;
            Gizmos.DrawCube(center, size);
        }

        if (drawWire)
        {
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(center, size);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}


