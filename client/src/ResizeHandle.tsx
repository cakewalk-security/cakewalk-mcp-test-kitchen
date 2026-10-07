import { useEffect, useRef, type KeyboardEvent, type PointerEvent } from "react";
import {
  BODY_RESIZING_CLASS,
  clampMessagesPaneWidth,
  RESIZE_HANDLE_ARIA_LABEL,
  RESIZE_HANDLE_STEP_PX,
  RESIZE_HANDLE_WIDTH_PX,
} from "./workspaceSplit";

type ResizeHandleProps = {
  value: number;
  min: number;
  max: number;
  onChange: (next: number) => void;
  onCommit: (next: number) => void;
};

export default function ResizeHandle({ value, min, max, onChange, onCommit }: ResizeHandleProps) {
  const drag = useRef<{ startX: number; startWidth: number } | null>(null);

  useEffect(() => () => document.body.classList.remove(BODY_RESIZING_CLASS), []);

  const widthFromClientX = (clientX: number, origin: { startX: number; startWidth: number }) =>
    clampMessagesPaneWidth(origin.startWidth + (origin.startX - clientX), min, max);

  const handlePointerDown = (event: PointerEvent<HTMLDivElement>) => {
    drag.current = { startX: event.clientX, startWidth: value };
    event.currentTarget.setPointerCapture?.(event.pointerId);
    document.body.classList.add(BODY_RESIZING_CLASS);
  };

  const handlePointerMove = (event: PointerEvent<HTMLDivElement>) => {
    if (!drag.current) {
      return;
    }

    onChange(widthFromClientX(event.clientX, drag.current));
  };

  const endDrag = (event: PointerEvent<HTMLDivElement>) => {
    if (!drag.current) {
      return;
    }

    const next = widthFromClientX(event.clientX, drag.current);
    drag.current = null;
    event.currentTarget.releasePointerCapture?.(event.pointerId);
    document.body.classList.remove(BODY_RESIZING_CLASS);
    onCommit(next);
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const delta =
      event.key === "ArrowLeft" ? RESIZE_HANDLE_STEP_PX : event.key === "ArrowRight" ? -RESIZE_HANDLE_STEP_PX : 0;
    if (delta === 0) {
      return;
    }

    event.preventDefault();
    const next = clampMessagesPaneWidth(value + delta, min, max);
    onChange(next);
    onCommit(next);
  };

  return (
    <div
      role="separator"
      aria-orientation="vertical"
      aria-label={RESIZE_HANDLE_ARIA_LABEL}
      aria-valuenow={Math.round(value)}
      aria-valuemin={min}
      aria-valuemax={max}
      tabIndex={0}
      className="resize-handle"
      style={{ width: RESIZE_HANDLE_WIDTH_PX }}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={endDrag}
      onPointerCancel={endDrag}
      onKeyDown={handleKeyDown}
    />
  );
}
