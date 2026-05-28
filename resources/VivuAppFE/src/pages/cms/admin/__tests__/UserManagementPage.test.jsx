import { render, screen } from "@testing-library/react";
import { vi, describe, it, expect, beforeEach } from "vitest";
import UserManagementPage from "../UserManagementPage";
import {
  useAllUsers,
  useUserActions,
  useUserManagementStats,
} from "@/hooks/users/useUsers";

// Mock the hooks
vi.mock("@/hooks/users/useUsers", () => ({
  useAllUsers: vi.fn(),
  useUserActions: vi.fn(),
  useUserManagementStats: vi.fn(),
}));

// Mock DataTable since it's a complex component
vi.mock("@/components/cms/common/DataTable", () => ({
  default: ({ data, loading, actions }) => (
    <div data-testid="data-table">
      {loading ? (
        <span>Loading...</span>
      ) : (
        <table>
          <tbody>
            {data.map((user) => (
              <tr key={user.id}>
                <td>{user.fullName}</td>
                <td>{actions(user)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  ),
}));

describe("UserManagementPage", () => {
  const mockUsers = [
    {
      id: 1,
      fullName: "User 1",
      email: "user1@test.com",
      role: "User",
      status: "active",
    },
    {
      id: 2,
      fullName: "User 2",
      email: "user2@test.com",
      role: "ADMIN",
      status: "active",
    },
  ];

  const mockMutate = vi.fn();
  const mockBan = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    useAllUsers.mockReturnValue({
      users: mockUsers,
      pagination: { currentPage: 1, totalPages: 1, totalCount: 2 },
      isLoading: false,
      mutate: mockMutate,
    });
    useUserManagementStats.mockReturnValue({
      usage: { totalUsers: 10, activeUsers: 8, bannedUsers: 2 },
      isLoading: false,
    });
    useUserActions.mockReturnValue({
      ban: mockBan,
      isBanning: false,
    });
  });

  it("renders the page and usage statistics", () => {
    render(<UserManagementPage />);
    expect(screen.getByText("Quản lý Người dùng")).toBeInTheDocument();
    expect(screen.getByText("10")).toBeInTheDocument(); // Total users
    expect(screen.getByText("8")).toBeInTheDocument(); // Active users
    expect(screen.getByText("2")).toBeInTheDocument(); // Banned users
  });

  it("renders users in the table", () => {
    render(<UserManagementPage />);
    expect(screen.getByText("User 1")).toBeInTheDocument();
    expect(screen.getByText("User 2")).toBeInTheDocument();
  });

  it("shows loading state in cards", () => {
    useUserManagementStats.mockReturnValue({ isLoading: true });
    render(<UserManagementPage />);
    // Skeleton should be rendered (can check by data-testid or class if needed, but here we just check for absence of numbers)
    expect(screen.queryByText("10")).not.toBeInTheDocument();
  });
});
