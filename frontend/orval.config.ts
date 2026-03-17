import { defineConfig } from "orval";

export default defineConfig({
  prevly: {
    input: {
      target: "./openapi/prevly.json",
    },
    output: {
      mode: "tags-split",
      target: "./lib/api/generated/endpoints.ts",
      schemas: "./lib/api/generated/model",
      client: "react-query",
      httpClient: "fetch",
      override: {
        mutator: {
          path: "./lib/api/http-client.ts",
          name: "customFetch",
        },
      },
    },
  },
});
