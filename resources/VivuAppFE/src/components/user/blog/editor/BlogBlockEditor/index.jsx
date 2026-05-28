import { BlogBlockEditorProvider } from "./Provider";
import { useBlogBlockEditor } from "./Context";
import BlockList from "./BlockList";

/**
 * BlogBlockEditor Compound Component
 * Usage:
 * <BlogBlockEditor.Provider initialBlocks={...}>
 *   <BlogBlockEditor.List />
 * </BlogBlockEditor.Provider>
 */
const BlogBlockEditor = {
  Provider: BlogBlockEditorProvider,
  List: BlockList,
  useEditor: useBlogBlockEditor,
};

export default BlogBlockEditor;
