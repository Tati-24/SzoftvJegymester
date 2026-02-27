const BASE_URL = 'http://localhost:5244';

let token: string | null =
  typeof localStorage !== 'undefined' ? localStorage.getItem('token') : null;

export function setToken(newToken: string | null) {
  token = newToken;
  if (typeof localStorage === 'undefined') return;
  if (newToken) {
    localStorage.setItem('token', newToken);
  } else {
    localStorage.removeItem('token');
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);

  headers.set('Content-Type', 'application/json');
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `Hiba: ${res.status}`);
  }

  return (await res.json()) as T;
}

export async function login(email: string, password: string) {
  const data = await request<{ token: string; expiresAt: string }>('/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });
  setToken(data.token);
  return data;
}

export async function getTestNames() {
  return await request<string[]>('/');
}