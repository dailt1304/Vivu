import React, {
  useState,
  useEffect,
  useRef,
  useCallback,
  useImperativeHandle,
  forwardRef,
} from "react";
import { useNavigate, useSearchParams, useBlocker } from "react-router-dom";
import {
  Send,
  Sparkles,
  Loader2,
  ImagePlus,
  X,
  RotateCcw,
  Home,
  AlertTriangle,
} from "lucide-react";
import { format, parseISO } from "date-fns";

import { Avatar, AvatarImage, AvatarFallback } from "../../ui/avatar";
import MessageBubble from "../chat/MessageBubble";
import MentionSuggestions from "../chat/MentionSuggestions";
import AIUsageIndicator from "../chat/AIUsageIndicator";

import aiApi from "../../../api/aiApi";
import tripMemberApi from "../../../api/tripMemberApi";
import tripApi from "../../../api/tripApi";
import useTypewriterBuffer from "../../../hooks/chat/useTypewriterBuffer";
import useChatHistory from "../../../hooks/chat/useChatHistory";
import { useTripChat } from "../../../hooks/chat/useTripChat";
import { useGenerationPolling } from "../../../hooks/chat/useGenerationPolling";
import { useUserUsage } from "../../../hooks/users/useUsers";
import { convertTripPlanToTripData } from "../../../utils/aiTripParser";
import IncrementalTripFormatter from "../../../utils/IncrementalTripFormatter";
import { generationCheckpoint } from "../../../utils/generationCheckpoint";
import ConfirmationModal from "../../common/modals/ConfirmationModal";
import toast from "../../../utils/toast";

