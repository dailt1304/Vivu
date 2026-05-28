import { useReducer, useCallback } from "react";
import { format } from "date-fns";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { useAuth } from "../../contexts/auth-context";
import { generationCheckpoint } from "../../utils/generationCheckpoint";

const initialState = {
  step: 1,
  data: {
    destination: null, // object: { id, name, ... } or string
    startDate: null,
    endDate: null,
    tripType: null, // solo, family, friends
    tripSize: 1,
    budget: "balanced", // saving, balanced, luxury
    interests: [], // array of category objects or ids
    travelStyle: "moderate", // relaxed, moderate, packed
    title: "",
    notes: "",
    selectedCoverUrl: null,
  },
  errors: {},
};

function formReducer(state, action) {
  switch (action.type) {
    case "SET_FIELD":
      return {
        ...state,
        data: {
          ...state.data,
          ...action.payload,
        },
        // Clear error for the field being updated
        errors: {
          ...state.errors,
          ...Object.keys(action.payload).reduce((acc, key) => {
            acc[key] = null;
            return acc;
          }, {}),
        },
      };
    case "SET_ERROR":
      return {
        ...state,
        errors: {
          ...state.errors,
          [action.payload.field]: action.payload.message,
        },
      };
    case "NEXT_STEP":
      return {
        ...state,
        step: Math.min(state.step + 1, 3),
      };
    case "PREV_STEP":
      return {
        ...state,
        step: Math.max(state.step - 1, 1),
      };
    case "RESET":
      return initialState;
    default:
      return state;
  }
}

export function useCreateTripForm() {
  const [state, dispatch] = useReducer(formReducer, initialState);
  const navigate = useNavigate();
  const { user } = useAuth();

  const setFormData = useCallback((payload) => {
    dispatch({ type: "SET_FIELD", payload });
  }, []);

  const setError = useCallback((field, message) => {
    dispatch({ type: "SET_ERROR", payload: { field, message } });
  }, []);

  const validateStep = useCallback(() => {
    const { step, data } = state;
    let isValid = true;

    if (step === 1) {
      if (!data.destination) {
        setError("destination", "Vui lòng chọn điểm đến");
        isValid = false;
      }
      if (!data.startDate) {
        setError("startDate", "Vui lòng chọn ngày bắt đầu");
        isValid = false;
      }
      if (!data.endDate) {
        setError("endDate", "Vui lòng chọn ngày kết thúc");
        isValid = false;
      }
      if (data.startDate && data.endDate && data.startDate > data.endDate) {
         setError("endDate", "Ngày kết thúc phải sau ngày bắt đầu");
         isValid = false;
      }
    }
    
    // Step 2 & 3 fields are mostly optional
    if (step === 2) {
       if (data.tripSize < 1) {
           setError("tripSize", "Số người phải lớn hơn 0");
           isValid = false;
       }
    }

    return isValid;
  }, [state, setError]);

  const nextStep = useCallback(() => {
    if (validateStep()) {
      dispatch({ type: "NEXT_STEP" });
    }
  }, [validateStep]);

  const prevStep = useCallback(() => {
    dispatch({ type: "PREV_STEP" });
  }, []);

  const submitForm = useCallback(() => {
     if (!validateStep()) return;

     // Vercel Rule: js-early-exit — chặn duplicate generation
     const existingCp = generationCheckpoint.get(user?.id);
     if (existingCp?.status === 'streaming' || existingCp?.status === 'pending') {
       // Auto-clear stale checkpoints (backend cache expires in 10 min)
       const age = Date.now() - (existingCp.ts || 0);
       if (age > 10 * 60 * 1000) {
         generationCheckpoint.clear();
       } else {
         toast.warning('Bạn đang có lịch trình đang tạo. Vui lòng đợi hoàn tất hoặc hủy trước.');
         return;
       }
     }

     const { data } = state;
     const destinationName = typeof data.destination === 'object' && data.destination !== null ? data.destination.name : data.destination;
     
     // Build the prompt string for the UI (Chat Bubble)
     let prompt = `Lên lịch trình đi ${destinationName}`;
     
     if (data.startDate && data.endDate) {
         prompt += ` từ ngày ${format(data.startDate, 'dd/MM/yyyy')} đến ngày ${format(data.endDate, 'dd/MM/yyyy')}.`;
     }

     let prefs = [];
     const tripTypeMapUI = {
         'solo': 'Đi một mình',
         'couple': 'Cặp đôi',
         'friends': 'Đi cùng bạn bè',
         'family': 'Đi cùng gia đình',
         'senior': 'Người lớn tuổi',
     };
     if (data.tripType) prefs.push(`Hình thức: ${tripTypeMapUI[data.tripType]}`);
     if (data.tripSize > 0) prefs.push(`Số người: ${data.tripSize}`);
     
     const budgetMapUI = {
         'saving': 'Tiết kiệm',
         'balanced': 'Cân đối',
         'luxury': 'Sang chảnh/Cao cấp'
     };
     if (data.budget) prefs.push(`Ngân sách: ${budgetMapUI[data.budget]}`);


     if (data.interests && data.interests.length > 0) {
         const interestNames = data.interests.map(i => typeof i === 'object' ? i.title || i.name : i);
         prefs.push(`Sở thích: ${interestNames.join(', ')}`);
     }

     let finalNotes = data.notes || "";

     if (finalNotes) {
         prefs.push(`Ghi chú bổ sung: ${finalNotes}`);
     }

     if (prefs.length > 0) {
         prompt += `\n[Thông tin bổ sung từ người dùng]:\n- ` + prefs.join("\n- ");
     }

     if (data.title) {
        prompt += `\nTitle: ${data.title}`;
     }
     
     // Build the Structured Payload for Structured Generation (StreamGenerateTripCommand)
     const budgetValueMap = {
         'saving': 1,
         'balanced': 2,
         'luxury': 3
     };

     const groupCompositionMap = {
         'solo': 1,
         'couple': 2,
         'family': 3,
         'friends': 4,
         'senior': 5
     };

     const payload = {
         generationId: crypto.randomUUID(),
         cityId: typeof data.destination === 'object' && data.destination?.id ? data.destination.id : "",
         coverUrl: data.selectedCoverUrl || null,
         startDate: data.startDate ? format(data.startDate, 'yyyy-MM-dd') : null,
         endDate: data.endDate ? format(data.endDate, 'yyyy-MM-dd') : null,
         groupSize: parseInt(data.tripSize) || 1,
         groupComposition: data.tripType ? groupCompositionMap[data.tripType] : 0,
         budget: budgetValueMap[data.budget] || 2, // Default Balanced
         preferences: data.interests ? data.interests.map(i => (typeof i === 'object' ? (i.title || i.name) : i).toString()) : [],
         notes: finalNotes || null,
         title: data.title || null,
         autoSave: true,
         generateInviteCode: true
     };

     // Persist generation state to localStorage via versioned checkpoint.
     // Unlike sessionStorage, this survives browser reloads and tab navigations,
     // allowing TripChatPanel to recover the payload if the user reloads mid-stream.
     // Rule: client-localstorage-schema — versioned, user-scoped, expiry-aware utility.
     generationCheckpoint.start(user?.id, payload);
     navigate("/trips/new?generating=true");

  }, [state, navigate, validateStep]);

  return {
    ...state,
    setFormData,
    nextStep,
    prevStep,
    submitForm,
  };
}
