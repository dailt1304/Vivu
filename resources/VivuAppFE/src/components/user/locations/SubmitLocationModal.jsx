import React, {
  useState,
  useCallback,
  createContext,
  use,
  useRef,
  useEffect,
} from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion } from "framer-motion";
import toast from "../../../utils/toast";
import {
  X,
  MapPin,
  Image as ImageIcon,
  Phone,
  Globe,
  Clock,
  Tag,
  Info,
  Plus,
  UploadCloud,
  Trash2,
  ArrowRight,
  ChevronDown,
  Search,
  Loader2,
} from "lucide-react";
import {
  useSubmitLocation,
  useLocationCategories,
} from "../../../hooks/locations/useLocations";
import { useSearchCities } from "../../../hooks/cities/useCities";
import useDebounce from "../../../hooks/utils/useDebounce";
import AddressAutocomplete from "./AddressAutocomplete";
import CategoryIcon from "../../common/CategoryIcon";
import WeeklyScheduleInput from "../../common/inputs/WeeklyScheduleInput";

// Vercel Rule: state-context-interface - Define generic interface
// interface SubmitContextValue {
//   state: SubmitState;
//   actions: SubmitActions;
//   meta: SubmitMeta;
// }
const SubmitContext = createContext(null);

// Vercel Rule: rendering-hoist-jsx - Hoist static constants
const INITIAL_STATE = {
  name: "",
  description: "",
  address: "",
  latitude: "",
  longitude: "",
  cityId: "",
  categoryId: "",
  openingHours: "",
  phone: "",
  website: "",
  tags: "",
  images: [], // File objects
};

const MAX_IMAGES = 5;
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

// --- COMPOUND COMPONENT PARTS ---

