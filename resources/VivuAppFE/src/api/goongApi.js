const GOONG_API_KEY = import.meta.env.VITE_GOONG_CLIENT_TOKEN;
const BASE = "https://rsapi.goong.io";

const fetchWithRetry = async (url, maxRetries = 3) => {
  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    const res = await fetch(url);
    if (res.ok) return res;

    if (res.status === 429 && attempt < maxRetries) {
      const delay = Math.pow(2, attempt) * 1000;
      await new Promise((r) => setTimeout(r, delay));
      continue;
    }

    throw new Error(`Goong API error: ${res.status}`);
  }
};

const goongApi = {
  autocomplete: async (input, sessionToken) => {
    if (!input) return { predictions: [] };

    let url = `${BASE}/Place/AutoComplete?input=${encodeURIComponent(input)}&api_key=${GOONG_API_KEY}&limit=5`;
    if (sessionToken) {
      url += `&sessiontoken=${sessionToken}`;
    }

    const res = await fetch(url);
    if (!res.ok) throw new Error("Failed to fetch autocomplete data");
    return res.json();
  },

  placeDetail: async (placeId, sessionToken) => {
    if (!placeId) return null;

    let url = `${BASE}/Place/Detail?place_id=${encodeURIComponent(placeId)}&api_key=${GOONG_API_KEY}`;
    if (sessionToken) {
      url += `&sessiontoken=${sessionToken}`;
    }

    const res = await fetch(url);
    if (!res.ok) throw new Error("Failed to fetch place detail");
    return res.json();
  },

  direction: async (origin, destination, vehicle = "car") => {
    if (!origin || !destination) return null;

    const url = `${BASE}/Direction?origin=${origin}&destination=${destination}&vehicle=${vehicle}&api_key=${GOONG_API_KEY}`;
    const res = await fetchWithRetry(url);
    return res.json();
  },
};

export default goongApi;
