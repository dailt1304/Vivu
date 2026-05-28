import { useState, useEffect, useCallback, useRef } from "react";
import { HubConnectionState } from "@microsoft/signalr";
import { createTripChatConnection } from "../../services/signalr";

export const useTripChat = (
  tripId,
  accessToken,
  {
    onMessageReceived,
    onAIProcessing,
    onAIChunk,
    onAIMessage,
    onTripUpdated,
    onMemberListChanged,
    onMemberKicked,
    onAIError,
    onMessageDeleted,
    // B1 — User-scoped generation events (received even without a tripId)
    onTripGenerationCompleted,
    onTripGenerationFailed,
  } = {},
) => {
  const connectionRef = useRef(null);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionId, setConnectionId] = useState(null);

  // Vercel Rule: advanced-event-handler-refs
  // Use a ref to store the latest callbacks so the effect doesn't need to re-run
  // when callback identities change (which happens on almost every render in the parent)
  const callbacksRef = useRef({
    onMessageReceived,
    onAIProcessing,
    onAIChunk,
    onAIMessage,
    onTripUpdated,
    onMemberListChanged,
    onMemberKicked,
    onAIError,
    onMessageDeleted,
    onTripGenerationCompleted,
    onTripGenerationFailed,
  });

  // Keep the ref in sync with latest props
  useEffect(() => {
    callbacksRef.current = {
      onMessageReceived,
      onAIProcessing,
      onAIChunk,
      onAIMessage,
      onTripUpdated,
      onMemberListChanged,
      onMemberKicked,
      onAIError,
      onMessageDeleted,
      onTripGenerationCompleted,
      onTripGenerationFailed,
    };
  }, [
    onMessageReceived,
    onAIProcessing,
    onAIChunk,
    onAIMessage,
    onTripUpdated,
    onMemberListChanged,
    onMemberKicked,
    onAIError,
    onMessageDeleted,
    onTripGenerationCompleted,
    onTripGenerationFailed,
  ]);

  useEffect(() => {
    // B1 — Allow connecting without a tripId so the generation waiting page
    // can receive user-scoped events (TripGenerationCompleted/Failed) from
    // the personal user-{userId} group joined in TripChatHub.OnConnectedAsync.
    if (!accessToken) return;

    let isMounted = true;
    const newConnection = createTripChatConnection(accessToken);

    // C8 — Reconnection handlers (must be registered BEFORE .start())
    // When network drops, update UI state immediately so components can
    // show a "reconnecting" indicator.
    newConnection.onreconnecting(() => {
      if (!isMounted) return;
      setIsConnected(false);
    });

    // When reconnected, re-join the trip room (new connectionId = not in
    // any room) and restore UI state.
    newConnection.onreconnected(() => {
      if (!isMounted) return;
      setIsConnected(true);
      setConnectionId(newConnection.connectionId);
      if (tripId) {
        newConnection
          .invoke("JoinTrip", tripId)
          .catch((err) => console.error("Re-join after reconnect failed:", err));
      }
    });

    // When all reconnect attempts exhausted, clean up fully.
    newConnection.onclose(() => {
      if (!isMounted) return;
      setIsConnected(false);
      setConnectionId(null);
      connectionRef.current = null;
    });

    newConnection
      .start()
      .then(() => {
        if (!isMounted) return;
        setIsConnected(true);
        setConnectionId(newConnection.connectionId);
        connectionRef.current = newConnection;

        // B1 — Only join the trip room if a tripId is provided.
        // When tripId is null (generation waiting page), we still connect
        // to receive user-scoped SignalR events from the personal group.
        if (tripId) {
          newConnection
            .invoke("JoinTrip", tripId)
            .catch((err) => console.error(err));
        }

        newConnection.on("ReceiveMessage", (message) => {
          if (callbacksRef.current.onMessageReceived) {
            callbacksRef.current.onMessageReceived(message);
          }
        });

        // AI started processing
        newConnection.on("AIProcessing", (payload) => {
          if (callbacksRef.current.onAIProcessing)
            callbacksRef.current.onAIProcessing(payload);
        });

        // AI Streaming chunk received
        newConnection.on("ReceiveAIChunk", (chunk) => {
          if (callbacksRef.current.onAIChunk)
            callbacksRef.current.onAIChunk(chunk);
        });

        // AI message complete
        newConnection.on("ReceiveAIMessage", (payload) => {
          if (callbacksRef.current.onAIMessage)
            callbacksRef.current.onAIMessage(payload);
        });

        // Trip was updated (role change, field edit, etc.)
        newConnection.on("TripUpdated", (payload) => {
          if (callbacksRef.current.onTripUpdated)
            callbacksRef.current.onTripUpdated(payload);
        });

        // Member list changed (join/leave/remove) — server-side broadcast
        newConnection.on("MemberListChanged", (payload) => {
          if (callbacksRef.current.onMemberListChanged)
            callbacksRef.current.onMemberListChanged(payload);
        });

        // Current user was kicked from trip by owner
        newConnection.on("MemberKicked", (payload) => {
          if (callbacksRef.current.onMemberKicked)
            callbacksRef.current.onMemberKicked(payload);
        });

        // AI encountered an error
        newConnection.on("AIError", (payload) => {
          if (callbacksRef.current.onAIError)
            callbacksRef.current.onAIError(payload);
        });

        // Chat message deleted
        newConnection.on("MessageDeleted", (payload) => {
          if (callbacksRef.current.onMessageDeleted)
            callbacksRef.current.onMessageDeleted(payload);
        });

        // B1 — User-scoped generation events (user-{userId} group)
        newConnection.on("TripGenerationCompleted", (payload) => {
          if (callbacksRef.current.onTripGenerationCompleted)
            callbacksRef.current.onTripGenerationCompleted(payload);
        });

        newConnection.on("TripGenerationFailed", (payload) => {
          if (callbacksRef.current.onTripGenerationFailed)
            callbacksRef.current.onTripGenerationFailed(payload);
        });
      })
      .catch((e) => console.log("Connection failed: ", e));

    return () => {
      isMounted = false;
      if (tripId && newConnection.state === HubConnectionState.Connected) {
        newConnection
          .invoke("LeaveTrip", tripId)
          .catch((err) => console.error(err));
      }
      newConnection.off("ReceiveMessage");
      newConnection.off("AIProcessing");
      newConnection.off("ReceiveAIChunk");
      newConnection.off("ReceiveAIMessage");
      newConnection.off("TripUpdated");
      newConnection.off("MemberListChanged");
      newConnection.off("MemberKicked");
      newConnection.off("AIError");
      newConnection.off("MessageDeleted");
      newConnection.off("TripGenerationCompleted");
      newConnection.off("TripGenerationFailed");
      newConnection.stop();
      setIsConnected(false);
      setConnectionId(null);
      connectionRef.current = null;
    };
  }, [tripId, accessToken]);

  const sendMessage = useCallback(
    async (content, messageType = "text", replyToId = null) => {
      const connection = connectionRef.current;
      if (
        connection &&
        isConnected &&
        connection.state === HubConnectionState.Connected
      ) {
        try {
          await connection.invoke("SendMessage", {
            tripId,
            content,
            messageType,
            replyToId,
          });
        } catch (e) {
          console.error("Send message failed: ", e);
          throw e;
        }
      } else {
        console.warn("No connection to server yet.");
        throw new Error("Cannot send data. Connection is not active.");
      }
    },
    [isConnected, tripId],
  );

  const broadcastTripEdit = useCallback(
    async (data) => {
      const connection = connectionRef.current;
      if (
        connection &&
        isConnected &&
        connection.state === HubConnectionState.Connected
      ) {
        try {
          await connection.invoke("BroadcastTripEdit", {
            tripId,
            ...data,
          });
        } catch (e) {
          console.error("Broadcast failed:", e);
        }
      }
    },
    [isConnected, tripId],
  );

  return { sendMessage, broadcastTripEdit, isConnected, connectionId };
};
