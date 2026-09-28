using UnityEngine;

// UI 요소를 Z축 기준으로 계속 회전시킨다. (회전 조작중 화면 소용돌이)
public class UIRotator : MonoBehaviour
{
    [Tooltip("초당 회전 각도. 양수=반시계, 음수=시계 방향")]
    [SerializeField] private float degreesPerSecond = -120f;

    private void Update()
    {
        // GameManager의 timeScale 영향을 받지 않도록 unscaled 사용
        transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
    }
}
