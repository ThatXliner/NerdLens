<script lang="ts">
    import ConnectionStatus from "$lib/components/ConnectionStatus.svelte";
    import NoActiveRec from "$lib/components/NoActiveRec.svelte";
    import { invoke } from "@tauri-apps/api/core";
    import { onMount, onDestroy } from "svelte";

    let isRecording = false;
    let isConnected = false;
    let mediaStream: MediaStream;
    let canvas: HTMLCanvasElement;
    let ctx: CanvasRenderingContext2D;
    let video: HTMLVideoElement;
    let animationId: number;
    let status = "Ready to record";
    let frameCount = 0;
    let fps = 30;
    let distance = 20;
    let frameInterval = 1000 / fps;
    let lastFrameTime = 0;

    onMount(() => {
        canvas = document.getElementById("canvas")!;
        ctx = canvas.getContext("2d")!;
        video = document.getElementById("video")!;
    });

    onDestroy(() => {
        if (animationId) {
            cancelAnimationFrame(animationId);
        }
        if (mediaStream) {
            mediaStream.getTracks().forEach((track) => track.stop());
        }
    });

    async function connectToServer() {
        try {
            const result = (await invoke("connect_to_server")) as string;
            isConnected = true;
            status = result;
        } catch (error) {
            status = `Connection failed: ${error}`;
        }
    }

    async function disconnectFromServer() {
        try {
            const result = await invoke("disconnect_from_server");
            isConnected = false;
            status = result as string;
        } catch (error) {
            status = `Disconnect failed: ${error}`;
        }
    }

    async function startRecording() {
        try {
            mediaStream = await navigator.mediaDevices.getDisplayMedia({
                video: {
                    mediaSource: "screen",
                    width: 1920,
                    height: 1080,
                    frameRate: fps,
                },
                audio: false,
            });

            video.srcObject = mediaStream;
            video.play();

            video.addEventListener("loadedmetadata", () => {
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                isRecording = true;
                frameCount = 0;
                status = "Recording...";
                captureFrame();
            });
        } catch (error) {
            status = `Failed to start recording: ${error}`;
        }
    }

    function stopRecording() {
        isRecording = false;
        if (animationId) {
            cancelAnimationFrame(animationId);
        }
        if (mediaStream) {
            mediaStream.getTracks().forEach((track) => track.stop());
        }
        status = `Recording stopped. ${frameCount} frames captured.`;
    }

    function captureFrame(timestamp = 0) {
        if (!isRecording) return;

        if (timestamp - lastFrameTime >= frameInterval) {
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

            canvas.toBlob(
                async (blob) => {
                    if (blob && isConnected && isRecording) {
                        try {
                            const arrayBuffer = await blob.arrayBuffer();
                            const uint8Array = new Uint8Array(arrayBuffer);
                            const base64 = btoa(
                                String.fromCharCode.apply(null, uint8Array),
                            );
                            console.log(uint8Array, base64);

                            await invoke("send_frame", { frameData: base64 });
                            frameCount++;
                            status = `Recording... ${frameCount} frames sent`;
                        } catch (error) {
                            status = `Failed to send frame: ${error}`;
                        }
                    }
                },
                "image/jpeg",
                0.8,
            );

            lastFrameTime = timestamp;
        }

        if (isRecording) {
            animationId = requestAnimationFrame(captureFrame);
        }
    }
</script>

<main
    class="min-h-screen bg-gradient-to-br from-blue-900 via-purple-900 to-indigo-900 text-white"
