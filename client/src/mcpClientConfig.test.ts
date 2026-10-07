import { describe, expect, it } from "vitest";
import { buildMcpClientConfig, buildMcpClientConfigJson } from "./mcpClientConfig";

describe("buildMcpClientConfig", () => {
  it("builds Cursor-compatible MCP server config", () => {
    expect(buildMcpClientConfig("mcp_test_token", "https://mcp-test-kitchen.cakewalk.security")).toEqual({
      mcpServers: {
        "mcp-test-kitchen": {
          url: "https://mcp-test-kitchen.cakewalk.security/mcp",
          headers: {
            Authorization: "Bearer mcp_test_token",
          },
        },
      },
    });
  });

  it("strips trailing slash from origin", () => {
    const config = buildMcpClientConfig("mcp_test_token", "http://localhost:5094/");
    expect(config.mcpServers["mcp-test-kitchen"].url).toBe("http://localhost:5094/mcp");
  });
});

describe("buildMcpClientConfigJson", () => {
  it("returns pretty-printed JSON", () => {
    const json = buildMcpClientConfigJson("mcp_test_token", "http://localhost:5094");
    expect(json).toContain('"mcpServers"');
    expect(json).toContain('"url": "http://localhost:5094/mcp"');
    expect(json).toContain('"Authorization": "Bearer mcp_test_token"');
  });
});
