import "@testing-library/jest-dom";
import { beforeAll, afterEach, afterAll } from "vitest";
import { server } from "./mocks/server";

// Start MSW server before all tests
beforeAll(() => server.listen({ onUnhandledRequest: "warn" }));

// Reset handlers between tests (prevent state leaking)
afterEach(() => server.resetHandlers());

// Clean up after all tests
afterAll(() => server.close());
