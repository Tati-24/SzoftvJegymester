const BASE = 'http://localhost:5244';



let token: string | null =

  typeof localStorage !== 'undefined' ? localStorage.getItem('token') : null;



export function setToken(t: string | null) {

  token = t;

  if (typeof localStorage === 'undefined') return;

  t ? localStorage.setItem('token', t) : localStorage.removeItem('token');

}

export function isAuthenticated(): boolean {
  if (!token) return false;
  const payload = getJwtPayload();
  if (!payload) return false;
  const exp = payload.exp;
  if (typeof exp === 'number' && Number.isFinite(exp)) {
    return Date.now() / 1000 < exp - 15;
  }
  return true;
}

function getJwtPayload(): Record<string, unknown> | null {
  if (!token) return null;
  const parts = token.split('.');
  if (parts.length < 2) return null;

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = atob(base64);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function getUserRole(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const role =
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
    payload.role ??
    payload.Role;

  return typeof role === 'string' ? role : null;
}

export function isAdmin(): boolean {
  return getUserRole() === 'ADMIN';
}

export function getUserEmail(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const email =
    payload.email ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];

  return typeof email === 'string' ? email : null;
}

export function getUserName(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const name =
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
    payload.name ??
    payload.unique_name;

  return typeof name === 'string' && name.trim() ? name.trim() : null;
}

export function getUserId(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const id = payload.sub;
  return typeof id === 'string' ? id : null;
}



function humanizeApiError(body: string): string {

  const t = body.trim().replace(/^["']+|["']+$/g, '');

  const map: Record<string, string> = {
    'For not registered users giving phone number and email is mandatory.':
      'A szerver nem fogadja el a belépésed (néha ez lejárt token). Jelentkezz be újra — vagy töltsd ki az adatokat vendéghez a vetítésnél kosár előtt.',
    'For guest purchases name, phone number and email are mandatory.':
      'Vendég vásárlásnál kötelező a név, az e-mail és a telefonszám minden jegynél.',
    'Ticket already purchased.': 'Erről a vetítésről erre a székre már van eladott jegy.',
    'Invalid User identification.': 'Érvénytelen felhasználó-azonosítás. Jelentkezz be újra.',
    'User does not own that ticket.': 'Ez a jegy nem a fiókodhoz tartozik.',
    'Invalid ticket id': 'Érvénytelen jegyazonosító.'
  };

  return map[t] ?? body;

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

    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);

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

export type Film = {
  id: string;
  title: string;
  description: string;
  length: number;
  ageRating: string;
  releaseDate: string;
  genre: string;
  director: string;
  isActive: boolean;
};

/** Megjelenési dátum szerint csökkenő, majd cím — így a főoldali 10-es szeletben is előkerülnek a frissebb filmek. */
export function sortFilmsForDisplay(films: Film[]): Film[] {
  return [...films].sort((a, b) => {
    const ta = new Date(a.releaseDate).getTime();
    const tb = new Date(b.releaseDate).getTime();
    const da = Number.isFinite(ta) ? ta : 0;
    const db = Number.isFinite(tb) ? tb : 0;
    if (db !== da) return db - da;
    return a.title.localeCompare(b.title, 'hu');
  });
}

export async function getFilms(): Promise<Film[]> {
  return req<Film[]>('/films');
}

export async function getFilm(id: string): Promise<Film> {
  return req<Film>(`/films/${id}`);
}

export type FilmCreateInput = {
  title: string;
  description: string;
  length: number;
  ageRating?: string | null;
  releaseDate: string;
  genre?: string | null;
  director?: string | null;
  isActive?: boolean;
};

export async function createFilm(data: FilmCreateInput): Promise<Film> {
  return req<Film>('/films/admin/upload', {
    method: 'POST',
    body: JSON.stringify({
      title: data.title,
      description: data.description,
      length: data.length,
      ageRating: data.ageRating ?? null,
      releaseDate: data.releaseDate,
      genre: data.genre ?? null,
      director: data.director ?? null,
      isActive: data.isActive ?? true
    })
  });
}

export type FilmUpdateInput = {
  title: string;
  description: string;
  length: number;
  ageRating: string | null;
  releaseDate: string;
  genre: string | null;
  director: string | null;
  isActive: boolean;
};

export async function updateFilm(id: string, data: FilmUpdateInput): Promise<Film> {
  return req<Film>(`/films/admin/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      title: data.title,
      description: data.description,
      length: data.length,
      ageRating: data.ageRating,
      releaseDate: data.releaseDate,
      genre: data.genre,
      director: data.director,
      isActive: data.isActive
    })
  });
}

export async function deleteFilm(id: string): Promise<void> {
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(`${BASE}/films/admin/${id}`, {
      method: 'DELETE',
      headers
    });
  } catch {
    throw new Error('A szerver nem elérhető.');
  }

  if (!res.ok) {
    const msg = await res.text();
    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);
  }
}

export type Screening = {
  id: string;
  filmId: string;
  filmTitle?: string;
  movieHallId: string;
  /** MovieHalls.HallName a szerverről */
  movieHallName?: string;
  startTime: string;
  basePrice: number;
  isCancelled: boolean;
};

export function screeningHallLabel(s: { movieHallName?: string | null }): string {
  const n = s.movieHallName?.trim();
  return n && n.length > 0 ? n : '—';
}

export async function getScreenings(): Promise<Screening[]> {
  return req<Screening[]>('/screenings');
}

export async function getScreening(id: string): Promise<Screening> {
  return req<Screening>(`/screenings/${id}`);
}

export const TicketBuyerType = {
  RegisteredUser: 0,
  Guest: 1
} as const;

export type PurchaseTicketInput = {
  screeningId: string;
  seatNumber: number;
  buyerType: (typeof TicketBuyerType)[keyof typeof TicketBuyerType];
  userId?: string | null;
  guestName?: string | null;
  guestEmail?: string | null;
  guestPhone?: string | null;
};

export async function purchaseTicket(data: PurchaseTicketInput) {
  return req<{
    screeningId?: string | null;
    seatNumber: number;
    price: number;
    purchasedAt: string;
    userId?: string | null;
    guestName?: string | null;
    guestEmail?: string | null;
    guestPhone?: string | null;
  }>('/tickets/purchase', {
    method: 'POST',
    body: JSON.stringify({
      screeningId: data.screeningId,
      seatNumber: data.seatNumber,
      buyerType: data.buyerType,
      userId: data.userId ?? null,
      guestName: data.guestName ?? null,
      guestEmail: data.guestEmail ?? null,
      guestPhone: data.guestPhone ?? null
    })
  });
}

export type MyTicket = {
  id: string;
  seatNumber: number;
  ticketPrice: number;
  purchasedAt: string;
  isCancelled?: boolean;
  isValidated?: boolean;
  validatedAt?: string | null;
  screening?: {
    startTime: string;
    film?: {
      title: string;
    };
  };
};

export async function getMyTickets(): Promise<MyTicket[]> {
  return req<MyTicket[]>('/tickets/my-tickets');
}

export async function cancelMyTicket(ticketId: string): Promise<void> {
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(`${BASE}/tickets/${ticketId}`, {
      method: 'DELETE',
      headers
    });
  } catch {
    throw new Error('A szerver nem elérhető.');
  }

  if (!res.ok) {
    const msg = await res.text();
    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);
  }
}
