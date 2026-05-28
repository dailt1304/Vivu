import React from "react";
import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Image from "@tiptap/extension-image";
import Link from "@tiptap/extension-link";
import Placeholder from "@tiptap/extension-placeholder";
import PropTypes from "prop-types";
import EditorToolbar from "./EditorToolbar";

/**
 * Rich text blog editor using TipTap
 */
const BlogEditor = ({
  content,
  onChange,
  placeholder = "Bắt đầu viết bài của bạn...",
}) => {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: {
          levels: [1, 2, 3],
        },
      }),
      Image.configure({
        inline: false,
        allowBase64: true,
      }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: {
          class: "text-blue-600 underline hover:text-blue-800",
        },
      }),
      Placeholder.configure({
        placeholder,
      }),
    ],
    content,
    onUpdate: ({ editor }) => {
      onChange?.(editor.getHTML());
    },
    editorProps: {
      attributes: {
        class:
          "prose prose-sm sm:prose lg:prose-lg max-w-none focus:outline-none min-h-[300px] px-4 py-3",
      },
    },
  });

  return (
    <div className="border border-gray-200 rounded-2xl overflow-hidden bg-white">
      {/* Toolbar */}
      <EditorToolbar editor={editor} />

      {/* Editor Content */}
      <div className="min-h-[300px] max-h-[500px] overflow-y-auto custom-scrollbar">
        <EditorContent editor={editor} />
      </div>

      {/* Word count */}
      <div className="px-4 py-2 border-t border-gray-100 text-xs text-gray-400 flex justify-between">
        <span>{editor?.storage.characterCount?.characters?.() || 0} ký tự</span>
        <span>Nhấn Enter để xuống dòng</span>
      </div>
    </div>
  );
};

BlogEditor.propTypes = {
  content: PropTypes.string,
  onChange: PropTypes.func,
  placeholder: PropTypes.string,
};

export default BlogEditor;
