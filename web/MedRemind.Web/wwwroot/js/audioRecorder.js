// Audio Recorder using Web Audio API
// Provides voice recording functionality for MedRemind

window.audioRecorder = {
    mediaRecorder: null,
    audioChunks: [],
    stream: null,

    /**
     * Check if browser supports audio recording
     */
    isSupported: function() {
        return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia && window.MediaRecorder);
    },

    /**
     * Initialize audio recording
     * Returns: { success: boolean, error: string }
     */
    initialize: async function() {
        try {
            if (!this.isSupported()) {
                return {
                    success: false,
                    error: "Your browser doesn't support audio recording. Please use Chrome, Firefox, or Edge."
                };
            }

            // Request microphone access
            this.stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true
                }
            });

            // Determine supported MIME type
            let mimeType = 'audio/webm';
            if (MediaRecorder.isTypeSupported('audio/webm;codecs=opus')) {
                mimeType = 'audio/webm;codecs=opus';
            } else if (MediaRecorder.isTypeSupported('audio/ogg;codecs=opus')) {
                mimeType = 'audio/ogg;codecs=opus';
            } else if (MediaRecorder.isTypeSupported('audio/mp4')) {
                mimeType = 'audio/mp4';
            }

            // Create MediaRecorder
            this.mediaRecorder = new MediaRecorder(this.stream, {
                mimeType: mimeType,
                audioBitsPerSecond: 128000
            });

            // Reset chunks
            this.audioChunks = [];

            // Handle data available
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.audioChunks.push(event.data);
                }
            };

            return { success: true, error: null };
        } catch (error) {
            console.error('Error initializing audio recorder:', error);
            return {
                success: false,
                error: error.name === 'NotAllowedError'
                    ? 'Microphone access was denied. Please allow microphone access and try again.'
                    : `Failed to access microphone: ${error.message}`
            };
        }
    },

    /**
     * Start recording
     */
    startRecording: function() {
        try {
            if (!this.mediaRecorder) {
                return { success: false, error: 'Recorder not initialized' };
            }

            this.audioChunks = [];
            this.mediaRecorder.start();
            console.log('Recording started');
            return { success: true, error: null };
        } catch (error) {
            console.error('Error starting recording:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Stop recording and return audio blob
     * Returns: Promise<{ success: boolean, audioBlob: Blob, base64: string, duration: number, error: string }>
     */
    stopRecording: function() {
        return new Promise((resolve) => {
            try {
                if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
                    resolve({ success: false, error: 'No active recording' });
                    return;
                }

                const startTime = Date.now();

                this.mediaRecorder.onstop = async () => {
                    try {
                        const duration = Math.floor((Date.now() - startTime) / 1000);

                        // Create blob from chunks
                        const audioBlob = new Blob(this.audioChunks, {
                            type: this.mediaRecorder.mimeType
                        });

                        // Convert to base64
                        const reader = new FileReader();
                        reader.onloadend = () => {
                            const base64 = reader.result;
                            resolve({
                                success: true,
                                audioBlob: audioBlob,
                                base64: base64,
                                duration: duration,
                                mimeType: this.mediaRecorder.mimeType,
                                size: audioBlob.size,
                                error: null
                            });
                        };
                        reader.onerror = () => {
                            resolve({ success: false, error: 'Failed to convert audio to base64' });
                        };
                        reader.readAsDataURL(audioBlob);

                    } catch (error) {
                        console.error('Error processing recorded audio:', error);
                        resolve({ success: false, error: error.message });
                    }
                };

                this.mediaRecorder.stop();
                console.log('Recording stopped');

            } catch (error) {
                console.error('Error stopping recording:', error);
                resolve({ success: false, error: error.message });
            }
        });
    },

    /**
     * Get recording state
     * Returns: 'inactive', 'recording', 'paused', or 'error'
     */
    getState: function() {
        if (!this.mediaRecorder) return 'inactive';
        return this.mediaRecorder.state;
    },

    /**
     * Play audio from base64
     */
    playAudio: function(base64Audio) {
        try {
            const audio = new Audio(base64Audio);
            audio.play();
            return { success: true };
        } catch (error) {
            console.error('Error playing audio:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Cleanup and release resources
     */
    cleanup: function() {
        try {
            if (this.stream) {
                this.stream.getTracks().forEach(track => track.stop());
                this.stream = null;
            }
            if (this.mediaRecorder) {
                this.mediaRecorder = null;
            }
            this.audioChunks = [];
            console.log('Audio recorder cleaned up');
            return { success: true };
        } catch (error) {
            console.error('Error cleaning up audio recorder:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Get supported MIME types
     */
    getSupportedMimeTypes: function() {
        const types = [
            'audio/webm',
            'audio/webm;codecs=opus',
            'audio/ogg;codecs=opus',
            'audio/mp4',
            'audio/mpeg'
        ];

        return types.filter(type => MediaRecorder.isTypeSupported(type));
    }
};

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = audioRecorder;
}
