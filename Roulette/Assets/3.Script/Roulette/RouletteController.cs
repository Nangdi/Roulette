using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// 룰렛 흐름: 대기 → (첫 신호) 회전 연출 → (마지막 신호 후 1초 무신호) 해당 번호 영상 재생 → 대기.
// 통신 프로토콜(RS232): 센서 번호를 ASCII 숫자 + 개행으로 보낸다. 예) "5\n" = 5번 센서 감지.
// 영상 매핑: StreamingAssets/Video/{센서번호}/ 폴더 안의 첫 번째 영상 파일(이름순).
public class RouletteController : MonoBehaviour
{
    private enum State { Idle, Spinning, Playing }

    [Header("화면")]
    [SerializeField] private GameObject idlePanel;
    [SerializeField] private GameObject spinPanel;
    [SerializeField] private GameObject videoPanel;

    [Header("영상")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private AspectRatioFitter videoAspectFitter;
    [Tooltip("StreamingAssets 기준 영상 폴더")]
    [SerializeField] private string videoFolder = "Video";

    [Header("판정")]
    [Tooltip("마지막 신호 후 이 시간(초) 동안 새 신호가 없으면 룰렛이 멈춘 것으로 판정")]
    [SerializeField] private float stopDecisionSeconds = 1f;

    [Header("테스트")]
    [Tooltip("키보드 숫자키(1~9, 0=10)로 센서 신호를 흉내낸다.")]
    [SerializeField] private bool useKeyboardTest = true;

    private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".webm", ".avi", ".m4v" };

    private State state;
    private int lastSensorId;
    private float lastSignalTime;
    private SerialPortManager subscribedManager;

    private void Awake()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.started += OnVideoStarted;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void Start()
    {
        TrySubscribe();
        ShowIdle();
    }

    private void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.OnDataReceived -= OnSerialReceived;

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.started -= OnVideoStarted;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }

    private void Update()
    {
        // SerialPortManager.Instance가 늦게 만들어지는 경우 대비
        if (subscribedManager == null) TrySubscribe();

        if (useKeyboardTest) PollKeyboard();

        if (state == State.Spinning && Time.unscaledTime - lastSignalTime >= stopDecisionSeconds)
            PlayVideo(lastSensorId);
    }

    private void TrySubscribe()
    {
        if (SerialPortManager.Instance == null) return;
        subscribedManager = SerialPortManager.Instance;
        subscribedManager.OnDataReceived += OnSerialReceived;
    }

    private void OnSerialReceived(int controllerId, string data)
    {
        if (int.TryParse(data.Trim(), out int sensorId))
            OnSensorSignal(sensorId);
        else
            Debug.LogWarning($"[Roulette] 알 수 없는 신호 무시 (Ctrl {controllerId}): {data}");
    }

    private void PollKeyboard()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                OnSensorSignal(i == 0 ? 10 : i);
        }
    }

    // 센서 신호 수신. 영상 재생 중에는 무시한다.
    public void OnSensorSignal(int sensorId)
    {
        if (state == State.Playing) return;

        lastSensorId = sensorId;
        lastSignalTime = Time.unscaledTime;

        if (state == State.Idle)
        {
            state = State.Spinning;
            SetPanels(spin: true);
        }
    }

    private void PlayVideo(int sensorId)
    {
        string path = FindVideoPath(sensorId);
        if (path == null)
        {
            Debug.LogWarning($"[Roulette] {sensorId}번 영상을 찾을 수 없음 - 대기화면으로 복귀");
            ShowIdle();
            return;
        }

        Debug.Log($"[Roulette] {sensorId}번 영상 재생: {path}");
        state = State.Playing;
        // 준비가 끝날 때까지 회전 화면을 유지해 검은 화면이 보이지 않게 한다.
        videoPlayer.url = path;
        videoPlayer.Prepare();
    }

    private string FindVideoPath(int sensorId)
    {
        string dir = Path.Combine(Application.streamingAssetsPath, videoFolder, sensorId.ToString());
        if (!Directory.Exists(dir)) return null;

        return Directory.GetFiles(dir)
            .Where(f => VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (state != State.Playing) return;

        videoImage.texture = vp.texture;
        if (videoAspectFitter != null && vp.height > 0)
            videoAspectFitter.aspectRatio = (float)vp.width / vp.height;
        vp.Play();
    }

    private void OnVideoStarted(VideoPlayer vp)
    {
        if (state == State.Playing) SetPanels(video: true);
    }

    private void OnVideoFinished(VideoPlayer vp) => ShowIdle();

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[Roulette] 영상 오류: {message}");
        ShowIdle();
    }

    private void ShowIdle()
    {
        state = State.Idle;
        if (videoPlayer.isPlaying || videoPlayer.isPrepared) videoPlayer.Stop();
        videoImage.texture = null;
        SetPanels(idle: true);
    }

    private void SetPanels(bool idle = false, bool spin = false, bool video = false)
    {
        idlePanel.SetActive(idle);
        spinPanel.SetActive(spin);
        videoPanel.SetActive(video);
    }
}
