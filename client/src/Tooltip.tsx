import type { ReactElement } from "react";

type TooltipPlacement = "top" | "bottom";

type TooltipProps = {
  label: string;
  placement?: TooltipPlacement;
  children: ReactElement;
};

const PLACEMENT_CLASSES: Record<TooltipPlacement, string> = {
  top: "bottom-full mb-2",
  bottom: "top-full mt-2",
};

const TOOLTIP_SURFACE =
  "pointer-events-none absolute left-1/2 z-50 max-w-xs -translate-x-1/2 rounded-md border border-insp-border bg-insp-surface px-2.5 py-1.5 text-xs font-medium text-insp-heading opacity-0 shadow-lg transition-opacity duration-150 group-hover:opacity-100 group-focus-within:opacity-100";

export default function Tooltip({ label, placement = "top", children }: TooltipProps) {
  return (
    <span className="group relative inline-flex">
      {children}
      <span role="tooltip" className={`${TOOLTIP_SURFACE} ${PLACEMENT_CLASSES[placement]} text-center whitespace-normal`}>
        {label}
      </span>
    </span>
  );
}
