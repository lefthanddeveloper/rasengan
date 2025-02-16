using Oculus.Interaction;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class RasenganActivator : MonoBehaviour
{
    [Header("[ Gesture Events ]")]
    [SerializeField] private SelectorUnityEventWrapper m_NinjaGestureEvent;
    [SerializeField] private SelectorUnityEventWrapper m_RasenganGestureEvent;

    [Header("[ Rasegan Obj ]")]
    [SerializeField] private GameObject m_RasenganPrefab;

    [Header("[ Hand Tr ]")]
    [SerializeField] private HandVisual m_HandVisual_L;

    [Header("[ Shadow ]")]
    [SerializeField] private Material m_ShadowMt;

    private bool isNinjaGestureReady = false;
    private Coroutine ninjaGestureCoolTimeCor = null;


    private Coroutine rasenganFollowCor = null;
    private GameObject curRasenganObj = null;
    void Start()
    {
        m_NinjaGestureEvent.WhenSelected.AddListener(OnNinjaGesture);
        m_RasenganGestureEvent.WhenSelected.AddListener(OnRasenganGesture);
    }

    private void OnNinjaGesture()
    {
        Debug.Log($"{nameof(OnNinjaGesture)}");

        if (curRasenganObj != null) return;

        CancelCoolTimeCor();        
        ninjaGestureCoolTimeCor = StartCoroutine(NinjaGestureCoolTimeCor(3.0f));
        CreateShadow();
    }

    private IEnumerator NinjaGestureCoolTimeCor(float duration)
    {
        isNinjaGestureReady = true;
        yield return new WaitForSeconds(duration);
        isNinjaGestureReady = false;

        ninjaGestureCoolTimeCor = null;
    }

    private void CancelCoolTimeCor()
    {
        if(ninjaGestureCoolTimeCor != null)
        {
            StopCoroutine(ninjaGestureCoolTimeCor);
            ninjaGestureCoolTimeCor = null;
        }

        isNinjaGestureReady = false;
    }


    private void OnRasenganGesture()
    {
        Debug.Log($"<color=cyan>{nameof(OnRasenganGesture)}</color>");

        if(isNinjaGestureReady)
        {
            CancelCoolTimeCor();

            Transform palmTr = m_HandVisual_L.GetTransformByHandJointId(Oculus.Interaction.Input.HandJointId.HandPalm);
            Vector3 spawnPos = palmTr.position - palmTr.up * 0.15f;

            curRasenganObj = Instantiate(m_RasenganPrefab, spawnPos, Quaternion.identity);

            rasenganFollowCor = StartCoroutine(FollowCor(curRasenganObj.transform, () => palmTr.position - palmTr.up * 0.15f, 5.0f, () =>
            {
                Destroy(curRasenganObj);
                curRasenganObj = null;
                rasenganFollowCor = null;
            }));

            CreateShadow();
        }
    }

   private IEnumerator FollowCor(Transform follower, Func<Vector3> followPos, float duration, Action done)
    {
        float timePassed = 0f;
        while(timePassed < duration)
        {
            timePassed += Time.deltaTime;
            follower.position = Vector3.Lerp(follower.position, followPos(), 0.1f);
            yield return null;
        }

        done?.Invoke();
    }

    private void CreateShadow()
    {
        SkinnedMeshRenderer skinnedMeshRend = m_HandVisual_L.GetComponentInChildren<SkinnedMeshRenderer>();

        Mesh mesh = new Mesh();
        skinnedMeshRend.BakeMesh(mesh);

        GameObject shadowObj = new GameObject("ShadowObj");
        MeshFilter meshFilter = shadowObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRend = shadowObj.AddComponent<MeshRenderer>();
        meshRend.material = m_ShadowMt;

        shadowObj.transform.SetPositionAndRotation(skinnedMeshRend.transform.position, skinnedMeshRend.transform.rotation);

        StartCoroutine(ShadowEffectCor(shadowObj, 0.5f));
    }

    private IEnumerator ShadowEffectCor(GameObject shadowObj, float duration)
    {
        float timePassed = 0f;
        float ratio = 0f;

        MeshRenderer meshRend = shadowObj.GetComponent<MeshRenderer>();
        float startAlpha = meshRend.material.color.a;
        Vector3 startScale = shadowObj.transform.localScale;

        while(ratio < 1f)
        {
            timePassed += Time.deltaTime;
            ratio = timePassed / duration;

            float lerpAlpha = Mathf.Lerp(startAlpha, 0f, ratio);
            Color curColor = meshRend.material.color;
            curColor.a = lerpAlpha;
            meshRend.material.color = curColor;

            shadowObj.transform.localScale = Vector3.Lerp(startScale, startScale * 1.25f, ratio);

            yield return null;
        }

        Destroy(shadowObj);
    }
}
