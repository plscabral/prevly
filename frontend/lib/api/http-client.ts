const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5085";
const API_TOKEN = process.env.NEXT_PUBLIC_API_TOKEN;
const TOKEN_STORAGE_KEY = "prevly_token";
const BETTER_AUTH_BASE_URL =
  process.env.NEXT_PUBLIC_BETTER_AUTH_URL ?? "http://localhost:3000/api/auth";

const getSessionTokenFromPayload = (payload: unknown): string | undefined => {
  if (!payload || typeof payload !== "object") return undefined;

  const typedPayload = payload as {
    user?: { token?: string };
    data?: { user?: { token?: string } };
  };

  return typedPayload.user?.token ?? typedPayload.data?.user?.token;
};

const getToken = async () => {
  if (typeof window === "undefined") return API_TOKEN;

  const localToken = window.localStorage.getItem(TOKEN_STORAGE_KEY);
  if (localToken) return localToken;

  try {
    const sessionResponse = await fetch(`${BETTER_AUTH_BASE_URL}/get-session`, {
      credentials: "include",
    });

    if (!sessionResponse.ok) return API_TOKEN;

    const sessionPayload = await sessionResponse.json();
    const sessionToken = getSessionTokenFromPayload(sessionPayload);

    if (sessionToken) {
      window.localStorage.setItem(TOKEN_STORAGE_KEY, sessionToken);
      return sessionToken;
    }
  } catch {
    // ignore session token hydration errors and fallback to env token
  }

  return API_TOKEN;
};

const withBaseUrl = (url: string) => new URL(url, API_BASE_URL).toString();

const extractErrorMessage = (data: unknown, status: number) => {
  if (typeof data === "string" && data.trim()) return data;
  if (typeof data === "object" && data !== null && "detail" in data) {
    const detail = (data as { detail?: unknown }).detail;
    if (typeof detail === "string" && detail.trim()) return detail;
  }
  return `Erro na requisicao: ${status}`;
};

export async function customFetch<TResponse>(
  url: string,
  init?: RequestInit,
): Promise<TResponse> {
  const token = await getToken();
  const request = await fetch(withBaseUrl(url), {
    ...init,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {}),
    },
  });

  const responseText = await request.text();
  const parsedData = responseText ? JSON.parse(responseText) : undefined;
  const wrappedResponse = {
    data: parsedData,
    status: request.status,
    headers: request.headers,
  } as TResponse;

  if (!request.ok) {
    throw new Error(extractErrorMessage(parsedData, request.status));
  }

  return wrappedResponse;
}
