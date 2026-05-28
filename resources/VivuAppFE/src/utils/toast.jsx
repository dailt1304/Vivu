import { toast as reactToast } from "react-toastify";
import CustomToast from "../components/common/feedback/CustomToast";

const showToast = (type, message) => {
  reactToast(
    ({ closeToast }) => (
      <CustomToast type={type} message={message} closeToast={closeToast} />
    ),
    {
      icon: false,
      position: "top-right",
      autoClose: 3000,
      hideProgressBar: true,
      closeButton: false,
      // Tailwind v4: important modifier goes at the END of the class
      className:
        "p-0! bg-transparent! shadow-none! rounded-none! min-h-0! overflow-visible!",
      pauseOnFocusLoss: false,
      // Inline style as fallback to guarantee overrides
      style: {
        background: "transparent",
        boxShadow: "none",
        padding: 0,
        minHeight: 0,
        borderRadius: 0,
        overflow: "visible",
      },
    },
  );
};

const toast = {
  success: (message) => showToast("success", message),
  error: (message) => showToast("error", message),
  warning: (message) => showToast("warning", message),
  info: (message) => showToast("info", message),
};

export default toast;
