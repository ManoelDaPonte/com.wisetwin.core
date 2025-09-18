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
    }
});