import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import StatCard from "../StatCard";
import { Users } from "lucide-react";

describe("StatCard", () => {
  it("should render title and value correctly", () => {
    render(<StatCard title="Tổng người dùng" value="1,234" icon={Users} />);
    expect(screen.getByText("Tổng người dùng")).toBeInTheDocument();
    expect(screen.getByText("1,234")).toBeInTheDocument();
  });

  it("should render trend information and icon when provided", () => {
    render(
      <StatCard
        title="Revenue"
        value="$5k"
        icon={Users}
        trend="up"
        trendValue="15%"
      />,
    );
    expect(screen.getByText(/15%/i)).toBeInTheDocument();
  });
});
