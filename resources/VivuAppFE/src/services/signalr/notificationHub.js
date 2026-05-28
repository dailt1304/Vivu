import * as signalR from "@microsoft/signalr";

/**
 * Creates and configures a SignalR connection for the Notification Hub.
 *
 * @param {string} accessToken - The JWT access token for authentication.
 * @returns {signalR.HubConnection} - The configured SignalR connection instance.
 */
export const createNotificationConnection = (accessToken) => {
  const apiUrl = import.meta.env.VITE_API_URL || "https://localhost:7294/api";
  const baseUrl = apiUrl.replace(/\/api\/?$/, "");

  return new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/notifications`, {
      accessTokenFactory: () => localStorage.getItem("access_token"),
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Aggressive reconnect for notifications
    .build();
};
