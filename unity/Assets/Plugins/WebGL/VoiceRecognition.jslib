mergeInto(LibraryManager.library, {
    IsVoiceRecognitionSupported: function () {
        var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        return SpeechRecognition ? 1 : 0;
    },

    StartVoiceRecognition: function (gameObjectNamePtr, resultMethodPtr, interimMethodPtr, errorMethodPtr, endMethodPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var resultMethod = UTF8ToString(resultMethodPtr);
        var interimMethod = UTF8ToString(interimMethodPtr);
        var errorMethod = UTF8ToString(errorMethodPtr);
        var endMethod = UTF8ToString(endMethodPtr);

        var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognition) {
            SendMessage(gameObjectName, errorMethod, 'not-supported');
            return;
        }

        if (window.__voiceRecognition) {
            try {
                window.__voiceRecognition.stop();
            } catch (e) {}
        }

        var recognition = new SpeechRecognition();
        recognition.lang = 'ja-JP';
        recognition.continuous = false;
        recognition.interimResults = true;

        recognition.onresult = function (event) {
            var finalTranscript = '';
            var interimTranscript = '';
            for (var i = event.resultIndex; i < event.results.length; ++i) {
                if (event.results[i].isFinal) {
                    finalTranscript += event.results[i][0].transcript;
                } else {
                    interimTranscript += event.results[i][0].transcript;
                }
            }
            if (interimTranscript) {
                SendMessage(gameObjectName, interimMethod, interimTranscript);
            }
            if (finalTranscript) {
                SendMessage(gameObjectName, resultMethod, finalTranscript);
            }
        };

        recognition.onerror = function (event) {
            SendMessage(gameObjectName, errorMethod, event.error || 'unknown');
        };

        recognition.onend = function () {
            SendMessage(gameObjectName, endMethod, '');
            window.__voiceRecognition = null;
        };

        window.__voiceRecognition = recognition;
        recognition.start();
    },

    StopVoiceRecognition: function () {
        if (window.__voiceRecognition) {
            try {
                window.__voiceRecognition.stop();
            } catch (e) {}
            window.__voiceRecognition = null;
        }
    }
});
