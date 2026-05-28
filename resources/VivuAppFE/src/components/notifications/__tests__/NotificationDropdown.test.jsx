import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import NotificationDropdown, { NotificationListContent } from "../NotificationDropdown";

// Define mock functions at module level
const mockRefetchUnreadCount = vi.fn();
const mockMutate = vi.fn();
const mockMarkAsRead = vi.fn();
const mockMarkAllAsRead = vi.fn();
const mockNavigate = vi.fn();

// Mock context correctly
vi.mock("../../../contexts/notification-context", () => ({
  useNotifications: () => ({
    unreadCount: 2,
    markAsRead: mockMarkAsRead,
    refetchUnreadCount: mockRefetchUnreadCount,
  }),
}));

// Mock hooks
vi.mock("../../../hooks/notifications/useNotifications", () => ({
  useNotificationList: vi.fn(),
  useMarkAllAsRead: () => ({
    markAllAsRead: mockMarkAllAsRead,
    isMarking: false,
  }),
}));

// Mock useNavigate
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Mock internal components
vi.mock("../NotificationItem", () => ({
  default: ({ notification, onClick }) => (
    <button onClick={() => onClick(notification)} data-testid={`notif-item-${notification.id}`}>
      {notification.title}
    </button>
  ),
}));

// Mock lucide-react
vi.mock("lucide-react", () => ({
  Check: () => <div data-testid="icon-check" />,
  Bell: () => <div data-testid="icon-bell" />,
  Loader2: () => <div data-testid="icon-loader" />,
}));

import { useNotificationList } from "../../../hooks/notifications/useNotifications";

describe("NotificationDropdown Component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    // Default hook behavior
    vi.mocked(useNotificationList).mockReturnValue({
      notifications: [
        { id: "1", title: "Notification 1", isRead: false, type: "MEMBER_JOINED", referenceId: "trip-1" },
        { id: "2", title: "Notification 2", isRead: true, type: "NEW_COMMENT", referenceId: "blog-1" },
      ],
      isLoading: false,
      totalCount: 2,
      mutate: mockMutate,
    });
  });

  it("should not render when isOpen is false", () => {
    const { container } = render(
      <BrowserRouter>
        <NotificationDropdown isOpen={false} onClose={vi.fn()} />
      </BrowserRouter>
    );
    expect(container.firstChild).toBeNull();
  });

  it("should render notifications list when isOpen is true", () => {
    render(
      <BrowserRouter>
        <NotificationDropdown isOpen={true} onClose={vi.fn()} />
      </BrowserRouter>
    );

    expect(screen.getByText("Thông báo")).toBeDefined();
    expect(screen.getByText("Notification 1")).toBeDefined();
    expect(screen.getByText("Notification 2")).toBeDefined();
  });

  it("should show empty state when no notifications", () => {
    vi.mocked(useNotificationList).mockReturnValue({
      notifications: [],
      isLoading: false,
      totalCount: 0,
      mutate: mockMutate,
    });

    render(
      <BrowserRouter>
        <NotificationDropdown isOpen={true} onClose={vi.fn()} />
      </BrowserRouter>
    );

    expect(screen.getByText("Không có thông báo nào")).toBeDefined();
  });

  it("should toggle filter to 'unread'", () => {
    render(
      <BrowserRouter>
        <NotificationListContent onClose={vi.fn()} />
      </BrowserRouter>
    );

    const unreadTab = screen.getByText("Chưa đọc");
    fireEvent.click(unreadTab);

    // Should call hook with unreadOnly: true
    expect(useNotificationList).toHaveBeenCalledWith(expect.objectContaining({ unreadOnly: true }));
  });

  it("should call markAsRead from Context and navigate when clicking on a notification", async () => {
    const mockOnClose = vi.fn();
    render(
      <BrowserRouter>
        <NotificationListContent onClose={mockOnClose} />
      </BrowserRouter>
    );

    const notifItem = screen.getByTestId("notif-item-1");
    fireEvent.click(notifItem);

    // Now it calls the context version
    expect(mockMarkAsRead).toHaveBeenCalledWith("1");
    expect(mockNavigate).toHaveBeenCalled();
    expect(mockOnClose).toHaveBeenCalled();
  });

  it("should call markAllAsRead when 'Đánh dấu tất cả đã đọc' is clicked", async () => {
    render(
      <BrowserRouter>
        <NotificationListContent onClose={vi.fn()} />
      </BrowserRouter>
    );

    const markAllBtn = screen.getByText("Đánh dấu tất cả đã đọc");
    fireEvent.click(markAllBtn);

    expect(mockMarkAllAsRead).toHaveBeenCalled();
    await waitFor(() => expect(mockRefetchUnreadCount).toHaveBeenCalled());
  });
});
