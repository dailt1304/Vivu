import React, { useState, useEffect, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { FolderHeart, Plus, Check, X, Loader2, Search, ChevronLeft, ChevronRight } from 'lucide-react';
import { 
  useUserCollections, 
  useAddLocationToCollection,
  useCreateCollection
} from '../../../hooks/collections';
import toast from '../../../utils/toast';

// Sub-component to manage per-item icon/image state
const CollectionIcon = ({ collection, isSelected }) => {
  const imageUrl = collection.coverImageUrl || (collection.thumbnailUrls && collection.thumbnailUrls[0]);
  const isValidUrl = imageUrl && typeof imageUrl === 'string' && imageUrl.startsWith('http');

  if (isValidUrl) {
    return (
      <div className="w-10 h-10 rounded-xl overflow-hidden shadow-sm border border-slate-100 shrink-0">
        <img 
          src={imageUrl} 
          alt={collection.name} 
          className="w-full h-full object-cover"
        />
      </div>
    );
  }

  return (
    <div className={`shrink-0 transition-all ${isSelected ? 'text-blue-600' : 'text-slate-400 group-hover:text-slate-600'}`}>
      <FolderHeart size={22} strokeWidth={isSelected ? 2.5 : 2} />
    </div>
  );
};

// Bento-style Skeleton for a high-end loading experience
const CollectionSkeleton = () => (
  <div className="flex items-center justify-between p-3.5 rounded-2xl border border-transparent">
    <div className="flex items-center gap-3.5 w-full">
      <div className="w-10 h-10 rounded-xl bg-slate-100 shrink-0 animate-shimmer-fast" />
      <div className="flex flex-col gap-2 w-full">
        <div className="w-2/3 h-4 bg-slate-100 rounded-md animate-shimmer-fast" />
        <div className="w-1/3 h-2.5 bg-slate-100/60 rounded-md animate-shimmer-fast" />
      </div>
    </div>
  </div>
);

const AddToCollectionModal = ({ isOpen, onClose, locationId }) => {
  const [page, setPage] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const pageSize = 5;

  const { data: collectionsData, isLoading: isFetching } = useUserCollections(page, pageSize, searchQuery);
  const { trigger: addToCollection, isMutating: isAdding } = useAddLocationToCollection();
  const { trigger: createCollection, isMutating: isCreating } = useCreateCollection();
  
  const [selectedId, setSelectedId] = useState(null);
  const [isCreatingNew, setIsCreatingNew] = useState(false);
  const [newCollectionName, setNewCollectionName] = useState('');
  const [isMediaReady, setIsMediaReady] = useState(false);

  const collections = useMemo(() => collectionsData?.items || [], [collectionsData]);
  const totalPages = collectionsData?.totalPages || 1;

  // Batch image pre-validation logic to ensure synchronized reveal
  useEffect(() => {
    if (isFetching || collections.length === 0) {
      setIsMediaReady(false);
      return;
    }

    const validateAllMedia = async () => {
      // Create an array of promises for items that need image loading
      const promises = collections.map(c => {
        const url = c.coverImageUrl || (c.thumbnailUrls && c.thumbnailUrls[0]);
        if (!url || typeof url !== 'string' || !url.startsWith('http')) {
          return Promise.resolve(true); // Instant resolve for non-image items
        }
        
        return new Promise((resolve) => {
          const img = new Image();
          img.src = url;
          img.onload = () => resolve(true);
          img.onerror = () => resolve(false);
        });
      });

      await Promise.allSettled(promises);
      setIsMediaReady(true);
    };

    validateAllMedia();
  }, [collections, isFetching]);

  // Reset state when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setSelectedId(null);
      setIsCreatingNew(false);
      setNewCollectionName('');
      setSearchQuery('');
      setPage(1);
      setIsMediaReady(false);
    }
  }, [isOpen]);

  // Reset to page 1 when searching
  useEffect(() => {
    setPage(1);
    setIsMediaReady(false);
  }, [searchQuery]);

  const handleConfirm = async () => {
    try {
      if (isCreatingNew) {
        if (!newCollectionName.trim()) {
          toast.error("Vui lòng nhập tên bộ sưu tập");
          return;
        }
        
        const formData = new FormData();
        formData.append("name", newCollectionName.trim());
        const created = await createCollection(formData);
        
        if (created && created.id) {
          await addToCollection({ 
            collectionId: created.id, 
            locationId 
          });
          toast.success("Đã thêm vào bộ sưu tập mới");
          onClose();
        }
      } else {
        if (!selectedId) {
          toast.error("Vui lòng chọn một bộ sưu tập");
          return;
        }
        
        await addToCollection({ 
          collectionId: selectedId, 
          locationId 
        });
        toast.success("Đã thêm vào bộ sưu tập");
        onClose();
      }
    } catch (error) {
       toast.error(error.response?.data?.message || "Có lỗi xảy ra");
    }
  };

  const isMutating = isAdding || isCreating;
  const showLoading = isFetching || (collections.length > 0 && !isMediaReady);

  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={!isMutating ? onClose : undefined}
            className="absolute inset-0 bg-slate-900/40 backdrop-blur-md"
          />

          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.9, y: 20 }}
            transition={{ type: "spring", stiffness: 400, damping: 28 }}
            className="relative w-full max-w-sm bg-white/95 backdrop-blur-2xl rounded-[2.5rem] shadow-[0_32px_64px_-16px_rgba(0,0,0,0.15)] border border-white/40 overflow-hidden flex flex-col"
          >
            {/* Header */}
            <div className="px-8 pt-8 pb-4 flex items-center justify-between">
              <h3 className="font-black text-2xl text-slate-900 tracking-tighter leading-none">
                Thêm vào bộ sưu tập
              </h3>
              <button
                onClick={!isMutating ? onClose : undefined}
                className="p-2 hover:bg-slate-100 text-slate-400 hover:text-slate-600 rounded-full transition-all active:scale-90"
              >
                <X size={20} strokeWidth={2.5} />
              </button>
            </div>

            {/* Search Input */}
            <div className="px-8 py-2">
              <div className="relative group">
                <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 group-focus-within:text-blue-500 transition-colors" />
                <input 
                  type="text"
                  placeholder="Tìm kiếm bộ sưu tập..."
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-100 rounded-2xl text-sm font-medium focus:outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all placeholder:text-slate-400"
                />
              </div>
            </div>

            {/* List Content */}
            <div className="px-6 py-4 flex flex-col min-h-[340px]">
              <div className="flex-1 flex flex-col pt-2 overflow-hidden">
                <AnimatePresence mode="wait">
                  {showLoading ? (
                    <motion.div 
                      key="skeleton-container"
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      exit={{ opacity: 0 }}
                      transition={{ duration: 0.2 }}
                      className="flex flex-col gap-1.5"
                    >
                      {[1, 2, 3].map(i => <CollectionSkeleton key={i} />)}
                    </motion.div>
                  ) : (
                    <motion.div 
                      key="list-container"
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      exit={{ opacity: 0 }}
                      transition={{ duration: 0.3 }}
                      className="flex flex-col"
                    >
                      <div className="flex flex-col gap-1.5 min-h-[295px]">
                        {collections.map(c => {
                          const isSelected = selectedId === c.id && !isCreatingNew;
                          return (
                            <motion.button 
                              key={c.id}
                              whileHover={{ x: 4 }}
                              whileTap={{ scale: 0.98 }}
                              onClick={() => { setSelectedId(c.id); setIsCreatingNew(false); }}
                              className={`flex items-center justify-between p-3.5 rounded-2xl cursor-pointer transition-all border group ${isSelected ? 'border-blue-500 bg-blue-50/40 shadow-sm' : 'border-transparent hover:border-slate-100 hover:bg-slate-50/80'}`}
                            >
                              <div className="flex items-center gap-3.5 text-left">
                                <CollectionIcon collection={c} isSelected={isSelected} />
                                <div>
                                  <p className={`font-bold text-sm tracking-tight leading-none ${isSelected ? 'text-blue-900' : 'text-slate-700'}`}>{c.name}</p>
                                  <p className="text-[11px] font-bold text-slate-400 uppercase tracking-widest mt-1.5">{c.locationCount} địa điểm</p>
                                </div>
                              </div>
                              {isSelected && (
                                <motion.div initial={{ scale: 0, rotate: -45 }} animate={{ scale: 1, rotate: 0 }}>
                                  <div className="w-6 h-6 rounded-full bg-gradient-primary flex items-center justify-center text-white shadow-sm shadow-blue-500/30">
                                    <Check size={14} strokeWidth={3} />
                                  </div>
                                </motion.div>
                              )}
                            </motion.button>
                          );
                        })}

                        {collections.length === 0 && (
                          <div className="flex flex-col items-center justify-center py-12 px-4 rounded-3xl bg-slate-50/50 border border-dashed border-slate-200 flex-1">
                             <FolderHeart className="mx-auto text-slate-300 mb-3" size={32} strokeWidth={1.5} />
                             <p className="text-sm font-bold text-slate-400 tracking-tight text-center">Không tìm thấy bộ sưu tập nào</p>
                          </div>
                        )}
                      </div>

                      {/* Pagination Controls - Numeric Style */}
                      {totalPages > 1 && !isCreatingNew && (
                        <div className="flex items-center justify-center gap-2 mt-4 px-2">
                           <button 
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1}
                            className="p-2 rounded-xl hover:bg-slate-50 disabled:opacity-30 transition-all active:scale-95 text-slate-500"
                           >
                              <ChevronLeft size={16} strokeWidth={2.5} />
                           </button>

                           <div className="flex items-center gap-1.5">
                              {(() => {
                                const pages = [];
                                const maxVisible = 5;
                                
                                if (totalPages <= maxVisible) {
                                  for (let i = 1; i <= totalPages; i++) pages.push(i);
                                } else {
                                  pages.push(1);
                                  if (page > 3) pages.push('...');
                                  
                                  const start = Math.max(2, page - 1);
                                  const end = Math.min(totalPages - 1, page + 1);
                                  
                                  for (let i = start; i <= end; i++) {
                                    if (!pages.includes(i)) pages.push(i);
                                  }
                                  
                                  if (page < totalPages - 2) pages.push('...');
                                  if (!pages.includes(totalPages)) pages.push(totalPages);
                                }

                                return pages.map((p, idx) => (
                                  p === '...' ? (
                                    <span key={`dots-${idx}`} className="px-1 text-slate-400 font-bold text-xs">...</span>
                                  ) : (
                                    <button
                                      key={p}
                                      onClick={() => setPage(p)}
                                      className={`w-8 h-8 rounded-xl text-xs font-black transition-all ${page === p ? 'bg-gradient-primary text-white shadow-md shadow-blue-500/30 active:scale-90' : 'bg-slate-50 text-slate-500 hover:bg-slate-100 hover:text-slate-800'}`}
                                    >
                                      {p}
                                    </button>
                                  )
                                ));
                              })()}
                           </div>

                           <button 
                            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                            disabled={page === totalPages}
                            className="p-2 rounded-xl hover:bg-slate-50 disabled:opacity-30 transition-all active:scale-95 text-slate-500"
                           >
                              <ChevronRight size={16} strokeWidth={2.5} />
                           </button>
                        </div>
                      )}
                    </motion.div>
                  )}
                </AnimatePresence>

                {/* Create New Action - Static below the Animated Content */}
                {(!showLoading && collections.length > 0) && (
                  <div className="mt-6 pt-4 border-t border-slate-100/60 sticky bottom-0 z-10 -mx-1 px-1">
                    {!isCreatingNew ? (
                      <motion.button 
                        whileHover={{ y: -2 }}
                        whileTap={{ scale: 0.98 }}
                        onClick={() => { setIsCreatingNew(true); setSelectedId(null); }}
                        className="flex items-center gap-3 p-4 w-full rounded-2xl text-left bg-blue-50/50 hover:bg-blue-50 text-blue-600 font-bold transition-all text-sm group ring-1 ring-blue-500/5 shadow-sm shadow-blue-500/5"
                      >
                        <div className="w-8 h-8 rounded-xl bg-white flex items-center justify-center text-blue-500 shadow-sm border border-blue-100 group-hover:scale-110 transition-transform">
                           <Plus size={18} strokeWidth={2.5} /> 
                        </div>
                        Tạo bộ sưu tập mới
                      </motion.button>
                    ) : (
                      <motion.div 
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="flex flex-col gap-3 p-1"
                      >
                         <input 
                            autoFocus
                            type="text"
                            placeholder="Nhập tên bộ sưu tập..."
                            value={newCollectionName}
                            onChange={e => setNewCollectionName(e.target.value)}
                            className="w-full px-5 py-4 rounded-2xl border border-slate-100 bg-slate-50 outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 text-sm font-bold tracking-tight shadow-inner"
                          />
                        <button 
                          onClick={() => setIsCreatingNew(false)}
                          className="text-xs font-black text-slate-400 hover:text-red-500 text-left px-2 transition-all uppercase tracking-widest"
                        >
                          Hủy tạo mới
                        </button>
                      </motion.div>
                    )}
                  </div>
                )}
              </div>
            </div>

            {/* Footer */}
            <div className="px-8 py-6 border-t border-slate-100/60 bg-slate-50/50 flex items-center justify-end gap-4 mt-auto">
              <button
                onClick={onClose}
                disabled={isMutating}
                className="px-6 py-2.5 rounded-full font-black text-[12px] uppercase tracking-widest text-slate-400 hover:text-slate-600 hover:bg-white hover:shadow-sm transition-all active:scale-95 disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                onClick={handleConfirm}
                disabled={isMutating}
                className="relative overflow-hidden px-8 py-3 bg-gradient-primary text-white rounded-full font-black text-[12px] uppercase tracking-widest shadow-[0_8px_30px_-5px_rgba(59,130,246,0.3)] hover:shadow-[0_12px_40px_-5px_rgba(59,130,246,0.4)] active:scale-95 transition-all disabled:opacity-50 flex items-center gap-2 group"
              >
                {isMutating && (
                  <Loader2 className="animate-spin" size={14} strokeWidth={3} />
                )}
                <span>Lưu thông tin</span>
                <div className="absolute inset-0 bg-white/20 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none" />
              </button>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default AddToCollectionModal;
