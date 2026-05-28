import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "path";
import { fileURLToPath } from "url";

// https://vite.dev/config/
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: "./src/test/setup.js",
    env: {
      VITE_API_URL: "https://localhost:7294/api",
    },
    css: false,
    reporters: ["default", "vitest-sonar-reporter"],
    outputFile: {
      "vitest-sonar-reporter": "./test-reports/sonar-report.xml",
    },
    coverage: {
      provider: "v8",
      reporter: ["text", "text-summary", "lcov", "html"],
      reportsDirectory: "./test-reports/coverage",
      reportOnFailure: true,
      include: ["src/**/*.{js,jsx}"],
      exclude: [
        "src/test/**",
        "src/**/__tests__/**",
        "src/main.jsx",
        "src/App.jsx",
      ],
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
    },
  },
  server: {
    proxy: {
      "/api": {
        target: "https://localhost:7294",
        changeOrigin: true,
        secure: false,
      },
      "/hubs": {
        target: "https://localhost:7294",
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
});
