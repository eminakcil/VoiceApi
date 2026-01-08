class AudioService {
  constructor() {
    if (AudioService.instance) {
      return AudioService.instance;
    }

    this.audioContext = null;
    this.analyser = null;
    this.microphoneStream = null;
    this.scriptProcessor = null;
    this.nextStartTime = 0;
    this.isPlaying = false;

    // For visualizer
    this.dataArray = null;

    AudioService.instance = this;
  }

  initAudioContext() {
    if (!this.audioContext) {
      this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
      this.analyser = this.audioContext.createAnalyser();
      this.analyser.fftSize = 256;
      const bufferLength = this.analyser.frequencyBinCount;
      this.dataArray = new Uint8Array(bufferLength);
    }
  }

  async startRecording(onAudioDataCallback) {
    this.initAudioContext();

    try {
      this.microphoneStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const source = this.audioContext.createMediaStreamSource(this.microphoneStream);

      // Connect microphone to analyser for visualization
      source.connect(this.analyser);

      // Use ScriptProcessor for raw audio handling (or AudioWorklet in newer implementations)
      // We use ScriptProcessor for simplicity here as requested for base64 streaming
      this.scriptProcessor = this.audioContext.createScriptProcessor(4096, 1, 1);
      source.connect(this.scriptProcessor);
      this.scriptProcessor.connect(this.audioContext.destination);

      this.scriptProcessor.onaudioprocess = (audioProcessingEvent) => {
        const inputBuffer = audioProcessingEvent.inputBuffer;
        const inputData = inputBuffer.getChannelData(0);
        const currentSampleRate = this.audioContext.sampleRate;
        const targetSampleRate = 16000;

        let processedData = inputData;

        if (currentSampleRate > targetSampleRate) {
          processedData = this.downsampleBuffer(inputData, currentSampleRate, targetSampleRate);
        }

        const pcmData = this.floatTo16BitPCM(processedData);
        const base64String = this.arrayBufferToBase64(pcmData);

        if (onAudioDataCallback) {
          onAudioDataCallback(base64String);
        }
      };

      await this.audioContext.resume();
    } catch (error) {
      console.error("Error accessing microphone:", error);
      throw error;
    }
  }

  stopRecording() {
    if (this.microphoneStream) {
      this.microphoneStream.getTracks().forEach(track => track.stop());
      this.microphoneStream = null;
    }
    if (this.scriptProcessor) {
      this.scriptProcessor.disconnect();
      this.scriptProcessor = null;
    }
  }

  // Playback logic for streaming audio
  // Playback logic for full audio blobs (e.g. WAV files from TTS)
  playAudioBlob(base64Data) {
    const bytes = this.base64ToArrayBuffer(base64Data);
    const blob = new Blob([bytes], { type: "audio/wav" });
    const url = URL.createObjectURL(blob);
    const audio = new Audio(url);

    audio.play().catch(err => console.error("Audio playback error:", err));

    audio.onended = () => {
      URL.revokeObjectURL(url);
    };
  }

  getVisualizerData() {
    if (this.analyser && this.dataArray) {
      this.analyser.getByteFrequencyData(this.dataArray);
      return this.dataArray;
    }
    return new Uint8Array(0);
  }

  // Helpers
  floatTo16BitPCM(input) {
    const output = new Int16Array(input.length);
    for (let i = 0; i < input.length; i++) {
      const s = Math.max(-1, Math.min(1, input[i]));
      output[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
    }
    return output.buffer;
  }

  arrayBufferToBase64(buffer) {
    let binary = '';
    const bytes = new Uint8Array(buffer);
    const len = bytes.byteLength;
    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
  }

  base64ToArrayBuffer(base64) {
    const binaryString = window.atob(base64);
    const len = binaryString.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
  }

  downsampleBuffer(buffer, sampleRate, outSampleRate) {
    if (outSampleRate === sampleRate) {
      return buffer;
    }
    if (outSampleRate > sampleRate) {
      throw "downsampling rate show be smaller than original rate";
    }
    const sampleRateRatio = sampleRate / outSampleRate;
    const newLength = Math.round(buffer.length / sampleRateRatio);
    const result = new Float32Array(newLength);
    let offsetResult = 0;
    let offsetBuffer = 0;

    while (offsetResult < result.length) {
      const nextOffsetBuffer = Math.round((offsetResult + 1) * sampleRateRatio);

      // Simple averaging (box filter) for better quality than dropping samples
      let accum = 0, count = 0;
      for (let i = offsetBuffer; i < nextOffsetBuffer && i < buffer.length; i++) {
        accum += buffer[i];
        count++;
      }
      result[offsetResult] = count > 0 ? accum / count : 0;

      offsetResult++;
      offsetBuffer = nextOffsetBuffer;
    }
    return result;
  }
}

const audioService = new AudioService();
export default audioService;
