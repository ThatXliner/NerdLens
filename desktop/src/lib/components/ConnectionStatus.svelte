<script lang="ts">
    const { isConnected, status } = $props<{
        isConnected: boolean;
        status: string;
    }>();
</script>

<div
    class="bg-black/30 backdrop-blur-sm rounded-lg p-4 mb-6 border border-white/10"
>
    <div class="flex items-center justify-between">
        <div class="flex items-center space-x-3">
            <div
                class="w-3 h-3 rounded-full {isConnected
                    ? 'bg-green-400'
                    : 'bg-red-400'} animate-pulse"
            ></div>
            <span class="text-sm font-medium">
                {isConnected ? "Connected" : "Disconnected"}
            </span>
        </div>
        <div class="text-sm text-gray-300">
            Status: {status}
        </div>
    </div>
</div>
