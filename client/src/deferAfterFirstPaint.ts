export function deferAfterFirstPaint(task: () => void): () => void {
  let cancelled = false;

  const run = () => {
    if (!cancelled) {
      task();
    }
  };

  if (typeof requestIdleCallback === "function") {
    const idleId = requestIdleCallback(run, { timeout: 2_000 });
    return () => {
      cancelled = true;
      cancelIdleCallback(idleId);
    };
  }

  const timeoutId = window.setTimeout(run, 0);
  return () => {
    cancelled = true;
    window.clearTimeout(timeoutId);
  };
}
