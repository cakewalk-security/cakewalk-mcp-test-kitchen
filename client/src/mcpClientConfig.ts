export const MCP_ENDPOINT_PATH = "/mcp";

export const MCP_SERVER_CONFIG_NAME = "mcp-test-kitchen";

export type McpClientConfig = {
  mcpServers: Record<
    string,
    {
      url: string;
      headers: {
        Authorization: string;
      };
    }
  >;
};

export function buildMcpClientConfig(pat: string, origin: string): McpClientConfig {
  const normalizedOrigin = origin.replace(/\/$/, "");

  return {
    mcpServers: {
      [MCP_SERVER_CONFIG_NAME]: {
        url: `${normalizedOrigin}${MCP_ENDPOINT_PATH}`,
        headers: {
          Authorization: `Bearer ${pat}`,
        },
      },
    },
  };
}

export function buildMcpClientConfigJson(pat: string, origin: string): string {
  return JSON.stringify(buildMcpClientConfig(pat, origin), null, 2);
}