const TripChatPanel = forwardRef(
  (
    {
      id,
      actualTripId,
      setActualTripId,
      currentUser,
      userId,
      fetchTrip,
      fetchTripById,
      tripData,
      setTripData,
      setEditableTitle,
      setEditableStartDate,
      setEditableEndDate,
      setIsDrawerOpen,
      handleTripUpdated,
      handleMemberListChanged,
      handleMemberKicked,
    },
    ref,
  ) => {
    const navigate = useNavigate();
    const [searchParams, setSearchParams] = useSearchParams();

    const isGeneratingMode =
      id === "new" && searchParams.get("generating") === "true";

    // Typewriter buffer
    const { displayedText, setTargetText, flush, reset, isDraining } =
      useTypewriterBuffer(15);
    const generationBotMessageIdRef = useRef(null);
    const streamedGenerationTextRef = useRef("");

    const messageRef = useRef("");
    const mentionTimeoutRef = useRef(null);
    const messagesEndRef = useRef(null);
    const [chatMessages, setChatMessages] = useState([]);
    const [isStreaming, setIsStreaming] = useState(false);

    const [chatMode, setChatMode] = useState(null); // 'ai' | 'group' | null
    const [tripMembers, setTripMembers] = useState([]);

    const { usage, mutate: mutateUsage } = useUserUsage();
    const isAiExhausted =
      chatMode === "ai" &&
      usage?.hasActiveSubscription === false &&
      usage?.remaining === 0;

    const [mentionQuery, setMentionQuery] = useState("");
    const [showMentions, setShowMentions] = useState(false);
    const inputRef = useRef(null);
    const fileInputRef = useRef(null);
    const [selectedImage, setSelectedImage] = useState(null);
    const [isUploadingImage, setIsUploadingImage] = useState(false);

    const { messages: historyMessages, mutate: mutateChatHistory } =
      useChatHistory(id, 50);

    // Guard against React Strict Mode double-firing the generation stream
    const hasStartedGenerating = useRef(false);
    const [hasSaved, setHasSaved] = useState(false);

    // Error Recovery: track generation failure + saved payload for retry
    const [generationFailed, setGenerationFailed] = useState(false);
    const generationPayloadRef = useRef(null);

    // B2 — Background generation waiting state (after reload while AI is running)
    const [isWaitingForBackground, setIsWaitingForBackground] = useState(false);
    const [generationJustCompleted, setGenerationJustCompleted] = useState(false);
    const generationTimeoutRef = useRef(null);

    // Vercel Rule: advanced-init-once — stabilize token identity across re-renders
    // to prevent useTripChat from re-triggering SignalR connection.
    const [accessToken] = useState(() => localStorage.getItem("access_token"));

    // Task A3 — SSE/SignalR dedup
    // Tracks whether THIS client is currently owning an active SSE AI stream.
    // When true, incoming SignalR ReceiveAIChunk events from the server are
    // ignored to prevent duplicate chunk rendering (the SSE already delivers them).
    // Rule: rerender-use-ref-transient-values — ref avoids triggering re-renders.
    const isLocallyStreamingRef = useRef(false);

    const refreshMembers = useCallback(async () => {
      if (!id || id === "new") return;
      try {
        const res = await tripMemberApi.getMembers(id);
        if (res.success) {
          const members = res.data.items;
          setTripMembers(members);

          if (members.length > 1) {
            setChatMode("group");
          } else {
            setChatMode("ai");
          }
        }
      } catch (e) {
        console.error("Failed to refresh members:", e);
      }
    }, [id]);

    useEffect(() => {
      if (!id || !currentUser) return;

      if (id === "new") {
        setChatMode("ai");
        return;
      }

      refreshMembers();
    }, [id, currentUser, refreshMembers]);

    useEffect(() => {
      if (
        historyMessages &&
        historyMessages.length > 0 &&
        chatMessages.length === 0
      ) {
        const formatted = historyMessages
          .filter((msg) => msg.messageType !== "initial_prompt")
          .map((msg) => {
            const isAi = msg.isAiMessage;
            const isCurrentUser = msg.senderId === userId;

            let role;
            if (isAi) role = "assistant";
            else if (isCurrentUser) role = "user";
            else role = "member";

            return {
              id: msg.id,
              role,
              sender: {
                id: msg.senderId,
                fullName: msg.senderName || "User",
                avatarUrl: msg.senderAvatar,
              },
              content: msg.content,
              messageType: msg.messageType,
              imageUrl: msg.imageUrl,
              imageFileName: msg.imageFileName,
              timestamp: new Date(msg.createdAt),
            };
          });
        setChatMessages(formatted);
      }
    }, [historyMessages, currentUser, userId, chatMessages.length]);

    const handleMessageReceived = useCallback(
      (newMsg) => {
        setChatMessages((prev) => {
          if (prev.some((m) => m.id === newMsg.id)) return prev;

          const isAi = newMsg.isAiMessage;
          const isCurrentUser = newMsg.senderId === userId;

          let role;
          if (isAi) role = "assistant";
          else if (isCurrentUser) role = "user";
          else role = "member";

          const formatted = {
            id: newMsg.id,
            role,
            sender: {
              id: newMsg.senderId,
              fullName: newMsg.senderName || "User",
              avatarUrl: newMsg.senderAvatar,
            },
            content: newMsg.content,
            messageType: newMsg.messageType,
            imageUrl: newMsg.imageUrl,
            imageFileName: newMsg.imageFileName,
            timestamp: new Date(newMsg.createdAt),
          };

          return [...prev, formatted];
        });
      },
      [userId],
    );

    const handleAIProcessing = useCallback((payload) => {
      // Guard: if this client owns the active SSE stream, the local sendToAi
      // already created a bot bubble — skip the duplicate SignalR broadcast.
      if (isLocallyStreamingRef.current) return;

      setChatMessages((prev) => {
        const lastMsg = prev[prev.length - 1];
        if (lastMsg?.isRemoteStreaming) {
          return prev.map((msg, i) =>
            i === prev.length - 1
              ? {
                  ...msg,
                  status:
                    payload.status?.message || payload.status || msg.status,
                }
              : msg,
          );
        }
        return [
          ...prev,
          {
            id: `remote-ai-${Date.now()}`,
            role: "assistant",
            content: "",
            isStreaming: true,
            isRemoteStreaming: true,
            status:
              payload.status?.message ||
              payload.status ||
              "VivuAI đang xử lý yêu cầu...",
          },
        ];
      });
    }, []);

    const handleAIChunkReceived = useCallback((chunkText) => {
      // Task A3 — Guard: if this client is currently receiving chunks directly
      // via its own SSE connection, skip the SignalR duplicate.
      // After the local stream ends (isLocallyStreamingRef = false), remote
      // chunks from other users' AI requests flow through normally.
      if (isLocallyStreamingRef.current) return;

      setChatMessages((prev) => {
        const lastMsg = prev[prev.length - 1];

        if (lastMsg?.role === "assistant" && lastMsg?.isRemoteStreaming) {
          const rawContent = (lastMsg._rawContent || "") + chunkText;
          let displayContent = rawContent;

          if (rawContent.trim().startsWith("{")) {
            const summaryMatch = rawContent.match(
              /"summary"\s*:\s*"((?:[^"\\]|\\.)*)/,
            );
            if (summaryMatch) {
              displayContent = summaryMatch[1]
                .replace(/\\n/g, "\n")
                .replace(/\\"/g, '"');
            } else {
              displayContent = "Đang phân tích thay đổi...";
            }
          }

          return prev.map((msg, i) =>
            i === prev.length - 1
              ? { ...msg, _rawContent: rawContent, content: displayContent }
              : msg,
          );
        }

        return [
          ...prev,
          {
            id: `remote-ai-${Date.now()}`,
            role: "assistant",
            content: chunkText,
            _rawContent: chunkText,
            isStreaming: true,
            isRemoteStreaming: true,
            status: "VivuAI đang trả lời...",
          },
        ];
      });
    }, []);

    const handleAIMessageReceived = useCallback((payload) => {
      setChatMessages((prev) =>
        prev.map((msg) => {
          if (!msg.isRemoteStreaming) return msg;

          let finalContent = payload.data?.message || payload.data?.summary;
          if (!finalContent && msg._rawContent) {
            try {
              const parsed = JSON.parse(msg._rawContent);
              finalContent = parsed.summary || msg.content;
            } catch {
              finalContent = msg.content;
            }
          }

          return {
            ...msg,
            isStreaming: false,
            isRemoteStreaming: false,
            status: null,
            content: finalContent || msg.content,
            _rawContent: undefined,
          };
        }),
      );
    }, []);

    const handleAIError = useCallback((payload) => {
      setChatMessages((prev) =>
        prev.map((msg) => {
          if (!msg.isRemoteStreaming) return msg;

          return {
            ...msg,
            isStreaming: false,
            isRemoteStreaming: false,
            status: null,
            content: payload?.message || "Đã có lỗi xảy ra. Vui lòng thử lại.",
            _rawContent: undefined,
          };
        }),
      );
    }, []);

    const handleMessageDeleted = useCallback(
      (payload) => {
        const { messageId } = payload;

        // 1. Remove from local UI state immediately
        setChatMessages((prev) => prev.filter((msg) => msg.id !== messageId));

        // 2. Update SWR Infinite cache directly (no API re-fetch needed)
        //    This prevents stale data showing up when navigating back to the trip
        mutateChatHistory(
          (pages) => {
            if (!pages) return pages;
            return pages.map((page) => ({
              ...page,
              items: page.items?.filter((item) => item.id !== messageId) || [],
            }));
          },
          { revalidate: false },
        );

        // 3. Refresh Trip Gallery images in case the deleted message had images
        import("swr").then(({ mutate }) => {
          mutate(["trip-images", id]);
        });
      },
      [id, mutateChatHistory],
    );

    // B3 — Handle TripGenerationCompleted from user-{userId} SignalR group.
    // Fires when the backend finishes saving the trip after the client reloaded.
    const handleTripGenerationCompleted = useCallback((payload) => {
      if (!isGeneratingMode) return;
      clearTimeout(generationTimeoutRef.current);
      const tripId = payload?.tripId || payload?.TripId || payload?.id || payload?.Id;
      if (tripId) {
        generationCheckpoint.markSaved(userId, tripId);
        setIsWaitingForBackground(false);
        setGenerationJustCompleted(true);
        // Brief delay so the user sees the "completed" screen before redirect
        setTimeout(() => {
          generationCheckpoint.clear();
          navigate(`/trips/${tripId}`, { replace: true });
        }, 1500);
      }
    }, [isGeneratingMode, navigate, userId]);

    // B3 — Handle TripGenerationFailed from user-{userId} SignalR group.
    const handleTripGenerationFailed = useCallback(() => {
      if (!isGeneratingMode) return;
      clearTimeout(generationTimeoutRef.current);
      generationCheckpoint.clear();
      setIsWaitingForBackground(false);
      setGenerationFailed(true);
    }, [isGeneratingMode]);

    const {
      sendMessage: sendSignalRMessage,
      broadcastTripEdit,
      isConnected,
      connectionId,
    } = useTripChat(
      id !== "new" ? id : null,
      accessToken,
      {
        onMessageReceived: handleMessageReceived,
        onAIProcessing: handleAIProcessing,
        onAIChunk: handleAIChunkReceived,
        onAIMessage: handleAIMessageReceived,
        onTripUpdated: handleTripUpdated,
        onMemberListChanged: (payload) => {
          refreshMembers();
          if (handleMemberListChanged) {
            handleMemberListChanged(payload);
          }
        },
        onMemberKicked: handleMemberKicked,
        onAIError: handleAIError,
        onMessageDeleted: handleMessageDeleted,
        // B3 — Wire generation completion callbacks for the waiting screen
        onTripGenerationCompleted: handleTripGenerationCompleted,
        onTripGenerationFailed: handleTripGenerationFailed,
      },
    );

    // Vercel Rule: rerender-derived-state-no-effect — derive during render
    const waitingGenerationId = isWaitingForBackground
      ? generationPayloadRef.current?.generationId
      : null;

    // Polling fallback: fires alongside SignalR, whoever delivers first wins.
    useGenerationPolling({
      generationId: waitingGenerationId,
      enabled: isWaitingForBackground,
      onCompleted: handleTripGenerationCompleted,
      onFailed: handleTripGenerationFailed,
    });

    // Vercel Rule: rerender-dependencies — cleanup timeout on unmount
    useEffect(() => {
      return () => clearTimeout(generationTimeoutRef.current);
    }, []);

    useImperativeHandle(ref, () => ({
      broadcastTripEdit,
      refreshMembers,
      isConnected,
    }), [broadcastTripEdit, refreshMembers, isConnected]);

    const startGenerateStream = useCallback(
      async (prompt) => {
        try {
          setIsStreaming(true);
          setHasSaved(false);
          setGenerationFailed(false);
          reset();
          streamedGenerationTextRef.current = "";

          const botMessageId = "generation-bot";
          generationBotMessageIdRef.current = botMessageId;
          setChatMessages((prev) => [
            ...prev,
            {
              id: botMessageId,
              role: "assistant",
              content: "",
              isStreaming: true,
              status: "Đang phân tích...",
            },
          ]);

          const updateBotMsg = (updates) => {
            setChatMessages((prev) =>
              prev.map((msg) =>
                msg.id === botMessageId ? { ...msg, ...updates } : msg,
              ),
            );
          };

          const formatter = new IncrementalTripFormatter();

          await aiApi.streamGenerateTrip(
            prompt,
            {
              onStart: () =>
                updateBotMsg({ status: "Đang thiết kế lịch trình..." }),
              onParsing: () =>
                updateBotMsg({ status: "Đang phân tích yêu cầu..." }),
              onChunk: (chunk) => {
                const fullText = formatter.addChunk(chunk);
                if (
                  fullText &&
                  fullText.length > streamedGenerationTextRef.current.length
                ) {
                  streamedGenerationTextRef.current = fullText;
                  setTargetText(fullText);
                }
              },
              onComplete: (tripPlan) => {
                const finalText = formatter.finalize(tripPlan);
                streamedGenerationTextRef.current = finalText;
                setTargetText(finalText);

                updateBotMsg({ status: "Đang lưu lịch trình..." });

                const mockData = convertTripPlanToTripData(tripPlan);
                setTripData(mockData);

                setEditableTitle(tripPlan.Title || "Chuyến đi mới");
                if (tripPlan.Start)
                  setEditableStartDate(parseISO(tripPlan.Start));
                if (tripPlan.End) setEditableEndDate(parseISO(tripPlan.End));

                setIsDrawerOpen(true);
              },
              onSaving: () => updateBotMsg({ status: "Đang lưu..." }),
              onSaved: (savedTrip) => {
                setHasSaved(true);

                const newTripId = savedTrip?.id || savedTrip?.Id;
                if (newTripId) {
                  // Task B3 — Persist tripId to checkpoint BEFORE navigating.
                  // If navigation races with a reload, the checkpoint will
                  // redirect correctly on next mount.
                  generationCheckpoint.markSaved(userId, newTripId);

                  setSearchParams({}, { replace: true });
                  window.history.replaceState({}, "", `/trips/${newTripId}`);
                  setActualTripId(newTripId);
                  fetchTripById(newTripId);

                  // Clear checkpoint after successful navigation
                  generationCheckpoint.clear();
                } else {
                  toast.error("Không tìm thấy ID chuyến đi sau khi lưu");
                }
              },
              onError: (err) => {
                console.error("Stream error:", err);
                flush();
                setHasSaved(false);
                setGenerationFailed(true);

                // Determine user-friendly error message
                let errorContent = "";
                const errStr =
                  typeof err === "string" ? err : err?.message || String(err);
                if (
                  errStr.includes("503") ||
                  errStr.toLowerCase().includes("unavailable")
                ) {
                  errorContent =
                    "❌ Hệ thống AI hiện đang quá tải, vui lòng thử lại sau giây lát.";
                } else if (errStr.includes("RateLimitExceeded")) {
                  errorContent =
                    "❌ Bạn đã hết lượt sử dụng AI hôm nay. Vui lòng nâng cấp gói để tiếp tục.";
                  mutateUsage();
                } else if (
                  errStr.includes("timeout") ||
                  errStr.includes("Timeout")
                ) {
                  errorContent =
                    "❌ Yêu cầu đã hết thời gian chờ. Vui lòng thử lại.";
                } else {
                  errorContent = streamedGenerationTextRef.current
                    ? streamedGenerationTextRef.current +
                      "\n\n❌ Đã xảy ra lỗi trong quá trình tạo lịch trình."
                    : `❌ Lỗi: ${errStr}`;
                }

                updateBotMsg({
                  isStreaming: false,
                  status: null,
                  content: errorContent,
                  isError: true,
                });
                setIsStreaming(false);
              },
            },
            { autoSave: true },
          );
        } catch (e) {
          console.error("Failed to start generation:", e);
          setIsStreaming(false);
          setGenerationFailed(true);
        }
      },
      [
        setTargetText,
        flush,
        reset,
        fetchTripById,
        setSearchParams,
        setEditableEndDate,
        setEditableStartDate,
        setEditableTitle,
        setIsDrawerOpen,
        setActualTripId,
        setTripData,
        mutateUsage,
      ],
    );

    useEffect(() => {
      if (hasSaved && !isDraining && isStreaming) {
        setIsStreaming(false);
        setChatMessages((prev) => {
          const botMessageId = generationBotMessageIdRef.current;
          if (!botMessageId) return prev;

          return prev.map((msg) =>
            msg.id === botMessageId
              ? { ...msg, isStreaming: false, status: null }
              : msg,
          );
        });
        setHasSaved(false);
      }
    }, [hasSaved, isDraining, isStreaming]);

    useEffect(() => {
      if (!isGeneratingMode || hasStartedGenerating.current) return;

      // ── Task B3: Checkpoint-based recovery ───────────────────────────────────
      // Priority order:
      //  1. status='saved'    + tripId  → redirect immediately (no re-stream)
      //  2. status='streaming'+ payload → reload recovery: check backend, retry
      //  3. status='pending'  + payload → first-visit: start stream from checkpoint
      //  4. No valid checkpoint         → legacy sessionStorage fallback

      const cp = generationCheckpoint.get(userId);

      // Case 1 — Trip was saved before redirect could complete, redirect now
      if (cp?.status === 'saved' && cp?.tripId) {
        generationCheckpoint.clear();
        navigate(`/trips/${cp.tripId}`, { replace: true });
        return;
      }

      // Case 2 — Was actively streaming in a previous session and user reloaded.
      // B2 — Instead of re-generating, show a waiting UI and listen for
      // TripGenerationCompleted via the user-{userId} SignalR group.
      // Lock immediately to prevent React StrictMode double-firing.
      if (cp?.status === 'streaming' && cp?.payload) {
        hasStartedGenerating.current = true;
        generationPayloadRef.current = cp.payload;
        setIsWaitingForBackground(true);
        // Safety timeout: if SignalR doesn't deliver in 2 minutes, show error.
        generationTimeoutRef.current = setTimeout(() => {
          setIsWaitingForBackground(false);
          setGenerationFailed(true);
        }, 120_000);
        return;
      }

      // Case 3 — First visit (status='pending'): payload came from useCreateTripForm
      // which now writes to localStorage checkpoint instead of sessionStorage.
      // Start the stream directly from the checkpoint payload.
      if (cp?.status === 'pending' && cp?.payload) {
        generationCheckpoint.markStreaming(userId);
        generationPayloadRef.current = cp.payload;
        hasStartedGenerating.current = true;
        startGenerateStream(cp.payload);
        return;
      }

      // Case 4 — Legacy fallback: no checkpoint found (e.g. cleared by another tab,
      // or user navigated directly to /trips/new?generating=true without the form).
      // Try sessionStorage for backward compatibility.
      const payloadStr = sessionStorage.getItem("vivu_generating_payload");
      sessionStorage.removeItem("vivu_generating_prompt");
      sessionStorage.removeItem("vivu_generating_payload");

      if (!payloadStr) {
        navigate("/chat", { replace: true });
        return;
      }

      const payload = JSON.parse(payloadStr);
      generationPayloadRef.current = payload;
      hasStartedGenerating.current = true;
      startGenerateStream(payload);
    }, [isGeneratingMode, navigate, startGenerateStream, userId]);

    // Retry handler for failed generation
    const handleRetryGeneration = useCallback(() => {
      const payload = generationPayloadRef.current;
      if (!payload) {
        toast.error(
          "Không tìm thấy dữ liệu lịch trình. Vui lòng tạo lại từ đầu.",
        );
        navigate("/chat", { replace: true });
        return;
      }
      // Reset all error states
      setGenerationFailed(false);
      hasStartedGenerating.current = false;
      setChatMessages([]);
      // Re-trigger generation
      hasStartedGenerating.current = true;
      startGenerateStream(payload);
    }, [navigate, startGenerateStream]);

    const handleBackToChat = useCallback(() => {
      navigate("/chat", { replace: true });
    }, [navigate]);

    // Derived: should we show the dead-end recovery UI?
    const isDeadEnd = id === "new" && generationFailed && !isStreaming;

    useEffect(() => {
      if (isStreaming && displayedText) {
        setChatMessages((prev) => {
          const botMessageId = generationBotMessageIdRef.current;
          if (!botMessageId) return prev;

          return prev.map((msg) =>
            msg.id === botMessageId && msg.isStreaming
              ? { ...msg, content: displayedText }
              : msg,
          );
        });
      }
    }, [displayedText, isStreaming]);

    useEffect(() => {
      messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [chatMessages.length, isStreaming]);

    // Task B3 — Reload keyboard shortcut interceptor
    // Intercepts F5 / Ctrl+R / Cmd+R to show a custom ConfirmationModal instead
    // of the browser's native dialog. The checkpoint system handles recovery, so
    // this is informational — not a hard blocker.
    // Note: Browser reload button and address bar refresh cannot be intercepted
    // (browser security), but checkpoint recovery handles those cases seamlessly.
    const [showReloadModal, setShowReloadModal] = useState(false);

    useEffect(() => {
      if (!isStreaming) return;

      const handler = (e) => {
        const isF5 = e.key === 'F5';
        const isCtrlR = (e.ctrlKey || e.metaKey) && e.key === 'r';

        if (isF5 || isCtrlR) {
          e.preventDefault();
          setShowReloadModal(true);
        }
      };

      window.addEventListener('keydown', handler);
      return () => window.removeEventListener('keydown', handler);
    }, [isStreaming]);

    // Task B3b — In-app navigation blocker
    // Uses react-router useBlocker to intercept SPA navigations and show
    // ConfirmationModal instead of browser's native dialog.
    const blocker = useBlocker(
      ({ currentLocation, nextLocation }) =>
        isStreaming && currentLocation.pathname !== nextLocation.pathname,
    );

    const handleInputChange = (e) => {
      const val = e.target.value;
      messageRef.current = val;

      if (mentionTimeoutRef.current) {
        clearTimeout(mentionTimeoutRef.current);
      }

      mentionTimeoutRef.current = setTimeout(() => {
        if (val.endsWith("@")) {
          setShowMentions(true);
          setMentionQuery("");
        } else if (showMentions) {
          if (!val.includes("@")) {
            setShowMentions(false);
          } else {
            const parts = val.split("@");
            const query = parts[parts.length - 1];
            if (query.includes(" ")) {
              setShowMentions(false);
            } else {
              setMentionQuery(query);
            }
          }
        }
      }, 200);
    };

    const handleSelectMention = (item) => {
      const val = messageRef.current;
      const parts = val.split("@");
      parts.pop();
      const newValue = parts.join("@") + "@" + item.name + " ";
      messageRef.current = newValue;
      if (inputRef.current) inputRef.current.value = newValue;
      setShowMentions(false);
      inputRef.current?.focus();
    };

    const sendToAi = async (userPrompt, skipSaving = false) => {
      if (!id) return;

      const botMessageId = Date.now();
      setChatMessages((prev) => [
        ...prev,
        {
          id: botMessageId,
          role: "assistant",
          content: "",
          isStreaming: true,
          status: "Đang phân tích yêu cầu...",
        },
      ]);

      setIsStreaming(true);
      // Task A3 — Mark this client as the active local SSE streamer.
      // handleAIChunkReceived will ignore SignalR duplicates while this is true.
      isLocallyStreamingRef.current = true;
      let localContent = "";

      const updateBotMessage = (botId, updates) => {
        setChatMessages((prev) =>
          prev.map((msg) => (msg.id === botId ? { ...msg, ...updates } : msg)),
        );
      };

      await aiApi.streamTripChat(
        id,
        userPrompt,
        connectionId,
        {
          onStart: (data) =>
            updateBotMessage(botMessageId, {
              status: data?.message || "Đang khởi động...",
            }),
          onParsing: (data) =>
            updateBotMessage(botMessageId, {
              status: data?.message || "Đang phân tích...",
            }),
          onChunk: (chunk) => {
            localContent += chunk;
            let displayContent = localContent;
            if (localContent.trim().startsWith("{")) {
              const summaryMatch = localContent.match(
                /"summary"\s*:\s*"((?:[^"\\]|\\.)*)/,
              );
              if (summaryMatch) {
                displayContent = summaryMatch[1]
                  .replace(/\\n/g, "\n")
                  .replace(/\\"/g, '"');
              } else {
                displayContent = "Đang phân tích thay đổi...";
              }
            }
            updateBotMessage(botMessageId, {
              status: "Đang xử lý...",
              content: displayContent,
            });
          },
          onComplete: (data) => {
            if (data?.message) {
              // Task A3 — Local SSE stream is done; allow SignalR chunks through again
              isLocallyStreamingRef.current = false;
              setIsStreaming(false);
              updateBotMessage(botMessageId, {
                isStreaming: false,
                status: null,
                content: data.message,
              });
              mutateUsage();
            } else {
              updateBotMessage(botMessageId, {
                status: "Đang lưu thay đổi...",
              });
            }
          },
          onSaving: (data) =>
            updateBotMessage(botMessageId, {
              status: data?.message || "Đang lưu thay đổi...",
            }),
          onSaved: () => {
            // Task A3 — Local stream complete
            isLocallyStreamingRef.current = false;
            setIsStreaming(false);
            let finalContent = "Đã cập nhật lịch trình thành công!";
            const summaryMatch = localContent.match(
              /"summary"\s*:\s*"((?:[^"\\]|\\.)*)/,
            );
            if (summaryMatch) {
              finalContent = summaryMatch[1]
                .replace(/\\n/g, "\n")
                .replace(/\\"/g, '"');
            } else {
              try {
                const parsed = JSON.parse(localContent);
                if (parsed && parsed.summary) {
                  finalContent = parsed.summary;
                }
              } catch {
                if (!localContent.trim().startsWith("{"))
                  finalContent = localContent;
              }
            }
            updateBotMessage(botMessageId, {
              isStreaming: false,
              status: null,
              content: finalContent,
            });
            toast.success("Đã thay đổi lịch trình thành công!");
            fetchTrip(false);
            mutateUsage();
          },
          onError: (err) => {
            // Task A3 — Release local streaming lock on error
            isLocallyStreamingRef.current = false;
            setIsStreaming(false);
            let content =
              "Xin lỗi, mình không thể thực hiện thay đổi lúc này. Vui lòng thử lại.";
            if (
              err === "AI.RateLimitExceeded" ||
              (err &&
                typeof err === "string" &&
                err.includes("RateLimitExceeded"))
            ) {
              content =
                "Bạn đã hết lượt sử dụng AI. Vui lòng nâng cấp gói để tiếp tục trải nghiệm.";
              mutateUsage();
            }
            updateBotMessage(botMessageId, {
              isStreaming: false,
              status: null,
              content: content,
              error: err,
            });
          },
        },
        { skipSavingUserMessage: skipSaving },
      );
    };

    const handleSendMessage = async (e) => {
      e.preventDefault();
      const currentVal = messageRef.current || "";
      if (
        (!currentVal.trim() && !selectedImage) ||
        isStreaming ||
        isUploadingImage
      )
        return;

      if (chatMode === "ai" && isAiExhausted) {
        toast.warning(
          "Bạn đã hết lượt sử dụng AI hôm nay. Vui lòng nâng cấp gói để tiếp tục.",
        );
        return;
      }

      if (chatMode === "ai") {
        setChatMessages((prev) => [
          ...prev,
          { role: "user", content: currentVal, id: `user-msg-${Date.now()}` },
        ]);
        messageRef.current = "";
        if (inputRef.current) inputRef.current.value = "";
        await sendToAi(currentVal);
      } else {
        if (selectedImage) {
          try {
            setIsUploadingImage(true);
            const formData = new FormData();
            formData.append("image", selectedImage);
            if (currentVal.trim())
              formData.append("content", currentVal.trim());

            await tripApi.sendChatImage(id, formData);

            setSelectedImage(null);
            if (fileInputRef.current) fileInputRef.current.value = "";
            messageRef.current = "";
            if (inputRef.current) inputRef.current.value = "";
          } catch (e) {
            toast.error("Không thể gửi ảnh. Vui lòng thử lại.");
            console.error(e);
          } finally {
            setIsUploadingImage(false);
          }
          return;
        }

        messageRef.current = "";
        if (inputRef.current) inputRef.current.value = "";

        if (isConnected) {
          try {
            await sendSignalRMessage(currentVal, "text", null);
          } catch (error) {
            toast.error("Không thể gửi tin nhắn. Vui lòng thử lại.");
            console.error(error);
            return;
          }
        } else {
          toast.error(
            "Đang kết nối tới máy chủ chat hoặc mạng kém. Vui lòng chờ...",
          );
          return;
        }

        const isAiMentioned = /@vivuai/i.test(currentVal);
        if (isAiMentioned) {
          const aiPrompt =
            currentVal.replace(/@vivuai/gi, "").trim() ||
            "Chào bạn, mình có thể giúp gì cho chuyến đi này?";
          setTimeout(() => {
            sendToAi(aiPrompt, true);
          }, 500);
        }
      }
    };

    return (
      <>
      <div className="w-full h-full md:w-1/2 flex flex-col bg-slate-50/50 relative overflow-hidden">
        <div
          className="absolute inset-0 opacity-[0.03] pointer-events-none"
          style={{
            backgroundImage:
              "radial-gradient(circle at 1px 1px, #64748b 1px, transparent 0)",
            backgroundSize: "24px 24px",
          }}
        ></div>

        {/* B4 — Generation Waiting Screen: shown after reload while AI is still working */}
        {isGeneratingMode && isWaitingForBackground && (
          <div className="flex-1 flex flex-col items-center justify-center p-6 text-center space-y-10 pb-20 animate-in fade-in duration-500">
            <div className="space-y-6 max-w-lg">
              <div className="space-y-3">
                <h1 className="text-2xl md:text-4xl font-extrabold text-transparent bg-clip-text bg-linear-to-r from-gray-900 to-gray-700 tracking-tight leading-tight">
                  Đang hoàn thiện
                  <br /> lịch trình của bạn
                </h1>
                <p className="text-gray-500 text-sm md:text-base font-medium">
                  Hệ thống vẫn đang chạy ngầm. Lịch trình sẽ
                  <br /> tự động hiển thị khi hoàn tất.
                </p>
              </div>

              <div className="inline-flex items-center gap-3 px-4 py-2 bg-blue-50 text-blue-600 rounded-full text-sm font-medium">
                <Loader2 className="w-4 h-4 animate-spin" />
                <span>Quá trình này thường mất 30–60 giây...</span>
              </div>

              <div className="pt-8">
                <button
                  onClick={() => {
                    clearTimeout(generationTimeoutRef.current);
                    generationCheckpoint.clear();
                    navigate("/chat", { replace: true });
                  }}
                  className="text-gray-400 hover:text-gray-600 font-medium text-sm transition-colors border-b border-transparent hover:border-gray-400 pb-0.5"
                >
                  Hủy và về trang chính
                </button>
              </div>
            </div>
          </div>
        )}

        {/* B4 — Generation Completed Screen: brief confirmation before redirect */}
        {isGeneratingMode && generationJustCompleted && (
          <div className="flex-1 flex flex-col items-center justify-center p-6 text-center space-y-10 pb-20 animate-in fade-in zoom-in-95 duration-300">
            <div className="space-y-6 max-w-lg">
              <div className="space-y-3">
                <h1 className="text-2xl md:text-4xl font-extrabold text-transparent bg-clip-text bg-linear-to-r from-gray-900 to-gray-700 tracking-tight leading-tight">
                  Lịch trình đã sẵn sàng!
                </h1>
                <p className="text-gray-500 text-sm md:text-base font-medium">
                  Đang chuyển hướng đến chuyến đi của bạn...
                </p>
              </div>

              <div className="max-w-[200px] mx-auto h-1.5 bg-gray-100 rounded-full overflow-hidden mt-8">
                <div className="h-full bg-gradient-to-r from-emerald-400 to-green-500 rounded-full animate-[grow_1.5s_ease-out_forwards]" style={{ width: "0%" }}>
                  <style>{`@keyframes grow { to { width: 100%; } }`}</style>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Normal chat content — hidden when waiting/completed screens are active */}
        {(!isGeneratingMode || (!isWaitingForBackground && !generationJustCompleted)) && (<>
        <div className="flex-1 overflow-y-auto p-4 md:p-6 pb-60 md:pb-52 relative scroll-smooth no-scrollbar">
          {chatMode === null ? (
            <div className="h-full flex items-center justify-center">
              <Loader2 className="w-8 h-8 text-blue-500 animate-spin" />
            </div>
          ) : chatMessages.length > 0 || isStreaming ? (
            <div className="max-w-2xl mx-auto space-y-4 pb-4">
              {chatMessages.map((msg) => (
                <MessageBubble
                  key={msg.id}
                  msg={msg}
                  currentUser={currentUser}
                  tripData={tripData}
                  actualTripId={actualTripId || id}
                />
              ))}
              <div ref={messagesEndRef} />
            </div>
          ) : (
            <div className="h-full flex flex-col items-center justify-center text-center space-y-10 pb-20">
              <div className="space-y-2 max-w-lg">
                <h1 className="text-2xl md:text-4xl font-extrabold text-transparent bg-clip-text bg-linear-to-r from-gray-900 to-gray-700 tracking-tight leading-tight">
                  Bạn muốn lên kế hoạch
                  <br /> đi đâu hôm nay?
                </h1>
                <p className="text-gray-500 text-sm md:text-base font-medium">
                  Vivu AI sẵn sàng hỗ trợ bạn tìm địa điểm, lên lịch trình và
                  nhiều hơn nữa.
                </p>
              </div>
            </div>
          )}
        </div>

        <div className="absolute bottom-4 pb-14 md:bottom-6 md:pb-0 left-0 right-0 px-4 md:px-8 z-20 pointer-events-none">
          <div className="max-w-3xl mx-auto pointer-events-auto">
            {/* Error Recovery Banner — shown when generation failed on /trips/new */}
            {isDeadEnd ? (
              <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
                <div className="bg-white/90 backdrop-blur-2xl rounded-2xl border border-rose-200/60 shadow-[0_8px_30px_rgb(0,0,0,0.10)] p-5 ring-1 ring-rose-100">
                  <div className="flex items-start gap-3 mb-4">
                    <div className="w-10 h-10 rounded-full bg-rose-100 flex items-center justify-center shrink-0">
                      <AlertTriangle size={20} className="text-rose-500" />
                    </div>
                    <div>
                      <h3 className="text-sm font-bold text-gray-900">
                        Không thể tạo lịch trình
                      </h3>
                      <p className="text-xs text-gray-500 mt-0.5">
                        Đã xảy ra lỗi trong quá trình tạo. Bạn có thể thử lại
                        hoặc quay về trang chính.
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <button
                      onClick={handleRetryGeneration}
                      className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-gradient-primary text-white text-sm font-bold rounded-xl shadow-md shadow-blue-200/50 hover:shadow-lg hover:scale-[1.02] active:scale-[0.98] transition-all duration-200"
                    >
                      <RotateCcw size={16} />
                      Thử lại
                    </button>
                    <button
                      onClick={handleBackToChat}
                      className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-white text-gray-700 text-sm font-bold rounded-xl border border-gray-200 shadow-sm hover:bg-gray-50 hover:border-gray-300 hover:shadow-md active:scale-[0.98] transition-all duration-200"
                    >
                      <Home size={16} />
                      Về trang chính
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              /* Normal chat input area */
              <>
                {chatMode === "ai" && (
                  <div className="mb-3 px-2 w-full">
                    <AIUsageIndicator usage={usage} />
                  </div>
                )}
                {selectedImage && (
                  <div className="mb-3 px-4 w-full flex items-center">
                    <div className="relative inline-block">
                      <img
                        src={URL.createObjectURL(selectedImage)}
                        alt="Preview"
                        className="h-20 w-20 object-cover rounded-xl border-2 border-white shadow-md"
                      />
                      <button
                        type="button"
                        onClick={() => {
                          setSelectedImage(null);
                          if (fileInputRef.current)
                            fileInputRef.current.value = "";
                        }}
                        className="absolute -top-2 -right-2 bg-rose-500 text-white rounded-full p-1 shadow-sm hover:bg-rose-600 transition-colors"
                      >
                        <X size={14} strokeWidth={3} />
                      </button>
                    </div>
                  </div>
                )}
                <div className="relative group">
                  <div className="absolute -inset-2 bg-gradient-primary rounded-3xl blur-md opacity-20 group-hover:opacity-30 transition duration-700"></div>
                  <form
                    onSubmit={handleSendMessage}
                    className="relative flex items-center gap-2 bg-white/80 backdrop-blur-2xl rounded-3xl border border-white/50 shadow-[0_8px_30px_rgb(0,0,0,0.12)] p-2 pl-5 hover:shadow-[0_12px_40px_rgb(0,0,0,0.16)] transition-all duration-300 ring-1 ring-black/5"
                  >
                    <input
                      ref={inputRef}
                      type="text"
                      defaultValue=""
                      onChange={handleInputChange}
                      disabled={
                        (chatMode === "ai" && isAiExhausted) ||
                        (id === "new" && isStreaming)
                      }
                      placeholder={
                        id === "new" && isStreaming
                          ? "Đang tạo lịch trình..."
                          : chatMode === "group"
                            ? "Nhập tin nhắn... (@ để nhắc tên)"
                            : chatMode === "ai" && isAiExhausted
                              ? "Đã hết lượt AI hôm nay"
                              : "Lên kế hoạch chuyến đi với Vivu AI..."
                      }
                      className="flex-1 bg-transparent border-none outline-none text-gray-800 placeholder-gray-400 font-medium py-3 text-[15px] disabled:opacity-60 disabled:cursor-not-allowed"
                    />

                    {chatMode === "group" && (
                      <div className="shrink-0">
                        <input
                          type="file"
                          ref={fileInputRef}
                          className="hidden"
                          accept="image/jpeg,image/png,image/webp,image/jpg"
                          onChange={(e) => {
                            const file = e.target.files[0];
                            if (file) setSelectedImage(file);
                          }}
                        />
                        <button
                          type="button"
                          onClick={() => fileInputRef.current?.click()}
                          className="p-2 text-slate-400 hover:text-blue-500 hover:bg-blue-50 rounded-full transition-colors relative group"
                        >
                          <ImagePlus size={20} />
                          <span className="absolute -top-8 left-1/2 -translate-x-1/2 text-[10px] bg-slate-800 text-white px-2 py-1 rounded opacity-0 group-hover:opacity-100 whitespace-nowrap transition-opacity pointer-events-none">
                            Gửi ảnh
                          </span>
                        </button>
                      </div>
                    )}

                    <MentionSuggestions
                      visible={showMentions}
                      filterText={mentionQuery}
                      members={tripMembers}
                      onSelect={handleSelectMention}
                      onClose={() => setShowMentions(false)}
                    />

                    <div className="flex items-center gap-1 md:gap-2 pr-1 shrink-0">
                      <button
                        type="submit"
                        disabled={
                          isStreaming || isUploadingImage || id === "new"
                        }
                        className="w-10 h-10 flex items-center justify-center bg-gradient-primary text-white rounded-full shadow-md shadow-blue-500/30 disabled:opacity-50 disabled:shadow-none hover:shadow-lg hover:scale-105 active:scale-95 transition-all duration-300 relative overflow-hidden shrink-0"
                      >
                        <div className="absolute inset-0 bg-white/20 opacity-0 hover:opacity-100 transition-opacity"></div>
                        {isUploadingImage ? (
                          <Loader2 size={18} className="animate-spin" />
                        ) : (
                          <Send
                            size={18}
                            fill="currentColor"
                            strokeWidth={2}
                            className="transition-transform duration-300 transform active:scale-90"
                          />
                        )}
                      </button>
                    </div>
                  </form>
                </div>
                <div className="text-center pt-3 pb-1">
                  <p className="text-[10px] text-gray-500 font-semibold tracking-wide drop-shadow-sm">
                    Vivu AI có thể cung cấp thông tin không chính xác • Enter để
                    gửi
                  </p>
                </div>
              </>
            )}
          </div>
        </div>
        </>
        )}
      </div>

        {/* B3b — Navigation blocker modal */}
        <ConfirmationModal
          isOpen={blocker.state === "blocked"}
          onClose={() => blocker.reset?.()}
          onConfirm={() => blocker.proceed?.()}
          title="AI đang tạo lịch trình"
          message="Vivu AI đang tạo lịch trình cho bạn. Nếu rời đi, quá trình vẫn tiếp tục ở chế độ nền và bạn có thể theo dõi trên thanh thông báo."
          confirmText="Rời đi"
          cancelText="Ở lại"
          isDanger={false}
        />

        {/* B3 — Reload interceptor modal (F5 / Ctrl+R / Cmd+R) */}
        <ConfirmationModal
          isOpen={showReloadModal}
          onClose={() => setShowReloadModal(false)}
          onConfirm={() => window.location.reload()}
          title="AI đang tạo lịch trình"
          message="Vivu AI đang tạo lịch trình cho bạn. Nếu tải lại trang, quá trình vẫn tiếp tục ở chế độ nền và sẽ tự động cập nhật khi hoàn tất."
          confirmText="Tải lại trang"
          cancelText="Ở lại"
          isDanger={false}
        />
      </>
    );
  },
);

export default React.memo(TripChatPanel);
