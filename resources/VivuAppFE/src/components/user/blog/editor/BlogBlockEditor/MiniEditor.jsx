import React from "react";
import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";

/**
 * MiniEditor
 * A lightweight TipTap editor for use within blocks.
 * Focused on text formatting (Bold, Italic, Lists) without a heavy toolbar.
 */
const MiniEditor = ({
  content,
  onChange,
  placeholder = "Viết cảm nhận của bạn...",
}) => {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: false, // No headings in mini editor
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
        class: "prose prose-sm max-w-none focus:outline-none min-h-[80px]",
      },
    },
  });

  return (
    <div className="relative">
      {/* Mini Inline Toolbar when focused */}
      {editor?.isFocused && (
        <div className="absolute -top-10 left-0 flex items-center gap-1 p-1 bg-white border border-slate-200 rounded-lg shadow-sm z-20">
          <ToolbarButton
            active={editor.isActive("bold")}
            onClick={() => editor.chain().focus().toggleBold().run()}
            label="B"
          />
          <ToolbarButton
            active={editor.isActive("italic")}
            onClick={() => editor.chain().focus().toggleItalic().run()}
            label="I"
          />
          <ToolbarButton
            active={editor.isActive("bulletList")}
            onClick={() => editor.chain().focus().toggleBulletList().run()}
            label="• List"
          />
        </div>
      )}
      <EditorContent editor={editor} />
    </div>
  );
};

const ToolbarButton = ({ active, onClick, label }) => (
  <button
    onClick={onClick}
    className={`px-2 py-1 text-xs font-bold rounded hover:bg-slate-100 transition-colors ${active ? "text-blue-600 bg-blue-50" : "text-slate-500"}`}
  >
    {label}
  </button>
);

export default MiniEditor;
