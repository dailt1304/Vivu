import { createContext, useContext } from "react";

export const BlogBlockEditorContext = createContext();

export const useBlogBlockEditor = () => {
  const context = useContext(BlogBlockEditorContext);
  if (!context) {
    throw new Error(
      "useBlogBlockEditor must be used within a BlogBlockEditorProvider",
    );
  }
  return context;
};
