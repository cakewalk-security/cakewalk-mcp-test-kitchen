import { buildAccountLoginUrl } from "./appRoutes";
import {
  MANAGEMENT_OBSERVATIONS_STREAM_PATH,
  OBSERVATIONS_STREAM_RECONNECT_MS,
} from "./apiPaths";
import type { Observation } from "./types";

type ObservationStreamOptions = {
  onObservation: (observation: Observation) => void;
  onConnected: () => void;
  onDisconnected: () => void;
  signal?: AbortSignal;
};

export async function consumeObservationStream(options: ObservationStreamOptions): Promise<void> {
  const response = await fetch(MANAGEMENT_OBSERVATIONS_STREAM_PATH, {
    credentials: "include",
    headers: { Accept: "text/event-stream" },
    signal: options.signal,
  });

  if (response.status === 401) {
    window.location.href = buildAccountLoginUrl(window.location.pathname);
    return;
  }

  if (!response.ok || !response.body) {
    throw new Error(`Stream failed with status ${response.status}`);
  }

  options.onConnected();

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });

    let newlineIndex = buffer.indexOf("\n");
    while (newlineIndex >= 0) {
      const line = buffer.slice(0, newlineIndex).trimEnd();
      buffer = buffer.slice(newlineIndex + 1);

      if (line.startsWith("data:")) {
        const payload = line.slice(5).trimStart();
        if (payload) {
          options.onObservation(JSON.parse(payload) as Observation);
        }
      }

      newlineIndex = buffer.indexOf("\n");
    }
  }
}

export function runObservationStreamLoop(
  options: Omit<ObservationStreamOptions, "signal">,
): () => void {
  let disposed = false;
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  let abortController: AbortController | null = null;

  const connect = async () => {
    if (disposed) {
      return;
    }

    abortController?.abort();
    abortController = new AbortController();

    try {
      await consumeObservationStream({
        ...options,
        signal: abortController.signal,
      });
    } catch {
      if (disposed || abortController.signal.aborted) {
        return;
      }
    }

    if (!disposed) {
      options.onDisconnected();
      reconnectTimer = setTimeout(() => {
        void connect();
      }, OBSERVATIONS_STREAM_RECONNECT_MS);
    }
  };

  void connect();

  return () => {
    disposed = true;
    if (reconnectTimer) {
      clearTimeout(reconnectTimer);
    }
    abortController?.abort();
    options.onDisconnected();
  };
}
