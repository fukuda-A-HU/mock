using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// WebGL browser bridge for the Web Speech API. No-op outside WebGL builds.
/// </summary>
public sealed class VoiceRecognitionBridge : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsVoiceRecognitionSupported();

    [DllImport("__Internal")]
    private static extern void StartVoiceRecognition(
        string gameObjectName,
        string resultMethod,
        string interimMethod,
        string errorMethod,
        string endMethod);

    [DllImport("__Internal")]
    private static extern void StopVoiceRecognition();
#endif

    private Action<string> onFinal;
    private Action<string> onInterim;
    private Action<string> onError;
    private Action onEnd;

    public bool IsSupported
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsVoiceRecognitionSupported() == 1;
#else
            return false;
#endif
        }
    }

    public bool IsListening { get; private set; }

    public void StartListening(
        Action<string> onFinalResult,
        Action<string> onInterimResult,
        Action<string> onErrorCallback,
        Action onEndCallback)
    {
        if (!IsSupported || IsListening)
            return;

        onFinal = onFinalResult;
        onInterim = onInterimResult;
        onError = onErrorCallback;
        onEnd = onEndCallback;
        IsListening = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        StartVoiceRecognition(
            gameObject.name,
            nameof(HandleFinalResult),
            nameof(HandleInterimResult),
            nameof(HandleError),
            nameof(HandleEnd));
#endif
    }

    public void StopListening()
    {
        if (!IsListening)
            return;

#if UNITY_WEBGL && !UNITY_EDITOR
        StopVoiceRecognition();
#endif
        IsListening = false;
    }

    public void HandleFinalResult(string transcript)
    {
        onFinal?.Invoke(transcript);
    }

    public void HandleInterimResult(string transcript)
    {
        onInterim?.Invoke(transcript);
    }

    public void HandleError(string error)
    {
        IsListening = false;
        onError?.Invoke(error);
    }

    public void HandleEnd(string _)
    {
        IsListening = false;
        onEnd?.Invoke();
    }

    private void OnDisable()
    {
        StopListening();
    }
}
