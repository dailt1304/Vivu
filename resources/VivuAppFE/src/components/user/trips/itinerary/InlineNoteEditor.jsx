import React, { useEffect, useState } from "react";
import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import { 
  Bold, 
  Italic, 
  Strikethrough, 
  List, 
  ListOrdered,
  Heading2
} from "lucide-react";

/**
 * Minimal Rich Text Editor for inline notes inside Itinerary items.
 * Uses TipTap.
 */
const InlineNoteEditor = ({ initialContent, onBlur, isOpen }) => {
  const [internalContent, setInternalContent] = useState(initialContent || "");

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [2, 3] },
      }),
      Placeholder.configure({
        placeholder: "Nhập ghi chú chi tiết tại đây...",
      }),
    ],
    content: initialContent || "",
    editable: true,
    onUpdate: ({ editor }) => {
      setInternalContent(editor.getHTML());
    },
    onBlur: ({ editor }) => {
      const html = editor.getHTML();
      // Only trigger if actually changed to avoid spamming
      if (html !== initialContent) {
        onBlur(html);
      }
    },
    editorProps: {
      attributes: {
        class: "prose prose-sm max-w-none focus:outline-none min-h-[90px] leading-relaxed text-[13px] text-slate-600",
      },
    },
  });

  const prevContentRef = React.useRef(initialContent);
  if (prevContentRef.current !== initialContent) {
    prevContentRef.current = initialContent;
    if (editor && editor.getHTML() !== initialContent) {
      editor.commands.setContent(initialContent || "");
    }
    if (internalContent !== initialContent) {
      setInternalContent(initialContent || "");
    }
  }

  // Vercel performance fix: Stop propagation of drag events
  const stopPropagation = (e) => e.stopPropagation();

  if (!editor) {
    return null;
  }

  return (
    <div 
      className="w-full bg-slate-50 rounded-xl border border-slate-200 overflow-hidden shadow-inner focus-within:ring-2 focus-within:ring-blue-500/10 focus-within:border-blue-300 transition-all"
      onPointerDown={stopPropagation}
    >
      {/* Minimal Toolbar */}
      {isOpen && (
        <div className="flex items-center gap-1 p-1.5 border-b border-slate-200 bg-white/50 flex-wrap">
          <MenuButton
            onClick={() => editor.chain().focus().toggleBold().run()}
            isActive={editor.isActive("bold")}
            icon={<Bold size={14} />}
            title="Đậm"
          />
          <MenuButton
            onClick={() => editor.chain().focus().toggleItalic().run()}
            isActive={editor.isActive("italic")}
            icon={<Italic size={14} />}
            title="Nghiêng"
          />
          <MenuButton
            onClick={() => editor.chain().focus().toggleStrike().run()}
            isActive={editor.isActive("strike")}
            icon={<Strikethrough size={14} />}
            title="Gạch ngang"
          />
          <div className="w-px h-4 bg-slate-200 mx-1" />
          <MenuButton
            onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
            isActive={editor.isActive("heading", { level: 2 })}
            icon={<Heading2 size={14} />}
            title="Tiêu đề"
          />
          <div className="w-px h-4 bg-slate-200 mx-1" />
          <MenuButton
            onClick={() => editor.chain().focus().toggleBulletList().run()}
            isActive={editor.isActive("bulletList")}
            icon={<List size={14} />}
            title="Danh sách"
          />
          <MenuButton
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
            isActive={editor.isActive("orderedList")}
            icon={<ListOrdered size={14} />}
            title="Danh sách số"
          />
        </div>
      )}

      {/* Editor Surface */}
      <div className="p-3">
        <EditorContent editor={editor} />
      </div>
    </div>
  );
};

const MenuButton = ({ onClick, isActive, icon, title }) => (
  <button
    type="button"
    onClick={onClick}
    title={title}
    className={`p-1.5 rounded-lg transition-colors ${
      isActive
        ? "bg-blue-100 text-blue-700 font-bold"
        : "text-slate-500 hover:bg-slate-200 hover:text-slate-800"
    }`}
  >
    {icon}
  </button>
);

export default InlineNoteEditor;
