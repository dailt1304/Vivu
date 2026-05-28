import React from "react";
import {
  Bold,
  Italic,
  Strikethrough,
  Heading1,
  Heading2,
  Heading3,
  List,
  ListOrdered,
  Quote,
  Code,
  Link,
  Image,
  Undo,
  Redo,
  Minus,
} from "lucide-react";
import PropTypes from "prop-types";

/**
 * Toolbar button component - defined outside of EditorToolbar
 */
const ToolButton = ({ onClick, isActive, disabled, children, title }) => (
  <button
    type="button"
    onClick={onClick}
    disabled={disabled}
    title={title}
    className={`p-2 rounded-lg transition-all ${
      isActive
        ? "bg-blue-100 text-blue-600"
        : "text-gray-600 hover:bg-gray-100 hover:text-gray-900"
    } ${disabled ? "opacity-40 cursor-not-allowed" : ""}`}
  >
    {children}
  </button>
);

ToolButton.propTypes = {
  onClick: PropTypes.func,
  isActive: PropTypes.bool,
  disabled: PropTypes.bool,
  children: PropTypes.node,
  title: PropTypes.string,
};

/**
 * Divider component - defined outside of EditorToolbar
 */
const Divider = () => <div className="w-px h-6 bg-gray-200 mx-1" />;

/**
 * Toolbar for the blog editor
 */
const EditorToolbar = ({ editor }) => {
  if (!editor) return null;

  const addImage = () => {
    const url = window.prompt("Nhập URL hình ảnh:");
    if (url) {
      editor.chain().focus().setImage({ src: url }).run();
    }
  };

  const addLink = () => {
    const url = window.prompt("Nhập URL:");
    if (url) {
      editor.chain().focus().setLink({ href: url }).run();
    }
  };

  return (
    <div className="flex flex-wrap items-center gap-0.5 px-3 py-2 border-b border-gray-200 bg-gray-50">
      {/* Text Formatting */}
      <ToolButton
        onClick={() => editor.chain().focus().toggleBold().run()}
        isActive={editor.isActive("bold")}
        title="Bold (Ctrl+B)"
      >
        <Bold size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().toggleItalic().run()}
        isActive={editor.isActive("italic")}
        title="Italic (Ctrl+I)"
      >
        <Italic size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().toggleStrike().run()}
        isActive={editor.isActive("strike")}
        title="Strikethrough"
      >
        <Strikethrough size={18} />
      </ToolButton>

      <Divider />

      {/* Headings */}
      <ToolButton
        onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
        isActive={editor.isActive("heading", { level: 1 })}
        title="Heading 1"
      >
        <Heading1 size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
        isActive={editor.isActive("heading", { level: 2 })}
        title="Heading 2"
      >
        <Heading2 size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
        isActive={editor.isActive("heading", { level: 3 })}
        title="Heading 3"
      >
        <Heading3 size={18} />
      </ToolButton>

      <Divider />

      {/* Lists */}
      <ToolButton
        onClick={() => editor.chain().focus().toggleBulletList().run()}
        isActive={editor.isActive("bulletList")}
        title="Bullet List"
      >
        <List size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().toggleOrderedList().run()}
        isActive={editor.isActive("orderedList")}
        title="Numbered List"
      >
        <ListOrdered size={18} />
      </ToolButton>

      <Divider />

      {/* Block Elements */}
      <ToolButton
        onClick={() => editor.chain().focus().toggleBlockquote().run()}
        isActive={editor.isActive("blockquote")}
        title="Quote"
      >
        <Quote size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().toggleCodeBlock().run()}
        isActive={editor.isActive("codeBlock")}
        title="Code Block"
      >
        <Code size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().setHorizontalRule().run()}
        title="Horizontal Rule"
      >
        <Minus size={18} />
      </ToolButton>

      <Divider />

      {/* Media */}
      <ToolButton
        onClick={addLink}
        isActive={editor.isActive("link")}
        title="Add Link"
      >
        <Link size={18} />
      </ToolButton>
      <ToolButton onClick={addImage} title="Add Image">
        <Image size={18} />
      </ToolButton>

      <Divider />

      {/* Undo/Redo */}
      <ToolButton
        onClick={() => editor.chain().focus().undo().run()}
        disabled={!editor.can().undo()}
        title="Undo (Ctrl+Z)"
      >
        <Undo size={18} />
      </ToolButton>
      <ToolButton
        onClick={() => editor.chain().focus().redo().run()}
        disabled={!editor.can().redo()}
        title="Redo (Ctrl+Y)"
      >
        <Redo size={18} />
      </ToolButton>
    </div>
  );
};

EditorToolbar.propTypes = {
  editor: PropTypes.object,
};

export default EditorToolbar;
