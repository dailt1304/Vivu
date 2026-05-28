import React, { useState } from "react";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { useBlogBlockEditor } from "./Context";
import BlockWrapper from "./BlockWrapper";
import DayHeaderBlock from "./blocks/DayHeaderBlock";
import LocationBlock from "./blocks/LocationBlock";
import QuoteBlock from "./blocks/QuoteBlock";
import RichTextBlock from "./blocks/RichTextBlock";
import PhotoBlock from "./blocks/PhotoBlock";
import AddBlockMenu from "./AddBlockMenu";

// Vercel Rule: rendering-hoist-jsx
const BLOCK_COMPONENT_MAP = {
  day_header: DayHeaderBlock,
  location: LocationBlock,
  quote: QuoteBlock,
  text: RichTextBlock,
  photo: PhotoBlock,
};

/**
 * BlockList
 * Renders the list of blocks and manages drag-and-drop reordering.
 */
const BlockList = () => {
  const { blocks, updateBlock, removeBlock, reorderBlocks, addBlock } =
    useBlogBlockEditor();
  const [activeMenuId, setActiveMenuId] = useState(null);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        delay: 100,
        tolerance: 5,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  const handleDragEnd = (event) => {
    const { active, over } = event;

    if (active.id !== over?.id) {
      const oldIndex = blocks.findIndex((b) => b.id === active.id);
      const newIndex = blocks.findIndex((b) => b.id === over.id);
      reorderBlocks(oldIndex, newIndex);
    }
  };

  // Vercel Rule: rerender-memo - stabilize callbacks
  const getOnUpdate = React.useCallback(
    (id) => (updates) => updateBlock(id, updates),
    [updateBlock]
  );

  const renderBlock = (block, index) => {
    const BlockComponent = BLOCK_COMPONENT_MAP[block.blockType];
    if (!BlockComponent) return null;

    return (
      <div key={block.id}>
        <BlockWrapper
          id={block.id}
          onRemove={removeBlock}
          onAddAfter={(id) => setActiveMenuId(id)}
          isDragDisabled={block.blockType === "day_header"} // Don't allow dragging day headers for now to keep trip structure
        >
          <BlockComponent {...block} onUpdate={getOnUpdate(block.id)} />
        </BlockWrapper>

        {/* Render menu if this is the active insertion point */}
        {activeMenuId === block.id && (
          <div className="relative h-0">
            <AddBlockMenu
              onSelect={(type) => {
                addBlock(type, index + 1);
                setActiveMenuId(null);
              }}
              onClose={() => setActiveMenuId(null)}
            />
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="max-w-5xl mx-auto py-8 px-4 sm:px-6">
      {blocks.length === 0 ? (
        <div className="text-center py-20 bg-slate-50 rounded-3xl border-2 border-dashed border-slate-200">
          <p className="text-slate-400 font-medium">
            Chọn một chuyến đi để bắt đầu viết Blog...
          </p>
        </div>
      ) : (
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
        >
          <SortableContext
            items={blocks}
            strategy={verticalListSortingStrategy}
          >
            <div className="flex flex-col gap-2">
              {blocks.map((block, index) => renderBlock(block, index))}
            </div>
          </SortableContext>
        </DndContext>
      )}
    </div>
  );
};

export default BlockList;
