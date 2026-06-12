using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

// プレイヤー周辺に限定した照明を作り、他のライトやアンビエントを抑えて暗くする
public class PlayerLightController : MonoBehaviour
{
    [Header("Light Settings")]
    [Tooltip("ライトの半径（ワールド単位）。プレイヤー周囲3マス相当なら値を調整してください。")]
    public float lightRadius = 3f;
    [Tooltip("プレイヤーの上方に配置する距離（スポットライトの高さ）")]
    public float lightHeight = 6f;
    [Tooltip("ライトの強さ")]
    public float lightIntensity = 2f;
    [Tooltip("ライトの色")]
    public Color lightColor = Color.white;
    [Tooltip("スポットライトの最大強度")]
    public float maxIntensity = 10f;

    private Light playerLight;
    private Color prevAmbient;
    private List<Light> otherLights = new List<Light>();
    private List<float> otherLightIntensities = new List<float>();
    private List<bool> otherLightEnabled = new List<bool>();
    private AmbientMode prevAmbientMode;
    private float prevAmbientIntensity;
    private CameraClearFlags prevClearFlags;
    private Color prevCameraBg;
    private bool cameraChanged = false;

    void Awake()
    {
        // 作成済みの他ライトを記録して無効化しておく
        Light[] all = GameObject.FindObjectsOfType<Light>();
        foreach (var l in all)
        {
            if (l.transform.IsChildOf(transform)) continue;
            otherLights.Add(l);
            otherLightIntensities.Add(l.intensity);
            otherLightEnabled.Add(l.enabled);
            l.enabled = false; // 完全に消す
        }

        // アンビエントを暗くする（保存して設定）
        prevAmbient = RenderSettings.ambientLight;
        prevAmbientMode = RenderSettings.ambientMode;
        prevAmbientIntensity = RenderSettings.ambientIntensity;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;

        // カメラの背景を黒にする
        if (Camera.main != null)
        {
            prevClearFlags = Camera.main.clearFlags;
            prevCameraBg = Camera.main.backgroundColor;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
            cameraChanged = true;
        }

        // スポットライトを作成（プレイヤーの子として上方に配置する）
        GameObject go = new GameObject("PlayerLight");
        go.transform.SetParent(transform, false);
        playerLight = go.AddComponent<Light>();
        playerLight.type = LightType.Spot;
        playerLight.intensity = maxIntensity;
        playerLight.color = lightColor;
        playerLight.shadows = LightShadows.None;
        playerLight.range = lightHeight * 2f;
    }

    void LateUpdate()
    {
        if (playerLight != null)
        {
            // 常にプレイヤーの真上にスポットライトを配置して下向きに照らす
            playerLight.transform.localPosition = Vector3.up * lightHeight;
            playerLight.transform.localRotation = Quaternion.LookRotation(Vector3.down);
            // compute spotAngle so that radius at height approximates lightRadius
            float angle = Mathf.Rad2Deg * 2f * Mathf.Atan2(lightRadius, Mathf.Max(0.0001f, lightHeight));
            playerLight.spotAngle = Mathf.Clamp(angle, 1f, 179f);
            playerLight.range = lightHeight * 2f;
            playerLight.intensity = maxIntensity;
            playerLight.color = lightColor;
        }
    }

    void OnDestroy()
    {
        Restore();
    }

    void OnDisable()
    {
        Restore();
    }

    void Restore()
    {
        // 元のライト設定を戻す
        for (int i = 0; i < otherLights.Count; i++)
        {
            var l = otherLights[i];
            if (l == null) continue;
            l.intensity = otherLightIntensities[i];
            l.enabled = otherLightEnabled[i];
        }

        // アンビエントを復元
        RenderSettings.ambientMode = prevAmbientMode;
        RenderSettings.ambientLight = prevAmbient;
        RenderSettings.ambientIntensity = prevAmbientIntensity;

        // カメラ背景を復元
        if (cameraChanged && Camera.main != null)
        {
            Camera.main.clearFlags = prevClearFlags;
            Camera.main.backgroundColor = prevCameraBg;
        }
    }
}
