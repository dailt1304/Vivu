import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, act } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import NotificationBell from "../NotificationBell";
import * as NotificationContext from "../../../contexts/notification-context";
import * as NotificationHooks from "../../../hooks/notifications/useNotifications";

// Mock internal components
vi.mock("../NotificationDropdown", () => ({
  default: ({ isOpen }) => isOpen ? <div data-testid="notification-dropdown" /> : null,
  NotificationListContent: () => <div data-testid="list-content" />
}));

vi.mock("../NotificationBottomSheet", () => ({
  default: ({ isOpen, children }) => isOpen ? <div data-testid="notification-bottom-sheet">{children}</div> : null
}));

// Mock lucide-react
vi.mock("lucide-react", () => ({
  Bell: () => <div data-testid="icon-bell" />
}));

// Mock framer-motion
vi.mock("framer-motion", () => ({
  motion: {
    div: ({ children, ...props }) => <div {...props}>{children}</div>,
    button: ({ children, ...props }) => <button {...props}>{children}</button>,
  },
  AnimatePresence: ({ children }) => <>{children}</>,
}));

describe("NotificationBell Component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    // Default to desktop
    Object.defineProperty(window, "innerWidth", {
      writable: true,
      configurable: true,
      value: 1024,
    });
    window.dispatchEvent(new Event("resize"));

    // Mock Context
    vi.spyOn(NotificationContext, "useNotifications").mockReturnValue({
      unreadCount: 0,
      refetchUnreadCount: vi.fn(),
    });

    // Mock Hooks
    vi.spyOn(NotificationHooks, "useNotificationList").mockReturnValue({
      notifications: [],
      isLoading: false,
      totalCount: 0,
      mutate: vi.fn(),
    });
  });

  it("should render the bell icon", () => {
    render(
      <BrowserRouter>
        <NotificationBell />
      </BrowserRouter>
    );

    expect(screen.getByTestId("icon-bell")).toBeDefined();
  });

  it("should show unread count badge when count > 0", () => {
    vi.spyOn(NotificationContext, "useNotifications").mockReturnValue({
      unreadCount: 5,
      refetchUnreadCount: vi.fn(),
    });

    render(
      <BrowserRouter>
        <NotificationBell />
      </BrowserRouter>
    );

    expect(screen.getByText("5")).toBeDefined();
  });

  it("should show '9+' when unread count > 9", () => {
    vi.spyOn(NotificationContext, "useNotifications").mockReturnValue({
      unreadCount: 15,
      refetchUnreadCount: vi.fn(),
    });

    render(
      <BrowserRouter>
        <NotificationBell />
      </BrowserRouter>
    );

    expect(screen.getByText("9+")).toBeDefined();
  });

  it("should open dropdown when clicked on desktop", async () => {
    render(
      <BrowserRouter>
        <NotificationBell />
      </BrowserRouter>
    );

    const bellBtn = screen.getByRole("button");
    await act(async () => {
      fireEvent.click(bellBtn);
    });

    expect(screen.getByTestId("notification-dropdown")).toBeDefined();
  });

  it("should open bottom sheet when clicked on mobile", async () => {
    // Set viewport width to mobile BEFORE render to ensure useEffect picks it up
    Object.defineProperty(window, "innerWidth", {
      writable: true,
      configurable: true,
      value: 375,
    });
    window.dispatchEvent(new Event("resize"));

    render(
      <BrowserRouter>
        <NotificationBell />
      </BrowserRouter>
    );

    const bellBtn = screen.getByRole("button");
    await act(async () => {
      fireEvent.click(bellBtn);
    });

    expect(screen.getByTestId("notification-bottom-sheet")).toBeDefined();
  });
});
