/* eslint-disable react-refresh/only-export-components */
import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AuthProvider } from "../contexts/AuthContext";
import { SWRConfig } from "swr";

/**
 * Custom render bọc component trong AuthProvider + Router
 * Sử dụng MemoryRouter (không cần browser history)
 *
 * Usage:
 *   import { render, screen } from "../test/utils";
 *   render(<MyComponent />);
 */
function AllProviders({ children }) {
  return (
    <MemoryRouter>
      <SWRConfig value={{ provider: () => new Map() }}>
        <AuthProvider>{children}</AuthProvider>
      </SWRConfig>
    </MemoryRouter>
  );
}

const customRender = (ui, options) =>
  render(ui, { wrapper: AllProviders, ...options });

// Re-export everything from RTL
export * from "@testing-library/react";
// Override render with custom version
export { customRender as render };
