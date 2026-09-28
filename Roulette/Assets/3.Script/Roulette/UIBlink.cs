using UnityEngine;

// CanvasGroup 알파를 부드럽게 오르내려 화면이 멈추지 않았음을 보여준다. (대기화면 안내 이미지)
[RequireComponent(typeof(CanvasGroup))]
public class UIBlink : MonoBehaviour
{
    [Tooltip("한 번 어두워졌다 밝아지는 데 걸리는 시간(초)")]
    [SerializeField] private float period = 1.6f;
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.25f;

    private CanvasGroup canvasGroup;
    private float startTime;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        startTime = Time.unscaledTime;
    }

    private void Update()
    {
        // cos 기반이라 활성화 직후 알파 1에서 시작한다.
        float t = (Mathf.Cos((Time.unscaledTime - startTime) * Mathf.PI * 2f / period) + 1f) * 0.5f;
        canvasGroup.alpha = Mathf.Lerp(minAlpha, 1f, t);
    }
}
