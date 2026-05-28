import axiosClient from "./axiosClient";

const SSE_BLOCK_DELIMITER_REGEX = /\r?\n\r?\n/;
const SSE_LINE_BREAK_REGEX = /\r?\n/;
const CHUNK_CODE_FENCE_PREFIX_REGEX = /^```(?:json)?\s*/i;
const CHUNK_CODE_FENCE_SUFFIX_REGEX = /\s*```$/i;

const sanitizeChunkData = (chunk) => {
  if (!chunk) return "";

  return chunk
    .replace(CHUNK_CODE_FENCE_PREFIX_REGEX, "")
    .replace(CHUNK_CODE_FENCE_SUFFIX_REGEX, "");
};

const parseSSEEventBlock = (block) => {
  const lines = block.split(SSE_LINE_BREAK_REGEX);
  let eventType = null;
  const dataLines = [];

  for (const line of lines) {
    if (!line) continue;

    if (line.startsWith("event:")) {
      eventType = line.slice(6).trim();
      continue;
    }

    if (line.startsWith("data:")) {
      let data = line.slice(5);
      if (data.startsWith(" ")) {
        data = data.slice(1);
      }
      dataLines.push(data);
      continue;
    }

    // Capture continuation lines (JSON spanning multiple lines where only
    // the first line starts with 'data: ')
    if (dataLines.length > 0) {
      dataLines.push(line);
    }
  }

  return {
    eventType,
    dataPart: dataLines.join("\n"),
  };
};

const aiApi = {
  // Helper: Parse SSE stream
  _parseSSEStream: async (url, body, callbacks, signal) => {
    const {
      onStart,
      onParsing,
      onChunk,
      onComplete,
      onError,
      onSaving,
      onSaved,
    } = callbacks;

    try {
      const token = localStorage.getItem("access_token");
      const response = await fetch(url, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(body),
        signal, // Pass the AbortSignal
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";

      const processEventBlock = (block) => {
        const trimmedBlock = block.trim();
        if (!trimmedBlock) return;

        const { eventType, dataPart } = parseSSEEventBlock(trimmedBlock);
        if (!eventType) return;

        try {
          let data = null;
          if (eventType === "chunk") {
            data = sanitizeChunkData(dataPart);
          } else {
            try {
              data = dataPart ? JSON.parse(dataPart) : null;
            } catch {
              data = dataPart;
            }
          }

          switch (eventType) {
            case "start":
              if (onStart) onStart(data);
              break;
            case "parsing":
              if (onParsing) onParsing(data);
              break;
            case "chunk":
              if (onChunk && data) onChunk(data);
              break;
            case "complete":
            case "json":
              if (onComplete) onComplete(data);
              break;
            case "saving":
              if (onSaving) onSaving(data);
              break;
            case "saved":
              if (onSaved) onSaved(data);
              break;
            case "error":
              if (onError) onError(dataPart);
              break;
          }
        } catch (e) {
          console.error("Error parsing SSE data:", e, dataPart);
        }
      };

      while (true) {
        const { value, done } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true });
        buffer += chunk;

        const eventBlocks = buffer.split(SSE_BLOCK_DELIMITER_REGEX);
        buffer = eventBlocks.pop() || "";

        for (const eventBlock of eventBlocks) {
          processEventBlock(eventBlock);
        }
      }

      const trailingChunk = decoder.decode();
      if (trailingChunk) {
        buffer += trailingChunk;
      }

      if (buffer.trim()) {
        processEventBlock(buffer);
      }
    } catch (error) {
      if (onError) onError(error.message);
    }
  },

  // Streaming call for Generate
  streamGenerateTrip: async (payload, callbacks, options = {}) => {
    const { signal } = options;
    return aiApi._parseSSEStream(
      `${import.meta.env.VITE_API_URL}/ai/stream/generate-trip`,
      payload,
      callbacks,
      signal,
    );
  },

  // Streaming call for Trip Chat
  streamTripChat: async (
    tripId,
    message,
    connectionId,
    callbacks,
    options = {},
  ) => {
    const { skipSavingUserMessage = false, signal } = options;
    const url = `${import.meta.env.VITE_API_URL}/ai/trips/${tripId}/chat?connectionId=${connectionId}`;
    return aiApi._parseSSEStream(
      url,
      { userMessage: message, skipSavingUserMessage },
      callbacks,
      signal,
    );
  },

  /**
   * Poll generation status by tracking ID.
   * Vercel Rule: async-error-handling — caller handles errors.
   * @param {string} generationId - Client-generated tracking ID
   * @returns {Promise<{status: string, tripId?: string, error?: string}>}
   */
  getGenerationStatus: async (generationId) => {
    const token = localStorage.getItem("access_token");
    // No token → skip silently (auth not ready or user logged out)
    if (!token) {
      console.warn('[getGenerationStatus] No token in localStorage');
      return { status: "unknown" };
    }

    console.log('[getGenerationStatus] Token length:', token.length, 'first 20 chars:', token.substring(0, 20));

    const res = await fetch(
      `${import.meta.env.VITE_API_URL}/ai/generation-status/${generationId}`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    console.log('[getGenerationStatus] Response status:', res.status);

    // 401/403 → token expired, don't trigger refresh; just report unknown
    if (res.status === 401 || res.status === 403) {
      return { status: "unknown" };
    }
    // 404 → generation not tracked yet
    if (res.status === 404) {
      return { status: "unknown" };
    }
    if (!res.ok) throw new Error(`Status ${res.status}`);
    return res.json();
  },
};

export default aiApi;