// Frame (Modal wrapper)
const ModalFrame = ({ children, isOpen, onClose }) => {
  if (!isOpen) return null;

  return createPortal(
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="fixed inset-0 bg-black/60 backdrop-blur-sm z-[40] flex items-center justify-center p-4 pt-[80px] pb-[90px] md:p-6 md:pt-[90px] md:pb-6"
      >
        <motion.div
          initial={{ scale: 0.95, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          exit={{ scale: 0.95, opacity: 0 }}
          onClick={(e) => e.stopPropagation()}
          className="bg-white dark:bg-gray-900 rounded-3xl w-full max-w-4xl max-h-full flex flex-col shadow-2xl overflow-hidden border border-gray-100 dark:border-gray-800 relative z-[50] min-h-0"
        >
          {children}
        </motion.div>
      </motion.div>
    </AnimatePresence>,
    document.body,
  );
};

// Header
const ModalHeader = ({ title }) => {
  const {
    actions: { onClose },
    meta: { isMutating },
  } = use(SubmitContext);
  return (
    <div className="flex items-center justify-between p-6 border-b border-gray-100 dark:border-gray-800 bg-white/50 dark:bg-gray-900/50 backdrop-blur top-0 z-10 sticky">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center text-blue-600 dark:text-blue-400">
          <MapPin size={20} className="stroke-[2.5]" />
        </div>
        <h2 className="text-2xl font-bold bg-clip-text text-transparent bg-linear-to-r from-gray-900 to-gray-600 dark:from-white dark:to-gray-300">
          {title}
        </h2>
      </div>
      <button
        onClick={onClose}
        disabled={isMutating}
        className="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-500 transition-colors disabled:opacity-50"
      >
        <X size={24} />
      </button>
    </div>
  );
};

// Body (Scrollable form area)
const ModalBody = ({ children }) => {
  return (
    <div className="flex-1 overflow-y-auto p-6 md:p-8 hide-scrollbar bg-gray-50/50 dark:bg-gray-900/50">
      <div className="max-w-3xl mx-auto space-y-8">{children}</div>
    </div>
  );
};

// Footer (Actions)
const ModalFooter = ({ submitLabel = "Gửi địa điểm" }) => {
  const {
    actions: { onClose, submit },
    meta: { isMutating },
  } = use(SubmitContext);
  return (
    <div className="p-4 md:p-6 border-t border-gray-100 dark:border-gray-800 bg-white dark:bg-gray-900 sticky bottom-0 z-10 flex items-center justify-between">
      <p className="text-sm text-gray-500 hidden md:block">
        Thông tin sẽ được duyệt bởi quản trị viên trước khi hiển thị.
      </p>
      <div className="flex items-center gap-2 md:gap-3 w-full md:w-auto">
        <button
          type="button"
          onClick={onClose}
          disabled={isMutating}
          className="w-[100px] md:w-auto px-4 md:px-6 py-2.5 md:py-3 rounded-xl font-semibold text-gray-700 bg-gray-100 hover:bg-gray-200 dark:text-gray-300 dark:bg-gray-800 dark:hover:bg-gray-700 transition-colors disabled:opacity-50"
        >
          Hủy
        </button>
        <button
          type="button"
          onClick={submit}
          disabled={isMutating}
          className="flex-1 md:flex-none px-4 md:px-8 py-2.5 md:py-3 rounded-xl font-bold text-white bg-gradient-primary hover:opacity-90 shadow-lg shadow-blue-500/25 transition-all active:scale-95 disabled:opacity-75 flex items-center justify-center gap-1.5 md:gap-2 whitespace-nowrap"
        >
          {isMutating ? (
            <>
              <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              Đang xử lý...
            </>
          ) : (
            <>
              <Plus size={20} className="stroke-[2.5]" />
              {submitLabel}
            </>
          )}
        </button>
      </div>
    </div>
  );
};

// --- FORM FIELD COMPONENTS ---

const Section = ({ title, icon: Icon, children }) => (
  <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 border border-gray-100 dark:border-gray-700 shadow-sm">
    <div className="flex items-center gap-2 mb-6 text-gray-900 dark:text-white pb-4 border-b border-gray-50 dark:border-gray-700/50">
      <Icon size={20} className="text-blue-500" />
      <h3 className="font-bold text-lg">{title}</h3>
    </div>
    <div className="space-y-5">{children}</div>
  </div>
);

const Input = ({
  label,
  field,
  placeholder,
  type = "text",
  required = false,
  icon: Icon,
}) => {
  const {
    state,
    actions: { update },
  } = use(SubmitContext);
  return (
    <div className="space-y-1.5">
      <label className="text-sm font-semibold text-gray-700 dark:text-gray-300 ml-1">
        {label} {required && <span className="text-red-500">*</span>}
      </label>
      <div className="relative">
        {Icon && (
          <div className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400">
            <Icon size={18} />
          </div>
        )}
        <input
          type={type}
          value={state[field]}
          onChange={(e) => update(field, e.target.value)}
          placeholder={placeholder}
          className={`w-full bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl px-4 py-3 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all outline-none ${
            Icon ? "pl-11" : ""
          }`}
        />
      </div>
    </div>
  );
};

const Textarea = ({ label, field, placeholder, required = false }) => {
  const {
    state,
    actions: { update },
  } = use(SubmitContext);
  return (
    <div className="space-y-1.5">
      <label className="text-sm font-semibold text-gray-700 dark:text-gray-300 ml-1">
        {label} {required && <span className="text-red-500">*</span>}
      </label>
      <textarea
        value={state[field]}
        onChange={(e) => update(field, e.target.value)}
        placeholder={placeholder}
        rows={4}
        className="w-full bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl px-4 py-3 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all outline-none resize-none"
      />
    </div>
  );
};

const CustomSelect = ({
  label,
  field,
  options,
  placeholder,
  required = false,
  icon: Icon,
}) => {
  const {
    state,
    actions: { update },
  } = use(SubmitContext);
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);

  const optionsArray = Array.isArray(options) ? options : options?.items || [];
  const selectedOption = optionsArray.find(
    (opt) => (opt.value || opt.id) === state[field],
  );

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div className="space-y-1.5 flex-1 relative" ref={dropdownRef}>
      <label className="text-sm font-semibold text-gray-700 dark:text-gray-300 ml-1">
        {label} {required && <span className="text-red-500">*</span>}
      </label>
      <div
        onClick={() => setIsOpen(!isOpen)}
        className="w-full bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl px-4 py-3 text-gray-900 dark:text-white focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-blue-500 transition-all cursor-pointer flex items-center justify-between"
      >
        <div className="flex items-center gap-2 overflow-hidden">
          {Icon && <Icon size={18} className="text-gray-400 shrink-0" />}
          <span
            className={`truncate ${!selectedOption ? "text-gray-400" : ""}`}
          >
            {selectedOption
              ? selectedOption.label || selectedOption.name
              : placeholder}
          </span>
        </div>
        <ChevronDown
          size={18}
          className={`text-gray-400 transition-transform duration-200 ${isOpen ? "rotate-180" : ""}`}
        />
      </div>

      <AnimatePresence>
        {isOpen && (
          <div className="absolute top-full left-0 right-0 mt-2 bg-white dark:bg-gray-800 rounded-2xl shadow-xl shadow-blue-900/10 border border-gray-100 dark:border-gray-700 overflow-hidden z-50 py-1">
            <div className="max-h-[240px] overflow-y-auto">
              {optionsArray.length === 0 ? (
                <div className="px-4 py-3 text-sm text-gray-500 text-center">
                  Không có tùy chọn
                </div>
              ) : (
                optionsArray.map((opt) => (
                  <button
                    key={opt.id || opt.value}
                    type="button"
                    onClick={() => {
                      update(field, opt.value || opt.id);
                      setIsOpen(false);
                    }}
                    className={`w-full text-left px-4 py-2.5 text-sm transition-colors flex items-center gap-2 ${
                      state[field] === (opt.value || opt.id)
                        ? "bg-blue-50 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400 font-semibold"
                        : "text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700/50"
                    }`}
                  >
                    {state[field] === (opt.value || opt.id) && (
                      <div className="w-1.5 h-1.5 rounded-full bg-blue-500 shrink-0" />
                    )}
                    <CategoryIcon
                      name={opt.label || opt.name}
                      iconUrl={opt.iconUrl}
                      size={16}
                      className={
                        state[field] === (opt.value || opt.id)
                          ? "text-blue-600"
                          : "text-gray-400"
                      }
                    />
                    <span className="truncate">{opt.label || opt.name}</span>
                  </button>
                ))
              )}
            </div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};

const CitySearchInput = ({ label, field, placeholder, required = false }) => {
  const {
    actions: { update },
  } = use(SubmitContext);
  const [inputValue, setInputValue] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const [selectedCityName, setSelectedCityName] = useState("");
  const dropdownRef = useRef(null);

  const debouncedSearch = useDebounce(inputValue, 300);
  const {
    data: citiesData,
    trigger: searchCities,
    isMutating,
  } = useSearchCities();

  useEffect(() => {
    if (debouncedSearch.trim() && isOpen) {
      searchCities({ searchText: debouncedSearch, pageNumber: 1, pageSize: 5 });
    }
  }, [debouncedSearch, searchCities, isOpen]);

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSelect = (city) => {
    update(field, city.id);
    setSelectedCityName(city.name);
    setInputValue("");
    setIsOpen(false);
  };

  return (
    <div className="space-y-1.5 flex-1 relative" ref={dropdownRef}>
      <label className="text-sm font-semibold text-gray-700 dark:text-gray-300 ml-1">
        {label} {required && <span className="text-red-500">*</span>}
      </label>
      <div className="relative">
        <div className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400">
          <Search size={18} />
        </div>
        <input
          type="text"
          value={isOpen ? inputValue : selectedCityName || ""}
          onChange={(e) => {
            setInputValue(e.target.value);
            setIsOpen(true);
          }}
          onFocus={() => setIsOpen(true)}
          placeholder={selectedCityName || placeholder}
          className="w-full bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl pl-11 pr-10 py-3 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all outline-none"
        />
        {isMutating && (
          <div className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400">
            <Loader2 size={16} className="animate-spin" />
          </div>
        )}
      </div>

      <AnimatePresence>
        {isOpen &&
          (inputValue.trim() || (citiesData?.items?.length || 0) > 0) && (
            <div className="absolute top-full left-0 right-0 mt-2 bg-white dark:bg-gray-800 rounded-2xl shadow-xl shadow-blue-900/10 border border-gray-100 dark:border-gray-700 overflow-hidden z-50 py-1">
              <div className="max-h-[240px] overflow-y-auto">
                {!isMutating && (citiesData?.items?.length || 0) === 0 ? (
                  <div className="px-4 py-3 text-sm text-gray-500 text-center">
                    Không tìm thấy thành phố
                  </div>
                ) : (
                  citiesData?.items?.map((city) => (
                    <button
                      key={city.id}
                      type="button"
                      onClick={() => handleSelect(city)}
                      className="w-full text-left px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors flex items-center gap-3 border-b border-gray-50 dark:border-gray-800 last:border-0"
                    >
                      <div className="w-8 h-8 rounded-full bg-gray-100 dark:bg-gray-700 flex items-center justify-center text-gray-500 shrink-0">
                        <MapPin size={16} />
                      </div>
                      <div>
                        <h4 className="font-semibold text-gray-900 dark:text-white text-sm">
                          {city.name}
                        </h4>
                      </div>
                    </button>
                  ))
                )}
              </div>
            </div>
          )}
      </AnimatePresence>
    </div>
  );
};

const ImageUploader = () => {
  const {
    state,
    actions: { update, setError },
  } = use(SubmitContext);
  const fileInputRef = useRef(null);

  const handleFileChange = (e) => {
    const files = Array.from(e.target.files);
    if (!files.length) return;

    let fileError = null;
    // Check file size
    const validFiles = files.filter((file) => {
      if (file.size > MAX_FILE_SIZE) {
        fileError = `Ảnh "${file.name}" quá lớn (tối đa 5MB)`;
        return false;
      }
      return true;
    });

    // Update images if we have valid ones
    if (validFiles.length > 0) {
      const newImages = [...state.images, ...validFiles].slice(0, MAX_IMAGES);
      update("images", newImages);
    }

    // Set error LAST so it doesn't get wiped by update()
    if (fileError) {
      setError(fileError);
    }

    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const removeImage = (indexToRemove) => {
    // Vercel Rule: js-early-exit - don't need early exit but using functional updates
    update("images", (prev) => prev.filter((_, idx) => idx !== indexToRemove));
  };

  return (
    <div className="space-y-4">
      <div
        onClick={() =>
          state.images.length < MAX_IMAGES && fileInputRef.current?.click()
        }
        className={`border-2 border-dashed rounded-2xl p-8 text-center transition-all ${
          state.images.length >= MAX_IMAGES
            ? "border-gray-200 bg-gray-50 opacity-50 cursor-not-allowed dark:border-gray-700 dark:bg-gray-800/50"
            : "border-blue-200 bg-blue-50/50 hover:bg-blue-50 cursor-pointer dark:border-blue-900/50 dark:bg-blue-900/20 dark:hover:bg-blue-900/30"
        }`}
      >
        <div className="w-14 h-14 bg-white dark:bg-gray-800 rounded-full shadow-sm flex items-center justify-center mx-auto mb-4 text-blue-500">
          <UploadCloud size={24} />
        </div>
        <p className="font-semibold text-gray-900 dark:text-white mb-1">
          Bấm để tải ảnh lên
        </p>
        <p className="text-sm text-gray-500">
          PNG, JPG hoặc WebP (Tối đa {MAX_IMAGES} ảnh)
        </p>
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileChange}
          accept="image/*"
          multiple
          className="hidden"
        />
      </div>

      {state.images.length > 0 && (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-4">
          {state.images.map((file, idx) => (
            <div
              key={idx}
              className="relative aspect-square rounded-xl overflow-hidden group"
            >
              <img
                src={URL.createObjectURL(file)}
                alt={`Upload preview ${idx}`}
                className="w-full h-full object-cover"
              />
              <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    removeImage(idx);
                  }}
                  className="p-2 bg-red-500 text-white rounded-full hover:bg-red-600 transition-colors transform hover:scale-110"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// --- MAIN PROVIDER COMPONENT ---

const SubmitLocationModal = ({ isOpen, onClose, onSuccess }) => {
  const [state, setState] = useState(INITIAL_STATE);
  const [error, setError] = useState(null);

  // SWR Hooks (Vercel Rule: client-swr-dedup)
  const { data: categoriesData } = useLocationCategories();
  const { trigger: submitApi, isMutating } = useSubmitLocation();

  // Vercel Rule: rerender-functional-setstate - Use functional updates
  const update = useCallback((field, value) => {
    setState((prev) => {
      // If passing a function updater, use it
      if (typeof value === "function") {
        return { ...prev, [field]: value(prev[field]) };
      }
      return { ...prev, [field]: value };
    });
    setError(null);
  }, []);

  const handleClose = useCallback(() => {
    setState(INITIAL_STATE);
    setError(null);
    onClose();
  }, [onClose]);

  // Auto-dismiss error after 3 seconds
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => {
        setError(null);
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [error]);

  const submit = useCallback(async () => {
    // Vercel Rule: js-early-exit - Validate first
    if (
      !state.name.trim() ||
      !state.address.trim() ||
      !state.cityId ||
      !state.categoryId ||
      state.images.length === 0
    ) {
      setError("Vui lòng điền các trường bắt buộc (*) và ít nhất 1 hình ảnh");
      return;
    }

    try {
      const fd = new FormData();
      Object.entries(state).forEach(([key, val]) => {
        if (key === "images") {
          val.forEach((file) => fd.append("Images", file));
        } else if (val != null && val !== "") {
          // Capitalize first letter to match backend DTO
          fd.append(key.charAt(0).toUpperCase() + key.slice(1), val);
        }
      });

      await submitApi(fd);
      toast.success("Đóng góp địa điểm thành công! Vui lòng chờ kiểm duyệt.");
      onSuccess?.();
      handleClose();
    } catch (err) {
      const msg =
        err?.response?.data?.message || "Lỗi nộp địa điểm. Vui lòng thử lại.";
      setError(msg);
      toast.error(msg);
    }
  }, [state, submitApi, onSuccess, handleClose]);

  // Context value mapping
  const contextValue = {
    state,
    actions: { update, submit, setError, onClose: handleClose },
    meta: { isMutating, error, categories: categoriesData?.items || [] },
  };

  return (
    <SubmitContext value={contextValue}>
      <ModalFrame isOpen={isOpen} onClose={handleClose}>
        <ModalHeader title="Đóng góp địa điểm mới" />

        <ModalBody>
          <div className="sticky -top-px z-50 -mx-6 -mt-6 mb-6">
            {error && (
              <div className="bg-red-50/95 dark:bg-red-900/40 backdrop-blur-sm rounded-2xl text-red-600 dark:text-red-400 px-6 py-3.5 text-sm font-semibold border-b border-red-100 dark:border-red-800/50 flex items-center gap-3 shadow-md shadow-red-900/5 animate-in fade-in slide-in-from-top-full duration-300">
                <Info size={18} className="shrink-0" />
                <span className="flex-1">{error}</span>
                <button
                  onClick={() => setError(null)}
                  className="p-1.5 hover:bg-black/5 dark:hover:bg-white/5 rounded-full transition-colors shrink-0"
                >
                  <X size={16} />
                </button>
              </div>
            )}
          </div>

          <Section title="Thông tin cơ bản" icon={Info}>
            <Input
              label="Tên địa điểm"
              field="name"
              placeholder="VD: Quán Cafe A..."
              required
            />
            <Textarea
              label="Mô tả"
              field="description"
              placeholder="Giới thiệu nhanh về địa điểm này..."
            />
          </Section>

          <Section title="Vị trí & Phân loại" icon={MapPin}>
            <div className="flex flex-col md:flex-row gap-4 mb-5">
              <CitySearchInput
                label="Tỉnh/Thành phố"
                field="cityId"
                placeholder="Tìm thành phố..."
                required
              />
              <CustomSelect
                label="Phân loại"
                field="categoryId"
                placeholder="-- Chọn loại hình --"
                icon={Tag}
                options={categoriesData?.items || []}
                required
              />
            </div>

            <AddressAutocomplete
              value={state.address}
              onChange={(val) => update("address", val)}
              onSelectPlace={(address, lat, lng) => {
                update("address", address);
                update("latitude", lat);
                update("longitude", lng);
              }}
            />

            {state.latitude && state.longitude && (
              <div className="flex gap-2 items-center text-sm text-green-700 bg-green-50/50 border border-green-200 px-4 py-3 rounded-xl mt-2">
                <MapPin size={16} />
                <b>Tọa độ bản đồ:</b> {state.latitude}, {state.longitude}
              </div>
            )}
          </Section>

          <Section title="Thông tin liên hệ" icon={Phone}>
            <div className="w-full">
              <WeeklyScheduleInput
                value={state.openingHours}
                onChange={(json) => update("openingHours", json)}
                compact={true}
                variant="user"
              />
            </div>
            
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <Input
                label="Số điện thoại"
                field="phone"
                icon={Phone}
                placeholder="09xx xxx xxx"
              />
              <Input
                label="Website / Fanpage"
                field="website"
                icon={Globe}
                placeholder="https://..."
              />
            </div>

            <Input
              label="Tags (Từ khóa)"
              field="tags"
              icon={Tag}
              placeholder="cafe, sống ảo, vintage... (cách nhau bởi dấu phẩy)"
            />
          </Section>

          <Section
            title={
              <span>
                Hình ảnh <span className="text-red-500">*</span>
              </span>
            }
            icon={ImageIcon}
          >
            <ImageUploader />
          </Section>
        </ModalBody>

        <ModalFooter />
      </ModalFrame>
    </SubmitContext>
  );
};

export default SubmitLocationModal;
