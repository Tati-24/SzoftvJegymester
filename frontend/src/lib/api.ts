const BASE = 'http://localhost:5244';



let token: string | null =

  typeof localStorage !== 'undefined' ? localStorage.getItem('token') : null;



export function setToken(t: string | null) {

  token = t;

  if (typeof localStorage === 'undefined') return;

  t ? localStorage.setItem('token', t) : localStorage.removeItem('token');

}



async function req<T>(path: string, opts: RequestInit = {}): Promise<T> {

  const headers = new Headers(opts.headers);

  headers.set('Content-Type', 'application/json');

  if (token) headers.set('Authorization', `Bearer ${token}`);



  let res: Response;

  try {

    res = await fetch(`${BASE}${path}`, { ...opts, headers });

  } catch (e) {

    throw new Error(

      'A szerver nem elérhető.'

    );

  }



  if (!res.ok) {

    const msg = await res.text();

    throw new Error(msg || `Hiba: ${res.status}`);

  }



  return res.json() as Promise<T>;

}



export async function login(email: string, password: string) {

  const out = await req<{ token: string; expiresAt: string }>('/login', {

    method: 'POST',

    body: JSON.stringify({ email, password })

  });

  setToken(out.token);

  return out;

}



export async function register(data: {

  name: string;

  email: string;

  password: string;

  phoneNumber?: string | null;

}) {

  const out = await req<{ token: string; expiresAt: string }>('/register', {

    method: 'POST',

    body: JSON.stringify({

      name: data.name,

      email: data.email,

      password: data.password,

      phoneNumber: data.phoneNumber ?? null

    })

  });

  setToken(out.token);

  return out;

}



export async function getTestNames(): Promise<string[]> {

  return req<string[]>('/');

}