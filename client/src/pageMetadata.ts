import { useEffect } from "react";

export type PageMetadata = {
  title: string;
  description: string;
  robots?: string;
  canonicalPath?: string;
};

function upsertMeta(name: string, content: string, attribute: "name" | "property" = "name"): void {
  let element = document.head.querySelector<HTMLMetaElement>(`meta[${attribute}="${name}"]`);
  if (!element) {
    element = document.createElement("meta");
    element.setAttribute(attribute, name);
    document.head.appendChild(element);
  }

  element.content = content;
}

function upsertCanonical(href: string): void {
  let element = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (!element) {
    element = document.createElement("link");
    element.rel = "canonical";
    document.head.appendChild(element);
  }

  element.href = href;
}

export function applyPageMetadata(metadata: PageMetadata): void {
  document.title = metadata.title;
  upsertMeta("description", metadata.description);
  upsertMeta("robots", metadata.robots ?? "noindex, follow");
  upsertMeta("og:title", metadata.title, "property");
  upsertMeta("og:description", metadata.description, "property");
  upsertMeta("twitter:title", metadata.title);
  upsertMeta("twitter:description", metadata.description);

  const canonicalPath = metadata.canonicalPath ?? window.location.pathname;
  upsertCanonical(new URL(canonicalPath, window.location.origin).href);
}

export function usePageMetadata(metadata: PageMetadata): void {
  useEffect(() => {
    applyPageMetadata(metadata);
  }, [metadata.title, metadata.description, metadata.robots, metadata.canonicalPath]);
}
