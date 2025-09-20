mergeInto(LibraryManager.library, {
    NotifyFormationCompleted: function() {
        // Appeler la fonction JavaScript dans la page parent
        if (window.parent && window.parent.NotifyFormationCompleted) {
            window.parent.NotifyFormationCompleted();
        } else if (window.NotifyFormationCompleted) {
            window.NotifyFormationCompleted();
        } else {
            console.log('[WiseTwin] NotifyFormationCompleted called but no handler found in parent window');
        }
    },

    SendTrainingAnalytics: function(jsonPtr) {
        // Convertir le pointeur C# string en JavaScript string
        var jsonData = UTF8ToString(jsonPtr);

        // Parser le JSON pour vérification
        try {
            var analytics = JSON.parse(jsonData);

            // 1. Pour react-unity-webgl - Envoyer un message Unity
            if (typeof unityInstance !== 'undefined' && unityInstance) {
                // react-unity-webgl écoute ces événements
                if (typeof ReactUnityWebGL !== 'undefined' && ReactUnityWebGL.dispatchReactUnityEvent) {
                    ReactUnityWebGL.dispatchReactUnityEvent("TrainingAnalytics", jsonData);
                    console.log('[WiseTwin] Analytics sent via react-unity-webgl event');
                }
            }

            // 2. Méthode directe - Appeler la fonction globale si elle existe
            if (window.parent && window.parent.ReceiveTrainingAnalytics) {
                window.parent.ReceiveTrainingAnalytics(analytics);
                console.log('[WiseTwin] Training analytics sent to parent window');
            } else if (window.ReceiveTrainingAnalytics) {
                window.ReceiveTrainingAnalytics(analytics);
                console.log('[WiseTwin] Training analytics sent to current window');
            } else {
                console.log('[WiseTwin] Training analytics (no handler found):');
                console.log(analytics);
            }
        } catch (error) {
            console.error('[WiseTwin] Failed to parse analytics JSON:', error);
            console.log('[WiseTwin] Raw JSON:', jsonData);
        }
    }
});