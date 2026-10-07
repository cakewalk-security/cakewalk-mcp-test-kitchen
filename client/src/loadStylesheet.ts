const loadedStylesheetIds = new Set<string>();

export function loadStylesheet(href: string, id: string): void {
  if (loadedStylesheetIds.has(id) || document.getElementById(id)) {
    return;
  }

  const link = document.createElement("link");
  link.id = id;
  link.rel = "stylesheet";
  link.href = href;
  link.media = "print";
  link.onload = () => {
    link.media = "all";
  };
  document.head.appendChild(link);
  loadedStylesheetIds.add(id);
}
