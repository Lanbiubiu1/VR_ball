using System.Collections;
using UnityEngine;

public class GhostDissolve : MonoBehaviour
{
    [Header("Dissolve Setup")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material dissolveTemplate;
    [SerializeField] private string dissolveProperty = "_Dissolve";
    [SerializeField] private float dissolveDuration = 1.0f;
    [SerializeField] private bool disableCollidersOnDissolveStart = true;

    private Material _originalMat;
    private Material _dissolveMatInstance;
    private bool _isDissolving;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogWarning("[GhostDissolve] No Renderer found.", this);
            return;
        }

        // Store the original material
        _originalMat = targetRenderer.sharedMaterial;
    }

    public void PlayDissolveAndDisable()
    {
        if (_isDissolving || targetRenderer == null || dissolveTemplate == null)
        {
            if (dissolveTemplate == null)
                Debug.LogWarning("[GhostDissolve] Missing dissolveTemplate material.", this);
            return;
        }

        StartCoroutine(DissolveRoutine());

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddHit();
        }
    }

    private IEnumerator DissolveRoutine()
    {
        _isDissolving = true;

        //stop collisions
        if (disableCollidersOnDissolveStart)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        // Create dissolve material instance
        _dissolveMatInstance = new Material(dissolveTemplate);

        if (_originalMat != null)
        {
            if (_originalMat.HasProperty("_BaseMap") && _dissolveMatInstance.HasProperty("_BaseMap"))
                _dissolveMatInstance.SetTexture("_BaseMap", _originalMat.GetTexture("_BaseMap"));

            if (_originalMat.HasProperty("_BaseColor") && _dissolveMatInstance.HasProperty("_BaseColor"))
                _dissolveMatInstance.SetColor("_BaseColor", _originalMat.GetColor("_BaseColor"));
        }

        targetRenderer.material = _dissolveMatInstance;

        if (_dissolveMatInstance.HasProperty(dissolveProperty))
            _dissolveMatInstance.SetFloat(dissolveProperty, 0f);
        else
            Debug.LogWarning("[GhostDissolve] Dissolve property name is wrong: " + dissolveProperty, this);

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float amount = Mathf.Clamp01(t / dissolveDuration);

            if (_dissolveMatInstance.HasProperty(dissolveProperty))
                _dissolveMatInstance.SetFloat(dissolveProperty, amount);

            yield return null;
        }

        gameObject.SetActive(false);
    }
}