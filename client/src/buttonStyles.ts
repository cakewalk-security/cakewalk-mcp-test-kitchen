const BTN_BASE =
  "inline-flex items-center justify-center select-none transition-colors duration-150 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:pointer-events-none disabled:opacity-50";

export const BTN_PRIMARY = `${BTN_BASE} rounded-md bg-insp-primary px-3.5 py-2 text-sm font-medium text-insp-ink hover:bg-insp-primary-hover active:bg-insp-primary focus-visible:outline-insp-primary`;

export const BTN_DANGER = `${BTN_BASE} rounded-md bg-insp-error px-3.5 py-2 text-sm font-medium text-insp-heading hover:brightness-110 active:brightness-100 focus-visible:outline-insp-error`;

export const BTN_SECONDARY = `${BTN_BASE} rounded-md border border-insp-border bg-insp-surface px-3.5 py-2 text-sm font-medium text-insp-body hover:bg-insp-gray-0 active:bg-insp-gray-1 focus-visible:outline-insp-primary`;

export const BTN_GHOST = `${BTN_BASE} rounded-md px-3 py-1.5 text-sm font-medium text-insp-primary hover:bg-insp-primary-soft active:bg-insp-primary-soft focus-visible:outline-insp-primary`;

export const BTN_ICON = `${BTN_BASE} size-9 shrink-0 rounded-md text-insp-muted hover:bg-insp-gray-0 hover:text-insp-heading focus-visible:outline-insp-primary`;
