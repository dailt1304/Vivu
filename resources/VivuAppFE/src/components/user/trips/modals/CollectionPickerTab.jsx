import React, { useState, useCallback, useMemo } from "react";
import { Loader2, FolderOpen, Check, ArrowRight } from "lucide-react";
import { useCollectionsSummary } from "../../../../hooks/collections/useCollections";
import collectionApi from "../../../../api/collectionApi";
import toast from "../../../../utils/toast";

// Vercel Best Practice: rendering-hoist-jsx
const CollectionSkeleton = () => (
  <div className="space-y-3 animate-pulse p-4">
    {[1, 2, 3].map((i) => (
      <div key={i} className="h-16 bg-gray-100 rounded-xl" />
    ))}
  </div>
);

// Vercel Best Practice: rendering-hoist-jsx
const EmptyState = () => (
  <div className="text-center py-12 px-4 flex-1 flex flex-col justify-center items-center">
    <FolderOpen size={40} className="mx-auto text-gray-300 mb-3" />
    <p className="text-sm font-medium text-gray-500">Bạn chưa có bộ sưu tập nào</p>
    <p className="text-xs text-gray-400 mt-1">
      Hãy lưu địa điểm yêu thích từ trang Khám phá
    </p>
  </div>
);

// Vercel Best Practice: rerender-memo
const CollectionRow = React.memo(function CollectionRow({
  collection,
  isAdding,
  result,
  onAddAll,
}) {
  const isDone = !!result;

  return (
    <div className="flex items-center justify-between p-3 rounded-xl border border-gray-100 hover:border-gray-200 hover:bg-gray-50 transition-all duration-200">
      <div className="flex items-center gap-3 min-w-0">
        <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center shrink-0">
          <FolderOpen size={18} className="text-blue-500" />
        </div>
        <div className="min-w-0">
          <h4 className="font-semibold text-gray-800 text-sm truncate">
            {collection.name}
          </h4>
          <p className="text-xs text-gray-400">
            {collection.locationCount} địa điểm
          </p>
        </div>
      </div>

      {isDone ? (
        <span className="text-xs font-medium text-emerald-600 flex items-center gap-1 shrink-0 px-2">
          <Check size={14} /> Đã thêm {result.added}
        </span>
      ) : (
        <button
          onClick={onAddAll}
          disabled={isAdding || collection.locationCount === 0}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold rounded-lg text-blue-600 bg-blue-50 hover:bg-blue-100 disabled:opacity-40 disabled:cursor-not-allowed transition-all duration-200 active:scale-[0.97] shrink-0"
        >
          {isAdding ? (
            <Loader2 size={14} className="animate-spin" />
          ) : (
            <ArrowRight size={14} />
          )}
          {isAdding ? "Đang thêm..." : "Thêm tất cả"}
        </button>
      )}
    </div>
  );
});

const CollectionPickerTab = React.memo(function CollectionPickerTab({
  onAddLocation,
  existingLocationIds = [],
}) {
  // Vercel Best Practice: client-swr-dedup
  const { data: summaryData, isLoading } = useCollectionsSummary();
  const collections = summaryData?.items || summaryData || [];

  const [addingId, setAddingId] = useState(null);
  const [results, setResults] = useState({});

  // Vercel Best Practice: js-set-map-lookups (O(1) lookup instead of Array.includes)
  const existingSet = useMemo(
    () => new Set(existingLocationIds),
    [existingLocationIds]
  );

  // Vercel Best Practice: rerender-functional-setstate & js-combine-iterations
  const handleAddAll = useCallback(
    async (collection) => {
      if (addingId) return;
      setAddingId(collection.id);

      try {
        const response = await collectionApi.getById(collection.id);
        const locations = response.data?.locations || [];

        // Vercel Best Practice: js-early-exit
        if (locations.length === 0) {
          toast.info(`"${collection.name}" không có địa điểm nào`);
          setAddingId(null);
          return;
        }

        let added = 0,
          skipped = 0;

        // Vercel Best Practice: js-combine-iterations
        for (const loc of locations) {
          if (existingSet.has(loc.locationId)) {
            skipped++;
            continue;
          }

          // Map CollectionLocationDto → format useIdeasManagement
          await onAddLocation({
            id: loc.locationId,
            name: loc.name,
            address: loc.address || loc.cityName || "Vietnam",
            image: loc.imageUrl,
            rating: loc.ratingAverage || 0,
            latitude: loc.latitude,
            longitude: loc.longitude,
            category: loc.categoryName || "Attraction",
          });

          // Tránh duplicate cục bộ trong cùng 1 collection
          existingSet.add(loc.locationId);
          added++;
        }

        // Vercel Best Practice: rerender-functional-setstate
        setResults((prev) => ({
          ...prev,
          [collection.id]: { added, skipped, total: locations.length },
        }));

        if (added > 0) {
          toast.success(
            `Đã thêm ${added}/${locations.length} điểm từ "${collection.name}"`
          );
        } else {
          toast.info(`Tất cả điểm từ "${collection.name}" đã có trong Ý tưởng`);
        }
      } catch (error) {
        console.error("Failed to add locations from collection:", error);
        toast.error("Không thể thêm địa điểm. Vui lòng thử lại.");
      } finally {
        setAddingId(null);
      }
    },
    [addingId, existingSet, onAddLocation]
  );

  if (isLoading) return <CollectionSkeleton />;

  // Vercel Best Practice: rendering-conditional-render
  return collections.length === 0 ? (
    <EmptyState />
  ) : (
    <div className="flex-1 overflow-y-auto p-4 space-y-2">
      {collections.map((c) => (
        <CollectionRow
          key={c.id}
          collection={c}
          isAdding={addingId === c.id}
          result={results[c.id]}
          onAddAll={() => handleAddAll(c)}
        />
      ))}
    </div>
  );
});

export default CollectionPickerTab;
