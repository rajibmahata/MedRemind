// Notification Service for MedRemind
// Handles browser notifications with voice playback

window.notificationService = {
    permissionStatus: 'default', // 'default', 'granted', 'denied'
    activeNotifications: {},
    audioContext: null,

    /**
     * Check if notifications are supported
     */
    isSupported: function() {
        return 'Notification' in window;
    },

    /**
     * Get current permission status
     */
    getPermissionStatus: function() {
        if (!this.isSupported()) {
            return 'unsupported';
        }
        this.permissionStatus = Notification.permission;
        return this.permissionStatus;
    },

    /**
     * Request notification permission
     * Returns: Promise<string> - 'granted', 'denied', or 'default'
     */
    requestPermission: async function() {
        if (!this.isSupported()) {
            console.error('Notifications not supported');
            return 'unsupported';
        }

        try {
            const permission = await Notification.requestPermission();
            this.permissionStatus = permission;
            console.log('Notification permission:', permission);
            return permission;
        } catch (error) {
            console.error('Error requesting notification permission:', error);
            return 'denied';
        }
    },

    /**
     * Show a notification
     * @param {string} title - Notification title
     * @param {object} options - Notification options
     * @param {string} options.body - Notification body text
     * @param {string} options.icon - Icon URL
     * @param {string} options.badge - Badge URL
     * @param {string} options.tag - Notification tag (for replacing)
     * @param {boolean} options.requireInteraction - Keep notification visible
     * @param {string} options.voiceUrl - URL to audio file to play
     * @param {object} options.data - Custom data
     * @returns {Promise<boolean>} Success status
     */
    showNotification: async function(title, options = {}) {
        if (!this.isSupported()) {
            console.error('Notifications not supported');
            return false;
        }

        if (this.permissionStatus !== 'granted') {
            console.warn('Notification permission not granted');
            return false;
        }

        try {
            const notificationOptions = {
                body: options.body || '',
                icon: options.icon || '/icons/pill-icon.png',
                badge: options.badge || '/icons/pill-badge.png',
                tag: options.tag || `reminder-${Date.now()}`,
                requireInteraction: options.requireInteraction !== false, // Default true
                vibrate: [200, 100, 200],
                data: {
                    voiceUrl: options.voiceUrl,
                    medicationId: options.medicationId,
                    reminderId: options.reminderId,
                    ...options.data
                }
            };

            const notification = new Notification(title, notificationOptions);

            // Store notification reference
            this.activeNotifications[notificationOptions.tag] = notification;

            // Play voice if provided
            if (options.voiceUrl) {
                await this.playVoice(options.voiceUrl);
            }

            // Handle notification click
            notification.onclick = (event) => {
                event.preventDefault();
                window.focus();
                notification.close();

                // Callback to Blazor if provided
                if (options.onClick) {
                    options.onClick(notification.data);
                }

                // Navigate to medications page
                if (window.location.pathname !== '/medications') {
                    window.location.href = '/medications';
                }
            };

            // Handle notification close
            notification.onclose = () => {
                delete this.activeNotifications[notificationOptions.tag];
            };

            // Handle notification error
            notification.onerror = (error) => {
                console.error('Notification error:', error);
                delete this.activeNotifications[notificationOptions.tag];
            };

            console.log('Notification shown:', title);
            return true;

        } catch (error) {
            console.error('Error showing notification:', error);
            return false;
        }
    },

    /**
     * Play voice audio
     * @param {string} audioUrl - URL to audio file
     */
    playVoice: async function(audioUrl) {
        try {
            const audio = new Audio(audioUrl);
            audio.volume = 1.0;

            // Try to play
            const playPromise = audio.play();

            if (playPromise !== undefined) {
                await playPromise;
                console.log('Voice played successfully');
            }
        } catch (error) {
            // Audio play might fail due to browser policies
            // User interaction is often required
            console.warn('Could not play voice automatically:', error.message);

            // Try again with user interaction
            document.addEventListener('click', async () => {
                try {
                    const audio = new Audio(audioUrl);
                    await audio.play();
                    console.log('Voice played after user interaction');
                } catch (retryError) {
                    console.error('Voice playback failed:', retryError);
                }
            }, { once: true });
        }
    },

    /**
     * Close a specific notification
     */
    closeNotification: function(tag) {
        const notification = this.activeNotifications[tag];
        if (notification) {
            notification.close();
            delete this.activeNotifications[tag];
        }
    },

    /**
     * Close all notifications
     */
    closeAllNotifications: function() {
        Object.keys(this.activeNotifications).forEach(tag => {
            this.closeNotification(tag);
        });
    },

    /**
     * Show a medication reminder notification
     * @param {object} reminder - Reminder data
     */
    showMedicationReminder: async function(reminder) {
        const title = `💊 Time for ${reminder.medicationName}`;
        const body = reminder.instructions ||
                     `Take ${reminder.dosage} ${reminder.unit}`;

        const options = {
            body: body,
            tag: `med-reminder-${reminder.reminderId}`,
            requireInteraction: true,
            voiceUrl: reminder.voiceUrl,
            medicationId: reminder.medicationId,
            reminderId: reminder.reminderId,
            icon: '/icons/pill-icon.png',
            badge: '/icons/pill-badge.png'
        };

        return await this.showNotification(title, options);
    },

    /**
     * Schedule a notification (using setTimeout for simple scheduling)
     * For production, consider using Service Workers
     * @param {Date} scheduledTime - When to show notification
     * @param {object} reminder - Reminder data
     * @returns {number} Timeout ID
     */
    scheduleNotification: function(scheduledTime, reminder) {
        const now = new Date();
        const delay = scheduledTime.getTime() - now.getTime();

        if (delay < 0) {
            console.warn('Cannot schedule notification in the past');
            return null;
        }

        console.log(`Scheduling notification for ${scheduledTime.toLocaleString()} (in ${Math.round(delay/1000)}s)`);

        const timeoutId = setTimeout(() => {
            this.showMedicationReminder(reminder);
        }, delay);

        return timeoutId;
    },

    /**
     * Cancel a scheduled notification
     */
    cancelScheduledNotification: function(timeoutId) {
        if (timeoutId) {
            clearTimeout(timeoutId);
        }
    },

    /**
     * Test notification with voice
     */
    testNotification: async function(medicationName, voiceUrl) {
        const title = `💊 Test Reminder: ${medicationName}`;
        const body = 'This is a test notification with voice';

        const options = {
            body: body,
            tag: 'test-notification',
            requireInteraction: false,
            voiceUrl: voiceUrl,
            icon: '/icons/pill-icon.png'
        };

        return await this.showNotification(title, options);
    },

    /**
     * Check if notification permission is granted
     */
    hasPermission: function() {
        return this.getPermissionStatus() === 'granted';
    },

    /**
     * Initialize notification service
     */
    initialize: async function() {
        if (!this.isSupported()) {
            console.warn('Notifications not supported in this browser');
            return false;
        }

        this.getPermissionStatus();
        console.log('Notification service initialized');
        return true;
    }
};

// Initialize on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.notificationService.initialize();
    });
} else {
    window.notificationService.initialize();
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = notificationService;
}
