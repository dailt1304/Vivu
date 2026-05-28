import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';

import { 
    ArrowLeft, 
    MapPin, 
    Mountain, 
    Utensils, 
    Edit3, 
    Trash2, 
    Star, 
    Loader2, 
    Sparkles, 
    TreePine, 
    Coffee, 
    Building2, 
    Landmark, 
    Ticket, 
    ShoppingBag, 
    Umbrella,
    UtensilsCrossed,
    Search,
    SlidersHorizontal
} from 'lucide-react';
import AppNavbar from '../../../components/layout/AppNavbar';
import MapContainer from '../../../components/common/map/MapContainer';
import DestinationDrawer from '../../../components/common/drawers/DestinationDrawer';
import ConfirmationModal from '../../../components/common/modals/ConfirmationModal';
import { useCollectionDetail, useRemoveLocationFromCollection } from '../../../hooks/collections';
import { useCategories } from '../../../hooks/locations/useCategory';
import toast from '../../../utils/toast';

const CATEGORY_ICON_MAP = {
    'All': Sparkles,
    'Tất cả': Sparkles,
    'Sightseeing': Landmark,
    'Nature': TreePine,
    'Food': UtensilsCrossed,
    'Tham quan': Landmark,
    'Thiên nhiên': TreePine,
    'Ẩm thực': UtensilsCrossed,
    'Cà phê': Coffee,
    'Coffee': Coffee,
    'Dịch vụ du lịch': MapPin,
    'Vui chơi giải trí': Ticket,
    'Lưu trú': Building2,
    'Sự kiện': Ticket,
    'Cà phê & Đồ uống': Coffee,
    'Lưu trú - Phổ thông': Building2,
    'Giải trí Về đêm': Ticket,
    'Mua sắm': ShoppingBag,
    'Biển': Umbrella,
    'Nature & Park': TreePine,
};

