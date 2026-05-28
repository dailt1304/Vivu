import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { GoogleOAuthProvider } from "@react-oauth/google";
import { RouterProvider } from "react-router-dom";
import { ToastContainer } from "react-toastify";
import "./index.css";
import router from "./routes";

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <GoogleOAuthProvider clientId={import.meta.env.VITE_GOOGLE_CLIENT_ID}>
      <RouterProvider router={router} />
    </GoogleOAuthProvider>
    <ToastContainer
      position="top-right"
      autoClose={3000}
      closeButton={false}
      hideProgressBar
      toastClassName="bg-transparent! shadow-none! p-0! min-h-0! rounded-none! overflow-visible!"
      style={{ zIndex: 99999 }}
    />
  </StrictMode>,
);