>
    <div class="container mx-auto px-6 py-8">
        <div class="text-center mb-8">
            <h1
                class="text-4xl font-bold mb-2 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent"
            >
                Screen Recorder
            </h1>
            <p class="text-gray-300">
                Capture and stream your screen to localhost:12345
            </p>
        </div>

        <div class="max-w-4xl mx-auto">
            <!-- Status Bar -->
            <ConnectionStatus {isConnected} {status} />

            <!-- Control Panel -->
            <div
                class="bg-white/10 backdrop-blur-sm rounded-xl p-6 mb-6 border border-white/20"
            >
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <!-- Connection Controls -->
                    <div class="space-y-4">
                        <h3 class="text-lg font-semibold mb-3">
                            Server Connection
                        </h3>
                        <div class="flex space-x-3">
                            <button
                                on:click={connectToServer}
                                disabled={isConnected}
                                class="flex-1 px-4 py-2 bg-green-600 hover:bg-green-700 disabled:bg-gray-600 disabled:cursor-not-allowed rounded-lg font-medium transition-colors"
                            >
                                Connect
                            </button>
                            <button
                                on:click={disconnectFromServer}
                                disabled={!isConnected}
                                class="flex-1 px-4 py-2 bg-red-600 hover:bg-red-700 disabled:bg-gray-600 disabled:cursor-not-allowed rounded-lg font-medium transition-colors"
                            >
                                Disconnect
                            </button>
                        </div>
                    </div>

                    <!-- Recording Controls -->
                    <div class="space-y-4">
                        <h3 class="text-lg font-semibold mb-3">
                            Screen Recording
                        </h3>
                        <div class="flex space-x-3">
                            <button
                                on:click={startRecording}
                                disabled={isRecording || !isConnected}
                                class="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 disabled:cursor-not-allowed rounded-lg font-medium transition-colors flex items-center justify-center space-x-2"
                            >
                                <svg
                                    class="w-5 h-5"
                                    fill="currentColor"
                                    viewBox="0 0 20 20"
                                >
                                    <circle cx="10" cy="10" r="6" />
                                </svg>
                                <span>Start Recording</span>
                            </button>
                            <button
                                on:click={stopRecording}
                                disabled={!isRecording}
                                class="flex-1 px-4 py-2 bg-red-600 hover:bg-red-700 disabled:bg-gray-600 disabled:cursor-not-allowed rounded-lg font-medium transition-colors flex items-center justify-center space-x-2"
                            >
                                <svg
                                    class="w-5 h-5"
                                    fill="currentColor"
                                    viewBox="0 0 20 20"
                                >
                                    <rect x="6" y="6" width="8" height="8" />
                                </svg>
                                <span>Stop Recording</span>
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Settings -->
                <div class="mt-6 pt-6 border-t border-white/20">
                    <div class="flex items-center space-x-4">
                        <label class="text-sm font-medium">FPS:</label>
                        <input
                            type="range"
                            min="10"
                            max="60"
                            bind:value={fps}
                            on:input={() => {
                                frameInterval = 1000 / fps;
                            }}
                            disabled={isRecording}
                            class="flex-1 max-w-xs"
                        />
                        <span class="text-sm w-8">{fps}</span>
                        <label class="text-sm font-medium">Distance:</label>
                        <input
                            type="range"
                            min="15"
                            max="60"
                            bind:value={distance}
                            on:input={() => {
                                invoke("change_settings", {
                                    settings: JSON.stringify({ distance }),
                                });
                            }}
                            disabled={!isConnected}
                            class="flex-1 max-w-xs"
                        />
                        <span class="text-sm w-8">{distance}</span>
                    </div>
                </div>
            </div>

            <!-- Preview -->
            <div
                class="bg-black/30 backdrop-blur-sm rounded-xl p-6 border border-white/10"
            >
                <h3 class="text-lg font-semibold mb-4">Preview</h3>
                <div class="relative bg-black/50 rounded-lg overflow-hidden">
                    <video
                        id="video"
                        class="w-full h-auto max-h-96 object-contain"
                        muted
                        autoplay
                        style="display: {isRecording ? 'block' : 'none'}"
                    ></video>
                    <canvas id="canvas" class="hidden"></canvas>
                    {#if !isRecording}
                        <NoActiveRec />
                    {/if}
                </div>
            </div>

            <!-- Statistics -->
            {#if isRecording}
                <div
                    class="mt-6 bg-white/5 backdrop-blur-sm rounded-lg p-4 border border-white/10"
                >
                    <div class="grid grid-cols-3 gap-4 text-center">
                        <div>
                            <div class="text-2xl font-bold text-blue-400">
                                {frameCount}
                            </div>
                            <div class="text-sm text-gray-400">Frames Sent</div>
                        </div>
                        <div>
                            <div class="text-2xl font-bold text-green-400">
                                {fps}
                            </div>
                            <div class="text-sm text-gray-400">Target FPS</div>
                        </div>
                        <div>
                            <div class="text-2xl font-bold text-purple-400">
                                {canvas?.width || 0}x{canvas?.height || 0}
                            </div>
                            <div class="text-sm text-gray-400">Resolution</div>
                        </div>
                    </div>
                </div>
            {/if}
        </div>
    </div>
</main>
