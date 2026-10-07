export type CatalogFacet = "tools" | "resources" | "prompts";

export const CATALOG_FACETS: { id: CatalogFacet; label: string }[] = [
  { id: "tools", label: "Tools" },
  { id: "resources", label: "Resources" },
  { id: "prompts", label: "Prompts" },
];

export type BaselineResourceItem = {
  name: string;
  uri: string;
  mimeType: string;
  title: string;
  description: string;
  usage: string;
};

export type BaselinePromptItem = {
  name: string;
  title: string;
  description: string;
  usage: string;
  arguments?: { name: string; description: string; required: boolean }[];
};

export const BASELINE_RESOURCES: BaselineResourceItem[] = [
  {
    name: "valid_resource",
    uri: "test://resources/valid",
    mimeType: "text/plain",
    title: "Valid resource",
    description: "Always returns valid text resource content for MCP client testing.",
    usage: "Call resources/read with uri test://resources/valid to exercise the happy path.",
  },
  {
    name: "invalid_resource",
    uri: "test://resources/invalid",
    mimeType: "text/plain",
    title: "Invalid resource",
    description:
      "Advertised for testing invalid resource payloads. resources/read returns HTTP 200 with a schema-invalid JSON-RPC result.",
    usage: "Call resources/read with uri test://resources/invalid to test client validation and gateway error handling.",
  },
];

export const BASELINE_PROMPTS: BaselinePromptItem[] = [
  {
    name: "valid_prompt",
    title: "Valid prompt",
    description: "Always returns a valid user text prompt message for MCP client testing.",
    usage: "Call prompts/get with name valid_prompt. Optional argument topic is included in the message text.",
    arguments: [{ name: "topic", description: "Optional topic label included in the prompt text.", required: false }],
  },
  {
    name: "invalid_prompt",
    title: "Invalid prompt",
    description:
      "Advertised for testing invalid prompt payloads. prompts/get returns HTTP 200 with a schema-invalid JSON-RPC result.",
    usage: "Call prompts/get with name invalid_prompt to test client validation and gateway error handling.",
  },
];

export function getCatalogFacetHint(facet: CatalogFacet, mcpStateless: boolean): string {
  if (facet === "tools") {
    return mcpStateless
      ? "What the server simulates for tools on the next MCP connection. This host is configured with Stateless = true (no MCP sessions)."
      : "What the server simulates for tools on the next MCP connection.";
  }

  if (facet === "resources") {
    return "Always-on resources on every session. Pick one to see how to call resources/read — no scenario configuration.";
  }

  return "Always-on prompts on every session. Pick one to see how to call prompts/get — no scenario configuration.";
}

export function getCatalogFacetTitle(facet: CatalogFacet): string {
  if (facet === "tools") {
    return "Scenarios";
  }

  if (facet === "resources") {
    return "Resources";
  }

  return "Prompts";
}

export function getCatalogSearchPlaceholder(facet: CatalogFacet): string {
  if (facet === "tools") {
    return "Search scenarios...";
  }

  if (facet === "resources") {
    return "Search resources...";
  }

  return "Search prompts...";
}

export function matchesBaselineResourceQuery(item: BaselineResourceItem, query: string): boolean {
  if (!query) {
    return true;
  }

  const haystack = `${item.title} ${item.name} ${item.uri} ${item.description}`.toLowerCase();
  return haystack.includes(query);
}

export function matchesBaselinePromptQuery(item: BaselinePromptItem, query: string): boolean {
  if (!query) {
    return true;
  }

  const haystack = `${item.title} ${item.name} ${item.description}`.toLowerCase();
  return haystack.includes(query);
}
