import { defineConfig } from "vite";
import type { IncomingMessage } from "node:http";
import type { Socket } from "node:net";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";

const devApiProxy = {
  target: "http://localhost:5094",
  changeOrigin: true,
  cookieDomainRewrite: "",
  configure: (proxy: { on: (event: string, listener: (...args: unknown[]) => void) => void }) => {
    proxy.on("proxyRes", (proxyRes: IncomingMessage, req: IncomingMessage) => {
      const contentType = proxyRes.headers["content-type"] ?? "";
      if (!contentType.includes("text/event-stream")) {
        return;
      }

      proxyRes.headers["cache-control"] = "no-cache";
      proxyRes.headers["x-accel-buffering"] = "no";

      const socket = req.socket as Socket | undefined;
      socket?.setTimeout(0);
      socket?.setNoDelay(true);
      socket?.setKeepAlive(true);
    });
  },
};

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5174,
    proxy: {
      "/api": devApiProxy,
      "/Account": devApiProxy,
      "/signin-google": devApiProxy,
      "/signin-github": devApiProxy,
    },
  },
  build: {
    outDir: "dist",
    emptyOutDir: true,
  },
});
