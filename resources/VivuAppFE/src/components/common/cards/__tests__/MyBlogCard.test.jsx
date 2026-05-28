import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import MyBlogCard from "../MyBlogCard";

const publishedBlog = {
  id: "blog-1",
  title: "Hành trình khám phá Đà Lạt",
  shortDescription: "Chuyến đi 3 ngày ở thành phố sương mù.",
  coverImageUrl: "https://example.com/cover.jpg",
  publishedAt: "2026-01-15T10:00:00Z",
  viewCount: 123,
  likeCount: 45,
  commentCount: 12,
};

const draftBlog = {
  id: "blog-2",
  title: "Bản nháp: Về miền Tây",
  shortDescription: "Khám phá sông nước miền Tây.",
  coverImageUrl: null,
  publishedAt: null,
  viewCount: 0,
  likeCount: 0,
  commentCount: 0,
};

const renderCard = (data, props = {}) =>
  render(
    <BrowserRouter>
      <MyBlogCard data={data} {...props} />
    </BrowserRouter>,
  );

describe("MyBlogCard", () => {
  it("renders null when data is null", () => {
    const { container } = render(
      <BrowserRouter>
        <MyBlogCard data={null} />
      </BrowserRouter>,
    );
    expect(container.firstChild).toBeNull();
  });

  it("renders published blog with correct title and status", () => {
    renderCard(publishedBlog);
    expect(screen.getByText(publishedBlog.title)).toBeInTheDocument();
    expect(screen.getByText("Đã đăng")).toBeInTheDocument();
  });

  it("renders draft blog with draft badge and publish button", () => {
    renderCard(draftBlog);
    expect(screen.getByText("Bản nháp")).toBeInTheDocument();
    expect(screen.getByText("Xuất bản")).toBeInTheDocument();
  });

  it("does not show publish button for published blogs", () => {
    renderCard(publishedBlog);
    expect(screen.queryByText("Xuất bản")).not.toBeInTheDocument();
  });

  it("shows stats (views, likes, comments) for published blogs", () => {
    renderCard(publishedBlog);
    expect(screen.getByText("123")).toBeInTheDocument();
    expect(screen.getByText("45")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
  });

  it("calls onEdit when edit button is clicked", () => {
    const onEdit = vi.fn();
    renderCard(publishedBlog, { onEdit });
    fireEvent.click(screen.getByText("Sửa"));
    expect(onEdit).toHaveBeenCalledWith(publishedBlog);
  });

  it("calls onDelete when delete button is clicked", () => {
    const onDelete = vi.fn();
    renderCard(publishedBlog, { onDelete });
    fireEvent.click(screen.getByText("Xóa"));
    expect(onDelete).toHaveBeenCalledWith(publishedBlog);
  });

  it("calls onPublish when publish button is clicked on draft", () => {
    const onPublish = vi.fn();
    renderCard(draftBlog, { onPublish });
    fireEvent.click(screen.getByText("Xuất bản"));
    expect(onPublish).toHaveBeenCalledWith(draftBlog);
  });

  it("shows loading text when isPublishing is true", () => {
    renderCard(draftBlog, { isPublishing: true });
    expect(screen.getByText("Đang xuất bản...")).toBeInTheDocument();
  });
});
