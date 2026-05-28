import { useState, useEffect, useRef } from "react";
import aiApi from "../../api/aiApi";

export const useAITripChat = (tripId) => {
  const [isStreaming, setIsStreaming] = useState(false);
  const [streamedText, setStreamedText] = useState("");
  const abortControllerRef = useRef(null);
  const isMountedRef = useRef(true);

  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  const sendAIMessage = async (message, connectionId, callbacks = {}) => {
    // Abort previous request if still running
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    const controller = new AbortController();
    abortControllerRef.current = controller;

    setIsStreaming(true);
    setStreamedText("");

    try {
      await aiApi.streamTripChat(
        tripId,
        message,
        connectionId,
        {
          onStart: (data) => {
            if (isMountedRef.current && callbacks.onStart)
              callbacks.onStart(data);
          },
          onParsing: (data) => {
            if (isMountedRef.current && callbacks.onParsing)
              callbacks.onParsing(data);
          },
          onChunk: (chunk) => {
            if (isMountedRef.current) {
              setStreamedText((prev) => prev + chunk);
              if (callbacks.onChunk) callbacks.onChunk(chunk);
            }
          },
          onComplete: (data) => {
            if (isMountedRef.current) {
              setIsStreaming(false);
              if (callbacks.onComplete) callbacks.onComplete(data);
            }
          },
          onSaving: (data) => {
            if (isMountedRef.current && callbacks.onSaving)
              callbacks.onSaving(data);
          },
          onSaved: (data) => {
            if (isMountedRef.current && callbacks.onSaved)
              callbacks.onSaved(data);
          },
          onError: (err) => {
            if (isMountedRef.current) {
              setIsStreaming(false);
              if (callbacks.onError) callbacks.onError(err);
            }
          },
        },
        {
          signal: controller.signal,
        },
      );
    } catch (err) {
      if (err.name === "AbortError") {
        console.log("AI Stream aborted");
      } else {
        if (isMountedRef.current) {
          setIsStreaming(false);
          if (callbacks.onError) callbacks.onError(err.message);
        }
      }
    } finally {
      if (isMountedRef.current && abortControllerRef.current === controller) {
        abortControllerRef.current = null;
      }
    }
  };

  return { sendAIMessage, isStreaming, streamedText, setStreamedText };
};
