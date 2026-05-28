/**
 * Safely decode a JWT without external libraries (Vercel rule: bundle-conditional)
 * and extract the user roles from it.
 * @param {string} token - The JWT access token
 * @returns {string[]} An array of roles
 */
export const getRolesFromToken = (token) => {
  if (!token || typeof token !== "string") return [];

  try {
    const parts = token.split(".");
    if (parts.length !== 3) return [];

    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    // Decode base64, handling UTF-8 characters properly
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join("")
    );

    const payloadObj = JSON.parse(jsonPayload);

    // Xử lý các claim key role khác nhau (.NET mặc định dùng http schema)
    const roleClaimKey = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    
    let roles = payloadObj[roleClaimKey] || payloadObj.role || [];

    // Nếu chỉ có 1 role, nó có thể trả về string thay vì mảng
    if (typeof roles === "string") {
      roles = [roles];
    }

    return Array.isArray(roles) ? roles : [];
  } catch (error) {
    console.error("Failed to decode token:", error);
    return [];
  }
};