const CollectionDetailPage = () => {
    const { id } = useParams(); 
    const navigate = useNavigate();
    
    // Fetch Data
    const { data: collection, isLoading, mutate } = useCollectionDetail(id);
    const { categories } = useCategories();
    const { trigger: removeLocation, isMutating: isRemoving } = useRemoveLocationFromCollection();

    const [hoveredItemId, setHoveredItemId] = React.useState(null);
    const [selectedItem, setSelectedItem] = React.useState(null);
    const [activeFilter, setActiveFilter] = React.useState('all');
    const [searchQuery, setSearchQuery] = React.useState('');
    const [isCategoryMenuOpen, setIsCategoryMenuOpen] = React.useState(false);
    const [deleteModal, setDeleteModal] = React.useState({ isOpen: false, locationId: null });
    const [itemNotes, setItemNotes] = React.useState({});
    const [editingNoteId, setEditingNoteId] = React.useState(null);
    
    // Derive dynamic categories
    const dynamicCategories = React.useMemo(() => {
        const base = [{ id: 'all', label: 'Tất cả', icon: Sparkles }];
        const fromApi = (categories || []).map(cat => ({
            id: cat.id,
            label: cat.name,
            icon: CATEGORY_ICON_MAP[cat.name] || CATEGORY_ICON_MAP[cat.name.split('/')[0].trim()] || MapPin,
            categoryIcon: cat.iconUrl
        }));
        return [...base, ...fromApi];
    }, [categories]);


    // Filter locations based on categoryId and Search Query
    const filteredItems = React.useMemo(() => {
        const locations = collection?.locations || [];
        return locations
            .map(loc => {
                const catInfo = dynamicCategories.find(c =>
                    (loc.categoryId && String(c.id) === String(loc.categoryId)) ||
                    (loc.categoryName && c.label === loc.categoryName)
                );
                return { ...loc, categoryIcon: catInfo?.categoryIcon, categoryId: catInfo?.id || loc.categoryId };
            })
            .filter(loc => {
                const selectedCategory = dynamicCategories.find(c => c.id === activeFilter);
                const matchesCategory = activeFilter === 'all' || 
                    String(loc.categoryId) === String(activeFilter) ||
                    (loc.categoryName && selectedCategory && loc.categoryName === selectedCategory.label);
                    
                const matchesSearch = !searchQuery || 
                    loc.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
                    (loc.note && loc.note.toLowerCase().includes(searchQuery.toLowerCase())) ||
                    (loc.cityName && loc.cityName.toLowerCase().includes(searchQuery.toLowerCase()));
                return matchesCategory && matchesSearch;
            });
    }, [collection?.locations, activeFilter, searchQuery, dynamicCategories]);


    if (isLoading) {
        return (
            <div className="h-screen bg-white flex flex-col items-center justify-center">
                <Loader2 className="animate-spin text-blue-500" size={40} />
            </div>
        );
    }

    if (!collection) {
        return (
            <div className="h-screen bg-white flex flex-col items-center justify-center gap-4">
                <p>Không tìm thấy bộ sưu tập</p>
                <button onClick={() => navigate('/saved')} className="text-blue-600 font-medium">Quay lại</button>
            </div>
        );
    }

    const handleSaveNote = (itemId, noteContent) => {
        setItemNotes(prev => ({ ...prev, [itemId]: noteContent }));
    };

    const handleDeleteClick = (e, locationId) => {
        e.stopPropagation();
        setDeleteModal({ isOpen: true, locationId });
    };

    const confirmDelete = async () => {
        if (deleteModal.locationId) {
            try {
                await removeLocation({ collectionId: id, locationId: deleteModal.locationId });
                toast.success("Đã xóa địa điểm khỏi bộ sưu tập");
                if (selectedItem?.locationId === deleteModal.locationId) setSelectedItem(null);
                setDeleteModal({ isOpen: false, locationId: null });
                mutate();
            } catch (error) {
                toast.error(error.response?.data?.message || "Không thể xóa địa điểm");
            }
        }
    };

    return (
        <div className="h-screen bg-white flex flex-col font-sans text-slate-800 overflow-hidden">
             <AppNavbar />
             
             <ConfirmationModal
                isOpen={deleteModal.isOpen}
                onClose={() => setDeleteModal({ isOpen: false, locationId: null })}
                onConfirm={confirmDelete}
                confirmLoading={isRemoving}
                title="Xóa địa điểm"
                message="Bạn có chắc chắn muốn xóa địa điểm này khỏi bộ sưu tập?"
                confirmText="Xóa bỏ"
                cancelText="Hủy"
             />
             
             <div className="flex-1 flex overflow-hidden">
                {/* Left Panel: List */}
                <div className="w-full lg:w-1/2 flex flex-col border-r border-slate-200 bg-white z-10 shadow-xl overflow-hidden relative">
                    <div className="px-8 pt-8 pb-4 bg-white shrink-0 shadow-xs z-10 border-b border-slate-100">
                         <button 
                            onClick={() => navigate('/saved')}
                            className="flex items-center gap-2 text-slate-500 hover:text-slate-900 transition-colors mb-4 text-sm font-bold group"
                        >
                            <ArrowLeft size={18} className="group-hover:-translate-x-1 transition-transform" />
                            Quay lại đã lưu
                        </button>
                        <h1 className="text-3xl font-extrabold tracking-tight text-slate-900 mb-2">
                            {collection.name}
                        </h1>
                        {collection.description && (
                            <p className="text-slate-500 text-sm mb-2">{collection.description}</p>
                        )}
                         <p className="text-sm text-slate-500 font-medium">Bộ sưu tập • {collection.locations?.length || 0} địa điểm</p>

                        {/* Search and Filters */}
                        <div className="flex gap-2 relative mt-4">
                            <div className="relative flex-1 group">
                                <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-blue-500 transition-colors" size={18} />
                                <input 
                                    type="text" 
                                    placeholder="Tìm tên địa điểm, ghi chú..." 
                                    value={searchQuery}
                                    onChange={(e) => setSearchQuery(e.target.value)}
                                    className="w-full pl-10 pr-4 py-2.5 rounded-2xl bg-slate-50 border border-slate-200 focus:bg-white focus:border-blue-300 focus:ring-4 focus:ring-blue-500/5 outline-none transition-all text-sm font-medium"
                                />
                            </div>

                            <div className="relative">
                                <button
                                    onClick={() => setIsCategoryMenuOpen(!isCategoryMenuOpen)}
                                    className={`h-full aspect-square flex items-center justify-center rounded-2xl border transition-all shadow-sm ${
                                        isCategoryMenuOpen 
                                        ? 'bg-blue-50 border-blue-300 text-blue-600' 
                                        : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'
                                    }`}
                                    title="Bộ lọc danh mục"
                                >
                                    <SlidersHorizontal size={18} />
                                </button>

                                <AnimatePresence>
                                    {isCategoryMenuOpen && (
                                        <>
                                            <div className="fixed inset-0 z-40" onClick={() => setIsCategoryMenuOpen(false)} />
                                            <motion.div
                                                initial={{ opacity: 0, scale: 0.95, y: 10 }}
                                                animate={{ opacity: 1, scale: 1, y: 0 }}
                                                exit={{ opacity: 0, scale: 0.95, y: 10 }}
                                                className="absolute right-0 top-full mt-2 min-w-[260px] max-h-80 overflow-y-auto hide-scrollbar bg-white/95 backdrop-blur-xl rounded-2xl shadow-2xl border border-slate-100 p-2 z-50 flex flex-col gap-1"
                                            >
                                                {dynamicCategories.map(cat => (
                                                    <button
                                                        key={cat.id}
                                                        onClick={() => {
                                                            setActiveFilter(cat.id);
                                                            setIsCategoryMenuOpen(false);
                                                        }}
                                                        className={`flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all whitespace-nowrap ${
                                                            activeFilter === cat.id 
                                                            ? 'bg-blue-50 text-blue-600' 
                                                            : 'text-slate-600 hover:bg-slate-50'
                                                        }`}
                                                    >
                                                        <span className="shrink-0 p-1.5 rounded-lg bg-white shadow-xs border border-slate-100 flex items-center justify-center w-8 h-8 text-sm">
                                                            {cat.categoryIcon ? (
                                                                <span>{cat.categoryIcon}</span>
                                                            ) : (
                                                                cat.icon && <cat.icon size={14} />
                                                            )}
                                                        </span>
                                                        {cat.label}
                                                    </button>
                                                ))}
                                            </motion.div>
                                        </>
                                    )}
                                </AnimatePresence>
                            </div>
                        </div>
                    </div>

                    {/* Feed List */}
                    <div className="flex-1 overflow-y-auto p-5 scrollbar-hide">
                        {filteredItems.length === 0 ? (
                            <div className="text-center py-10 text-slate-400">
                                Không có địa điểm nào trong danh mục này.
                            </div>
                        ) : (
                            <div className="grid grid-cols-2 md:grid-cols-3 gap-x-4 gap-y-8">
                                {filteredItems.map((item) => (
                                    <motion.div 
                                        key={item.locationId}
                                        layoutId={`card-${item.locationId}`}
                                        onMouseEnter={() => setHoveredItemId(item.locationId)}
                                        onMouseLeave={() => setHoveredItemId(null)}
                                        onClick={() => setSelectedItem(item)}
                                        className="group cursor-pointer flex flex-col h-full"
                                    >
                                        <div className="aspect-4/3 w-full bg-slate-100 relative rounded-2xl overflow-hidden shadow-sm group-hover:shadow-md transition-all">
                                            <img 
                                                src={item.imageUrl || "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6"} 
                                                alt={item.name} 
                                                className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105" 
                                            />
                                            <button 
                                                onClick={(e) => handleDeleteClick(e, item.locationId)}
                                                className="absolute top-2 right-2 p-1.5 bg-white/40 hover:bg-red-500 hover:text-white backdrop-blur-md rounded-full text-white opacity-0 group-hover:opacity-100 transition-all z-20"
                                            >
                                                <Trash2 size={14} />
                                            </button>
                                        </div>

                                        <div className="mt-3 px-1 flex-1 flex flex-col">
                                            <h3 className="font-bold text-slate-800 text-sm leading-tight line-clamp-2 group-hover:text-blue-600 transition-colors">
                                                {item.name}
                                            </h3>

                                            <div className="mt-auto pt-1">
                                                <div className="flex items-center gap-1 text-slate-500 text-[10px] font-medium mb-1">
                                                    <MapPin size={10} />
                                                    <span className="truncate">{item.cityName || "Việt Nam"}</span>
                                                </div>

                                                <div className="flex items-center justify-between text-[11px] font-medium mb-2">
                                                    <span className="bg-slate-100 px-2 py-0.5 rounded-sm text-slate-600 truncate">{item.categoryName || "Điểm đến"}</span>
                                                    {item.ratingAverage > 0 && (
                                                        <div className="flex items-center gap-0.5 text-slate-700">
                                                            <Star size={10} className="fill-yellow-400 text-yellow-400" />
                                                            <span>{item.ratingAverage}</span>
                                                        </div>
                                                    )}
                                                </div>

                                                <AnimatePresence mode="wait">
                                                    {editingNoteId === item.locationId ? (
                                                        <div onClick={e => e.stopPropagation()}>
                                                            <div className="bg-slate-100 rounded-lg p-2 ring-1 ring-blue-400">
                                                                <textarea 
                                                                    value={itemNotes[item.locationId] || item.note || ''}
                                                                    onChange={(e) => handleSaveNote(item.locationId, e.target.value)}
                                                                    onKeyDown={(e) => {
                                                                        if (e.key === 'Enter' && !e.shiftKey) {
                                                                            e.preventDefault();
                                                                            setEditingNoteId(null);
                                                                            toast.success("Đã ghi chú");
                                                                        }
                                                                    }}
                                                                    className="w-full bg-transparent text-xs outline-none resize-none h-8 text-slate-700"
                                                                    autoFocus
                                                                    onBlur={() => setEditingNoteId(null)}
                                                                    placeholder="Viết ghi chú..."
                                                                />
                                                            </div>
                                                        </div>
                                                    ) : (
                                                        <div 
                                                            onClick={(e) => {
                                                                e.stopPropagation();
                                                                setEditingNoteId(item.locationId);
                                                            }}
                                                            className="bg-slate-50 border border-slate-100 hover:border-blue-200 transition-colors rounded-lg p-1.5 flex items-center justify-between cursor-pointer group/note"
                                                        >
                                                            {(itemNotes[item.locationId] || item.note) ? (
                                                                <div className="flex items-start gap-1.5 overflow-hidden">
                                                                    <Edit3 size={10} className="shrink-0 mt-0.5 text-blue-500" />
                                                                    <span className="text-slate-700 text-[10px] line-clamp-2">{itemNotes[item.locationId] || item.note}</span>
                                                                </div>
                                                            ) : (
                                                                <div className="flex items-center gap-1.5 text-slate-400 group-hover/note:text-blue-600 transition-colors">
                                                                    <Edit3 size={12} />
                                                                    <span className="text-[10px] font-medium">Thêm ghi chú</span>
                                                                </div>
                                                            )}
                                                        </div>
                                                    )}
                                                </AnimatePresence>
                                            </div>
                                        </div>
                                    </motion.div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>

                {/* Right Panel: Map */}
                <div className="hidden lg:block w-1/2 relative bg-slate-100">
                    <MapContainer 
                        locations={filteredItems.map(item => ({
                            id: item.locationId,
                            latitude: item.latitude,
                            longitude: item.longitude,
                            title: item.name,
                            description: item.categoryName,
                            categoryIcon: item.categoryIcon,
                            type: 'destination' 
                        }))}
                        hoveredItemId={hoveredItemId}
                        flyToLocation={selectedItem ? { latitude: selectedItem.latitude, longitude: selectedItem.longitude, zoom: 16 } : null}
                        onMarkerClick={(loc) => {
                            const item = filteredItems.find(i => i.locationId === loc.id);
                            setSelectedItem(item);
                        }}
                    />
                </div>
             </div>

             {selectedItem && (
                 <DestinationDrawer 
                    isOpen={!!selectedItem} 
                    onClose={() => setSelectedItem(null)} 
                    data={selectedItem}
                    side="left"
                    hasBackdrop={false} 
                 />
             )}
        </div>
    );
};

export default CollectionDetailPage;
