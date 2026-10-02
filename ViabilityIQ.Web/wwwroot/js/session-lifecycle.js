window.viabilitySession = (() => {
    let component;
    let idleTimeout;
    let warningTimeout;
    let warningTimer;
    let logoutTimer;
    let lastActivity = 0;
    let initialized = false;

    const activityEvents = ["pointerdown", "keydown", "touchstart", "scroll"];

    function clearTimers() {
        window.clearTimeout(warningTimer);
        window.clearTimeout(logoutTimer);
    }

    function scheduleTimers() {
        clearTimers();

        warningTimer = window.setTimeout(async () => {
            const remainingMinutes = Math.max(
                1,
                Math.ceil((idleTimeout - warningTimeout) / 60000));
            try {
                await component.invokeMethodAsync(
                    "ShowIdleWarningAsync",
                    remainingMinutes);
            } catch {
                // The circuit may be reconnecting; browser-side logout still applies.
            }
        }, warningTimeout);

        logoutTimer = window.setTimeout(() => {
            window.location.replace("/api/auth/idle-signout");
        }, idleTimeout);
    }

    function recordActivity() {
        const now = Date.now();
        if (now - lastActivity < 1000) {
            return;
        }

        lastActivity = now;
        scheduleTimers();
    }

    function initialize(dotNetReference, timeoutMilliseconds, warningBeforeMilliseconds) {
        if (initialized) {
            dispose();
        }

        component = dotNetReference;
        idleTimeout = timeoutMilliseconds;
        warningTimeout = Math.max(
            1000,
            timeoutMilliseconds - warningBeforeMilliseconds);

        for (const eventName of activityEvents) {
            window.addEventListener(eventName, recordActivity, { passive: true });
        }

        initialized = true;
        recordActivity();
    }

    function dispose() {
        clearTimers();
        for (const eventName of activityEvents) {
            window.removeEventListener(eventName, recordActivity);
        }

        component = undefined;
        initialized = false;
    }

    return { initialize, dispose };
})();
